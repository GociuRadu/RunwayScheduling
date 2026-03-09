@echo off
setlocal

REM =========================================================
REM Reset EF migrations + recreate DB in docker (Postgres)
REM Safe version without quoted DB name issues
REM =========================================================

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "SRC_DIR=%%~fI\"
set "API_DIR=%SRC_DIR%Api"

cd /d "%SRC_DIR%"

docker compose up -d || exit /b 1
docker ps

if exist "%API_DIR%\DataBase\Migrations" (
  rmdir /s /q "%API_DIR%\DataBase\Migrations"
)

REM Force drop by recreating container cleanly
docker compose down -v || exit /b 1
docker compose up -d || exit /b 1

cd /d "%API_DIR%"
dotnet build || exit /b 1

dotnet ef migrations add InitialCreate --context Api.DataBase.AppDbContext --output-dir DataBase\Migrations || exit /b 1

dotnet ef database update --context Api.DataBase.AppDbContext --connection "Host=localhost;Port=5433;Database=RunwayScheduling;Username=postgres;Password=postgre1234" || exit /b 1

cd /d "%SRC_DIR%"
docker exec -it runway_db psql -U postgres -d RunwayScheduling -c "\dt"

echo.
echo DONE.
endlocal