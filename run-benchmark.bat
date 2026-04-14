@echo off
setlocal

set OUTPUT=%~dp0benchmark_results.csv
set HTML=%~dp0benchmark_results.html

echo Running GA benchmarks...
dotnet run --project "%~dp0src\Modules.Solver.Benchmarks" --configuration Release -- --output "%OUTPUT%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo Benchmark failed with exit code %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Opening results...
start "" "%HTML%"

