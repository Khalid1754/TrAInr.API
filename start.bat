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
set forsok=0
:vanta_igen
timeout /t 2 /nobreak >nul
curl -sf -o nul http://localhost:8080/api/status
if not errorlevel 1 goto oppna
set /a forsok+=1
rem Ge upp efter ca 2 minuter och oppna appen anda.
if %forsok% lss 60 goto vanta_igen

:oppna

start "" http://localhost:8080
echo TrAInr kor pa http://localhost:8080

rem Datorns adress pa wifi, sa att appen kan oppnas fran mobilen pa samma natverk.
set ip=
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "(Get-NetIPConfiguration | Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } | Select-Object -First 1).IPv4Address.IPAddress"`) do set ip=%%i
if defined ip (
    echo.
    echo Pa mobilen ^(samma wifi^): http://%ip%:8080
)
echo.
pause
