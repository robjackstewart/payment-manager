FROM mcr.microsoft.com/devcontainers/dotnet:9.0

SHELL ["bash", "-c"]

RUN apt update \
    && apt upgrade -y

# install nodejs
RUN <<EOT bash
set -ex
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.2/install.sh | bash
source ~/.bashrc
nvm install 22
EOT

RUN npm install -g @go-task/cli

RUN dotnet workload update