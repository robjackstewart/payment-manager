# Stage 1: Build Angular frontend
FROM node:24-alpine AS frontend-build
WORKDIR /app
RUN apk add --no-cache curl && \
    sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -b /usr/local/bin
COPY frontend/Taskfile.yml ./
COPY frontend/package*.json ./
RUN task install-locked-dependencies
COPY frontend/ .
RUN task build:production

# Stage 2: Publish .NET WebAPI and build migrations bundle
# Use Alpine SDK so the native SQLite library targets musl (matching the runtime image)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS backend-build
WORKDIR /src
RUN apk add --no-cache curl && \
    sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -b /usr/local/bin
COPY Directory.Packages.props .
COPY .config/ .config/
COPY backend/Taskfile.yml ./
COPY backend/ .
ENV CONFIGURATION=Release
RUN task publish OUTPUT=/app/publish
RUN task bundle-migrations OUTPUT=/app/efbundle

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

# crond is provided by busybox, which is included in Alpine
# tini is used as PID 1 to handle zombie reaping and signal forwarding
WORKDIR /app
RUN apk add --no-cache tini

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

ENTRYPOINT ["/sbin/tini", "--", "/scripts/entrypoint.sh"]
