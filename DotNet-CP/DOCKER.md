# Running ComputerSeekho in Docker

Three containers — MySQL, the Spring Boot backend, the React frontend behind
nginx — brought up by one command.

```bash
cp .env.example .env      # copy .env.example .env  on Windows
docker compose up --build
```

First run takes a few minutes: Maven downloads the dependency tree, npm
installs, and MySQL loads the schema. Later runs are seconds.

| | URL |
|---|---|
| Website | http://localhost:5173 |
| API | http://localhost:8080/api |
| Health | http://localhost:8080/api/actuator/health |
| MySQL | `localhost:3307`, user `root` |

Stop with `Ctrl+C`, or `docker compose down`. Add `-v` to also wipe the
database and uploaded photos.

---

## Why these ports

**8080 and 5173 match local development exactly.** The Google OAuth client
is registered against `http://localhost:8080/api/login/oauth2/code/google`
and redirects to `http://localhost:5173/oauth/callback`. Using different
ports in Docker would mean maintaining a second set of redirect URIs in the
Google console for no benefit.

**MySQL is the exception, published on 3307.** A locally installed MySQL is
almost certainly already holding 3306, and two servers fighting over one port
produces a failure that reads like a corrupt database. Nothing inside the
stack uses 3307 — the backend reaches MySQL as `mysql:3306` on the container
network.

## What each image does

**Backend** — multi-stage. A `maven:3.9-eclipse-temurin-21` stage builds the
jar; the runtime stage is `eclipse-temurin:21-jre-jammy` and keeps none of the
toolchain. `pom.xml` is copied and dependencies resolved *before* `src/`, so
editing a Java file doesn't invalidate the cached dependency layer. Runs as a
non-root `spring` user.

Debian rather than Alpine deliberately: OpenPDF renders the receipt through
`java.awt`, and headless AWT on Alpine needs fontconfig and musl workarounds
costing more than the ~60 MB saved.

**Frontend** — Vite builds the bundle in a `node:22-alpine` stage, then the
static files are served by `nginx:alpine`. No JavaScript runtime in the final
image at all.

`nginx.conf` does one essential thing: `try_files $uri $uri/ /index.html`.
React Router owns the URL space, so a visitor reloading on
`/campus-life/albums/3` — or landing on `/oauth/callback` coming back from
Google — is asking nginx for a file that doesn't exist. Without the fallback
both are a 404 and the OAuth flow dies on its last hop.

**MySQL** — `mysql:8.4`, with `db/` mounted read-only at `/sql` and
`docker/mysql/init.sh` as the only file in `/docker-entrypoint-initdb.d`.

## Why the database needs an init script

The MySQL entrypoint runs whatever it finds in `/docker-entrypoint-initdb.d`
in **alphabetical order**. Mounting `db/` there directly would run
`migration_002_contact_messages.sql` before `schema.sql` and fail on the first
foreign key. The script names the files in the order they were written:

```
schema.sql → seed_sample_data.sql → migration_002 … 007
```

It runs **only when the data volume is empty**. Editing SQL and restarting
does nothing; to reload:

```bash
docker compose down -v
docker compose up --build
```

## Configuration

The backend runs under a `docker` profile
(`application-docker.properties`), separate from `prod` on purpose. `prod`
declares `${DB_URL}` and `${MAIL_HOST}` with no defaults so a real deployment
must supply every one — correct in production, wrong for someone running
`docker compose up` to look at the project. In the `docker` profile every
value has a working default and only secrets are worth setting.

Everything lives in `.env` (see `.env.example`). All of it is optional:

| Variable | Blank means |
|---|---|
| `DB_PASSWORD` | defaults to `computerseekho` |
| `JWT_SECRET` | a dev default is used — must be ≥32 chars |
| `GOOGLE_CLIENT_ID` / `_SECRET` | visitor sign-in unavailable |
| `MAIL_USERNAME` / `MAIL_PASSWORD` | mail logged instead of sent |

`.env` is gitignored. `.env.example` is the committed template.

## Startup ordering

`depends_on` alone only waits for a container to *start*, and MySQL accepts
TCP connections well before it will answer a query. Since the backend runs
`ddl-auto=validate` and reads the schema during startup, it would crash in
that window.

So MySQL has a `mysqladmin ping` healthcheck and the backend waits on
`condition: service_healthy`. The backend has its own actuator-based
healthcheck for the same reason — "the port is open" isn't the same as
"the application is ready".

## Volumes

| Volume | Holds | Lost on `down -v` |
|---|---|---|
| `mysql-data` | the database | yes |
| `uploads` | uploaded photos | yes |

Both are **named volumes**, not bind mounts. MySQL's data directory needs
permissions and file semantics a Windows host directory can't provide, and it
corrupts in ways that look like random crashes. Uploads are a named volume so
photos survive `docker compose up --build`; in the container's writable layer
they'd vanish on every rebuild.

## Troubleshooting

**`port is already allocated`** — something local holds 8080 or 5173. Stop the
STS backend and the Vite dev server, or change the left-hand side of the port
mapping in `docker-compose.yml`. If you change 8080 or 5173, the Google
redirect URIs need updating to match.

**Backend restarts in a loop** — `docker compose logs backend`. Most likely
the schema didn't load; check `docker compose logs mysql` for the `[init]`
lines, and confirm each SQL file was listed.

**`Table 'computerseekho.staff' doesn't exist`** — the init script didn't run,
which means the volume already existed from an earlier attempt.
`docker compose down -v` and bring it up again.

**`/docker-entrypoint-initdb.d/00-init.sh: cannot execute`** — Git rewrote the
script's line endings to CRLF on checkout. Convert it back to LF, or add
`*.sh text eol=lf` to a `.gitattributes` file.

**Google sign-in fails in Docker but works locally** — the container needs
`GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` in `.env`. They're set in your
STS run configuration, which Docker knows nothing about.

**Images render as broken links** — photos uploaded before the volume existed
are gone. The database still holds their URLs; re-upload them.

## Rebuilding

```bash
docker compose up --build              # after a code change
docker compose build --no-cache backend # if a cached layer is suspect
docker compose down -v && docker compose up --build   # full reset
```
