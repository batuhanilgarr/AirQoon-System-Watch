#!/bin/sh
set -e

DUMP_PATH="${MONGO_DUMP_PATH:-/dumps/airqoonBaseMapDB-2026-01-16.gz}"
DB_NAME="${MONGO_DB_NAME:-airqoonBaseMapDB}"

if [ ! -f "$DUMP_PATH" ]; then
  echo "Mongo dump not found at $DUMP_PATH" >&2
  exit 1
fi

echo "Restoring MongoDB dump into database: $DB_NAME"

mongorestore \
  --gzip \
  --archive="$DUMP_PATH" \
  --nsInclude "${DB_NAME}.*" \
  --drop

echo "MongoDB restore completed"
