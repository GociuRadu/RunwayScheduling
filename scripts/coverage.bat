@echo off
cd /d "%~dp0.."

dotnet test tests/RunwayScheduling.Tests/RunwayScheduling.Tests.csproj ^
  /p:CollectCoverage=true ^
  /p:CoverletOutputFormat=lcov ^
  /p:CoverletOutput=./coverage.lcov
if %errorlevel% neq 0 exit /b %errorlevel%

where reportgenerator >nul 2>&1
if %errorlevel% neq 0 (
  echo Installing reportgenerator...
  dotnet tool install -g dotnet-reportgenerator-globaltool
)

reportgenerator ^
  -reports:"tests/RunwayScheduling.Tests/coverage.lcov" ^
  -targetdir:"coverage-report" ^
  -reporttypes:Html
if %errorlevel% neq 0 exit /b %errorlevel%

start coverage-report/index.html
