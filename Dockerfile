# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY . .
RUN dotnet restore Jellyfin.Server/Jellyfin.Server.csproj
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8096 \
    JELLYFIN_WEB_DIR=/usr/share/jellyfin/web \
    DOTNET_EnableDiagnostics=0

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg libfontconfig1 ffmpeg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://repo.jellyfin.org/jellyfin_team.gpg.key | gpg --dearmor -o /etc/apt/keyrings/jellyfin.gpg \
    && VERSION_OS="$(awk -F= '/^ID=/{ print $2 }' /etc/os-release)" \
    && VERSION_CODENAME="$(awk -F= '/^VERSION_CODENAME=/{ print $2 }' /etc/os-release)" \
    && DPKG_ARCHITECTURE="$(dpkg --print-architecture)" \
    && printf "Types: deb\nURIs: https://repo.jellyfin.org/%s\nSuites: %s\nComponents: main\nArchitectures: %s\nSigned-By: /etc/apt/keyrings/jellyfin.gpg\n" \
        "$VERSION_OS" "$VERSION_CODENAME" "$DPKG_ARCHITECTURE" > /etc/apt/sources.list.d/jellyfin.sources \
    && apt-get update \
    && apt-get install -y --no-install-recommends jellyfin-web \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish/ /app/

EXPOSE 8096
ENTRYPOINT ["./jellyfin"]
