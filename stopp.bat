@echo off
rem Stoppar TrAInr. Datan i databasen sparas till nasta start.
title TrAInr
cd /d "%~dp0"
docker compose stop
echo TrAInr ar stoppat.
timeout /t 3 >nul
