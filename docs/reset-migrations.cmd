@echo off
setlocal

REM === ROOT (folder src) ===
set ROOT_DIR=%~dp0..
set API_DIR=%ROOT_DIR%\Api

cd /d "%ROOT_DIR%"

echo.
echo ===== STOP & REMOVE CONTAINER + VOLUME =====
docker compose down -v

echo.
echo ===== START FRESH POSTGRES =====
docker compose up -d

timeout /t 5 >nul

echo.
echo ===== DELETE OLD MIGRATIONS (FILES) =====
if exist "%API_DIR%\DataBase\Migrations" (
  rmdir /s /q "%API_DIR%\DataBase\Migrations"
)

echo.
echo ===== BUILD API =====
cd /d "%API_DIR%"
dotnet build || exit /b 1

echo.
echo ===== CREATE NEW MIGRATION =====
dotnet ef migrations add InitialCreate --context Api.DataBase.AppDbContext --output-dir DataBase\Migrations || exit /b 1

echo.
echo ===== APPLY MIGRATION =====
dotnet ef database update --context Api.DataBase.AppDbContext --connection "Host=localhost;Port=5433;Database=RunwayScheduling;Username=postgres;Password=postgre1234" || exit /b 1

echo.
echo ===== VERIFY TABLES =====
docker exec -it runway_db psql -U postgres -d RunwayScheduling -c "\dt"

echo.
echo ===== DONE =====
endlocal