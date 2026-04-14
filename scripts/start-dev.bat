@echo off
setlocal

echo Starting RunwayScheduling development environment...

cd /d %~dp0\..\src

call :kill_port 5173
call :kill_port 7286
call :kill_port 5190

start "runway-db" cmd /k "docker compose up -d db"
timeout /t 4 /nobreak >nul

start "runway-api" cmd /k "dotnet run --project Api\Api.csproj --launch-profile https"
timeout /t 6 /nobreak >nul

start "runway-frontend" cmd /k "cd frontend && npm run dev -- --host localhost"

echo.
echo Services started:
echo - API: https://localhost:7286
echo - Frontend: https://localhost:5173
echo - PostgreSQL: localhost:5433
echo.
pause
exit /b 0

:kill_port
for /f "tokens=5" %%I in ('netstat -ano ^| findstr /R /C:":%1 .*LISTENING"') do (
    taskkill /PID %%I /F >nul 2>nul
)
exit /b 0
