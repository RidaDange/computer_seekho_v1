#!/bin/bash
#
# Loads the schema, seed data and migrations into a fresh MySQL container.
#
# Why a script instead of mounting db/ straight into
# /docker-entrypoint-initdb.d: the entrypoint runs whatever it finds there
# in *alphabetical* order, which would put migration_002 before schema.sql
# and fail on the first foreign key. The order below is the order these were
# written in, and it is the only correct one.
#
# The entrypoint runs this against a temporary server on a local socket
# before MySQL accepts network connections, and only on first boot — once
# the data volume exists this never runs again. To re-seed, remove the
# volume: docker compose down -v
set -euo pipefail

SQL_DIR=/sql

# Each file already contains its own CREATE DATABASE / USE, so no database
# is named here — doing so would override the USE statements and silently
# load everything into the wrong schema if they ever disagreed.
FILES=(
  schema.sql
  seed_sample_data.sql
  migration_002_contact_messages.sql
  migration_003_staff_passwords.sql
  migration_004_day3_followups.sql
  migration_005_day4_seed.sql
  migration_006_batch_albums.sql
  migration_007_gallery_themes.sql
)

echo "[init] loading ${#FILES[@]} SQL files"

for file in "${FILES[@]}"; do
  if [[ ! -f "$SQL_DIR/$file" ]]; then
    # Loudly, not silently. A missing migration leaves the database in a
    # state the application will then fail to validate against, and tracing
    # that back from a Hibernate error is far harder than reading it here.
    echo "[init] ERROR: $file not found in $SQL_DIR" >&2
    exit 1
  fi

  echo "[init]   $file"
  mysql --protocol=socket -uroot -p"${MYSQL_ROOT_PASSWORD}" < "$SQL_DIR/$file"
done

echo "[init] done"
