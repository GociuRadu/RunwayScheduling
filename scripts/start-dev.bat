@echo off
echo Starting RunwayScheduling development environment...

cd /d %~dp0\..\src

start cmd /k "docker compose up -d db"
start cmd /k "dotnet run --project Api --launch-profile https"
start cmd /k "cd frontend && npm run dev"

echo All services started.
pause
