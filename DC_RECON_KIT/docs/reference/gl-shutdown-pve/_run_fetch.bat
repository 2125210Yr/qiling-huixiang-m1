@echo off
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0_fetch_yt.ps1"
echo.
echo Exit=%ERRORLEVEL%
dir /b *.mp4 2>nul
pause
