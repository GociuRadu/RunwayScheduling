FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Api/Api.csproj Api/
COPY Modules.Aircrafts/Modules.Aircrafts.csproj Modules.Aircrafts/
COPY Modules.Airports/Modules.Airports.csproj Modules.Airports/
COPY Modules.Scenarios/Modules.Scenarios.csproj Modules.Scenarios/
COPY Modules.Login/Modules.Login.csproj Modules.Login/

RUN dotnet restore Api/Api.csproj

COPY Api/ Api/
COPY Modules.Aircrafts/ Modules.Aircrafts/
COPY Modules.Airports/ Modules.Airports/
COPY Modules.Scenarios/ Modules.Scenarios/
COPY Modules.Login/ Modules.Login/

WORKDIR /src/Api
RUN dotnet publish Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
