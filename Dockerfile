# Stage 1: Build Angular frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ .
RUN npm run build -- --configuration production

# Stage 2: Publish .NET WebAPI and build migrations bundle
# Use Alpine SDK so the native SQLite library targets musl (matching the runtime image)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build
WORKDIR /src
COPY Directory.Packages.props .
COPY .config/ .config/
COPY backend/ .
RUN dotnet publish src/PaymentManager.WebApi/PaymentManager.WebApi.csproj \
    --configuration Release \
    --output /app/publish \
    --no-self-contained

# Restore local tools (dotnet-ef) from .config/dotnet-tools.json
RUN dotnet tool restore
# The bundle checks __EFMigrationsHistory and only applies pending migrations,
# making it safe to run on both fresh and existing databases.
# (--idempotent is not supported for SQLite; the bundle handles idempotency itself.)
RUN dotnet tool run dotnet-ef migrations bundle \
    --self-contained \
    --output /app/efbundle \
    --project src/PaymentManager.Infrastructure/PaymentManager.Infrastructure.Sqlite.csproj \
    --startup-project src/PaymentManager.WebApi/PaymentManager.WebApi.csproj \
    --configuration Release

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

# crond is provided by busybox, which is included in Alpine
WORKDIR /app

COPY --from=backend-build /app/publish .
COPY --from=backend-build /app/efbundle /scripts/efbundle

# Angular 18+ outputs browser assets to dist/<name>/browser/
COPY --from=frontend-build /app/dist/payment-manager/browser ./wwwroot

COPY image/scripts/entrypoint.sh /scripts/entrypoint.sh
COPY image/scripts/backup.sh /scripts/backup.sh
RUN chmod +x /scripts/*.sh /scripts/efbundle

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    SQLITE_DATABASE_PATH=/data/paymentmanager.db \
    BACKUP_CRON="0 2 * * *" \
    MAX_BACKUP_COUNT=7 \
    BACKUP_DIR=/data/backups

VOLUME /data

EXPOSE 8080

ENTRYPOINT ["/scripts/entrypoint.sh"]
