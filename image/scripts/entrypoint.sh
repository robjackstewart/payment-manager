#!/bin/sh
set -e

# Ensure data and backup directories exist
mkdir -p "${BACKUP_DIR:-/data/backups}"

# SQLITE_DATABASE_PATH is the user-facing variable.
# Derive ConnectionStrings__PaymentManager from it so ASP.NET Core picks it up.
SQLITE_DATABASE_PATH="${SQLITE_DATABASE_PATH:-/data/paymentmanager.db}"
export SQLITE_DATABASE_PATH
export ConnectionStrings__PaymentManager="Data Source=${SQLITE_DATABASE_PATH}"
export DB_PATH="${SQLITE_DATABASE_PATH}"

# BASE_PATH sets an optional URL prefix for reverse-proxy deployments (e.g. /payment-manager).
# Strip any trailing slash so the value is consistently unprefixed.
BASE_PATH="${BASE_PATH:-}"
BASE_PATH="${BASE_PATH%/}"
# If a base path is configured, rewrite the <base href> in the pre-built index.html so the
# browser resolves all chunk URLs relative to the correct path. Defaults to / when unset.
if [ -n "$BASE_PATH" ]; then
  sed -i "s|<base href=\"[^\"]*\">|<base href=\"${BASE_PATH}/\">|" /app/wwwroot/index.html
fi
# Export for ASP.NET Core configuration binding (maps to Configuration.BasePath).
export BasePath="${BASE_PATH}"

# Apply pending EF Core migrations using the pre-built bundle.
# The bundle checks __EFMigrationsHistory and is safe to run on any database state.
echo "[migrate] Applying migrations to ${DB_PATH}..."
/scripts/efbundle --connection "Data Source=${DB_PATH}"
echo "[migrate] Done."

# Write the crontab using the configured schedule
echo "${BACKUP_CRON:-0 2 * * *} /scripts/backup.sh >> /proc/1/fd/1 2>&1" > /etc/crontabs/root

# Start crond in the background (logs to stdout via /proc/1/fd/1)
crond -f -L /dev/stdout &

# Hand off to the .NET application
exec dotnet /app/PaymentManager.WebApi.dll