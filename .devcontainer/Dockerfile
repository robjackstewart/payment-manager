FROM mcr.microsoft.com/devcontainers/dotnet:9.0

ARG DOTNET_VERSION=9.0

RUN apt update \
    && apt upgrade -y \
    nodejs \
    npm

RUN npm install -g @azure/static-web-apps-cli @go-task/cli

