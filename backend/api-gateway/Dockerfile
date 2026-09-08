# syntax=docker/dockerfile:1

# ---------- Stage 1: restore ----------
# Only the .csproj files are copied first, so Docker reuses the cached restore
# layer whenever source changes but dependencies do not.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
WORKDIR /src
COPY shared/HRM.Shared/HRM.Shared.csproj shared/HRM.Shared/
COPY api-gateway/HRM.ApiGateway.csproj api-gateway/
RUN dotnet restore api-gateway/HRM.ApiGateway.csproj

# ---------- Stage 2: publish ----------
FROM restore AS build
COPY shared/ shared/
COPY api-gateway/ api-gateway/
RUN dotnet publish api-gateway/HRM.ApiGateway.csproj \
    -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false

# ---------- Stage 3: runtime ----------
# aspnet (not sdk) keeps the image small - runtime only, no compilers.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# curl is only here so Docker's HEALTHCHECK can call /health.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# The base image ships a non-root "app" user. Port 8080 is above 1024,
# so no elevated privileges are needed to bind it.
USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "HRM.ApiGateway.dll"]
