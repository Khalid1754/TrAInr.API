@echo off
rem Oppnar port 8080 i Windows-brandvaggen sa att mobilen kan na TrAInr pa hemmanatverket.
rem Behover koras en gang. Fragar efter administratorsbehorighet.
net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)
netsh advfirewall firewall delete rule name="TrAInr" >nul 2>&1
netsh advfirewall firewall add rule name="TrAInr" dir=in action=allow protocol=TCP localport=8080 profile=private
echo.
echo Klart. Mobilen kan nu na TrAInr nar den ar pa samma wifi.
echo Kontrollera att wifi-natverket ar satt som "Privat" i Windows.
pause
