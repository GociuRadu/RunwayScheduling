FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Api/Api.csproj Api/
COPY Modules.Aircrafts/Modules.Aircrafts.csproj Modules.Aircrafts/
COPY Modules.Airports/Modules.Airports.csproj Modules.Airports/
COPY Modules.Scenarios/Modules.Scenarios.csproj Modules.Scenarios/
COPY Modules.Login/Modules.Login.csproj Modules.Login/
COPY Modules.Solver/Modules.Solver.csproj Modules.Solver/

RUN dotnet restore Api/Api.csproj

COPY Api/ Api/
COPY Modules.Aircrafts/ Modules.Aircrafts/
COPY Modules.Airports/ Modules.Airports/
COPY Modules.Scenarios/ Modules.Scenarios/
COPY Modules.Login/ Modules.Login/
COPY Modules.Solver/ Modules.Solver/

WORKDIR /src/Api
RUN dotnet publish Api.csproj -c Release -o /app/publish \
    /p:UseAppHost=false \
    --self-contained false \
    -p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && (groupmod --gid 1654 app 2>/dev/null || groupadd --gid 1654 app) \
    && (usermod --uid 1654 --gid 1654 app 2>/dev/null || useradd --uid 1654 --gid app --no-create-home app)

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

COPY --from=build --chown=app:app /app/publish .
USER app
ENTRYPOINT ["dotnet", "Api.dll"]
