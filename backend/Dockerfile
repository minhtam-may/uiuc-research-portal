# Multi-stage build for the API. Build context must be `backend/` so this Dockerfile
# can see the whole solution and restore project references correctly.
#
# Not required for local development (see "How to Run" in README.md, which uses
# `dotnet run` directly) or for the Codespaces demo (see "Quick demo via GitHub
# Codespaces", which also runs the API with `dotnet run`). Kept here, verified with
# a real `docker build` + `docker run` against the local Postgres container (see
# docs/decisions.md), as a documented, working path to containerize the API if this
# ever needs to run on a container platform.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for better Docker layer caching across source-only changes.
COPY UiucResearchPortal.sln ./
COPY src/UiucResearchPortal.Domain/UiucResearchPortal.Domain.csproj src/UiucResearchPortal.Domain/
COPY src/UiucResearchPortal.Application/UiucResearchPortal.Application.csproj src/UiucResearchPortal.Application/
COPY src/UiucResearchPortal.Infrastructure/UiucResearchPortal.Infrastructure.csproj src/UiucResearchPortal.Infrastructure/
COPY src/UiucResearchPortal.Api/UiucResearchPortal.Api.csproj src/UiucResearchPortal.Api/
COPY src/UiucResearchPortal.Ingestion/UiucResearchPortal.Ingestion.csproj src/UiucResearchPortal.Ingestion/
COPY tests/UiucResearchPortal.Tests/UiucResearchPortal.Tests.csproj tests/UiucResearchPortal.Tests/

COPY . .

# Restore and publish in one step. An earlier attempt restored only Api.csproj then
# published with --no-restore, which failed with NETSDK1064 ("package ... was not
# found") -- the separate restore apparently didn't fully resolve every transitive
# package for the referenced projects in this SDK image. Letting `publish` do its own
# restore avoids that mismatch.
RUN dotnet publish src/UiucResearchPortal.Api/UiucResearchPortal.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Most container platforms route to a fixed container port; Kestrel must bind to it
# explicitly instead of the local dev port (5070).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UiucResearchPortal.Api.dll"]
