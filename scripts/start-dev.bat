@echo off
echo Starting RunwayScheduling development environment...

REM go to project root
cd /d %~dp0\..\src

REM start database
start cmd /k "docker compose up -d db"

REM start backend on HTTPS profile
start cmd /k "dotnet run --project Api --launch-profile https"

REM start frontend
start cmd /k "cd frontend && npm run dev"

echo All services started.
pause