# ---------------------------------------------------------------- build
FROM node:22-alpine AS build

WORKDIR /build

# Lockfile first, for the same layer-caching reason as the backend's
# pom.xml: dependencies change far less often than source does.
COPY package.json package-lock.json ./
# `npm ci` rather than `npm install` — it installs exactly what the
# lockfile pins and fails if the two disagree, so an image can't quietly
# be built against a different dependency tree than the one that was tested.
RUN npm ci

COPY . .

# Vite inlines VITE_* variables at build time; there is no runtime config
# in a static bundle. So the API URL has to be known here, which is why it
# is a build argument rather than an environment variable on the running
# container.
#
# It points at the published host port, not the compose service name: this
# value is used by the visitor's browser, which is outside the container
# network and cannot resolve `backend`.
ARG VITE_API_BASE_URL=http://localhost:8080/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# -------------------------------------------------------------- runtime
# A built React app is static files. Serving them from nginx rather than
# from Node means no JavaScript runtime in the final image at all.
FROM nginx:alpine

COPY --from=build /build/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

# -O /dev/null rather than --spider. BusyBox's wget (which is what Alpine
# has) implements --spider inconsistently and returns a non-zero exit code
# on a perfectly good 200, so the probe fails while nginx is serving fine.
# Fetching the body and discarding it tests the same thing unambiguously.
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD ["sh", "-c", "wget -q -O /dev/null http://localhost/ || exit 1"]
