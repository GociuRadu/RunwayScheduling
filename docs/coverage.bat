@echo off
dotnet test tests/RunwayScheduling.Tests/RunwayScheduling.Tests.csproj ^
  /p:CollectCoverage=true ^
  /p:CoverletOutputFormat=lcov ^
  /p:CoverletOutput=./coverage.lcov
if %errorlevel% neq 0 exit /b %errorlevel%

%USERPROFILE%\.dotnet\tools\reportgenerator ^
  -reports:"tests/RunwayScheduling.Tests/coverage.lcov" ^
  -targetdir:"coverage-report" ^
  -reporttypes:Html
if %errorlevel% neq 0 exit /b %errorlevel%

start coverage-report/index.html
