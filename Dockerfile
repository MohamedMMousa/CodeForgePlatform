# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` is cached independently of source changes.
COPY src/CodeForge.Api/CodeForge.Api.csproj src/CodeForge.Api/
COPY src/CodeForge.Application/CodeForge.Application.csproj src/CodeForge.Application/
COPY src/CodeForge.Domain/CodeForge.Domain.csproj src/CodeForge.Domain/
COPY src/CodeForge.Infrastructure/CodeForge.Infrastructure.csproj src/CodeForge.Infrastructure/
RUN dotnet restore src/CodeForge.Api/CodeForge.Api.csproj

COPY src/ src/
RUN dotnet publish src/CodeForge.Api/CodeForge.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
# Not Alpine: Program.cs constructs CultureInfo("ar")/("en") for the localization
# pipeline, which needs full ICU — an invariant-mode Alpine image would throw at startup.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime
WORKDIR /app

# curl is needed for docker-compose's HTTP healthcheck against /health — not present
# in the base runtime image by default.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app .

# "app" is the non-root user Microsoft's .NET 8+ runtime images create by default.
USER app

ENTRYPOINT ["dotnet", "CodeForge.Api.dll"]
