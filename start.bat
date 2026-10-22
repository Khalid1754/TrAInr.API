@echo off
rem Startar TrAInr i Docker och oppnar appen i webblasaren. Dubbelklicka for att kora.
title TrAInr
cd /d "%~dp0"

docker info >nul 2>&1
if not errorlevel 1 goto docker_klar
echo Startar Docker Desktop...
start "" "%ProgramFiles%\Docker\Docker\Docker Desktop.exe"
:vanta_docker
timeout /t 3 /nobreak >nul
docker info >nul 2>&1
if errorlevel 1 goto vanta_docker
:docker_klar

echo Startar TrAInr (forsta gangen kan ta nagon minut)...
docker compose up -d --build
if not errorlevel 1 goto vanta_app
echo.
echo Nagot gick fel. Las felmeddelandet ovan.
pause
exit /b 1

:vanta_app
echo Vantar pa att appen ska svara...
:vanta_igen
timeout /t 2 /nobreak >nul
curl -sf -o nul http://localhost:8080/api/workouts/status
if errorlevel 1 goto vanta_igen

start "" http://localhost:8080
echo TrAInr kor pa http://localhost:8080
timeout /t 3 >nul
