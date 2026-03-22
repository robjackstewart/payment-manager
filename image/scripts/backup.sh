#!/bin/sh
set -e

BACKUP_DIR="${BACKUP_DIR:-/data/backups}"
MAX_BACKUP_COUNT="${MAX_BACKUP_COUNT:-7}"
DB_PATH="${DB_PATH:-${SQLITE_DATABASE_PATH:-/data/paymentmanager.db}}"

if [ ! -f "$DB_PATH" ]; then
    echo "[backup] Database not found at $DB_PATH, skipping backup."
    exit 0
fi

mkdir -p "$BACKUP_DIR"

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
DEST="$BACKUP_DIR/paymentmanager-$TIMESTAMP.db"

cp "$DB_PATH" "$DEST"
echo "[backup] Created $DEST"

# Remove oldest backups beyond MAX_BACKUP_COUNT
EXCESS=$(ls -t "$BACKUP_DIR"/paymentmanager-*.db 2>/dev/null | tail -n +$((MAX_BACKUP_COUNT + 1)))
if [ -n "$EXCESS" ]; then
    echo "$EXCESS" | xargs rm
    echo "[backup] Pruned old backups, keeping latest $MAX_BACKUP_COUNT."
fi