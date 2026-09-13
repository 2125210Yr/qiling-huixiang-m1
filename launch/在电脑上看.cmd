@echo off
chcp 65001 >nul
set EXE=%~dp0..\dist\windows\Resonance.exe
if not exist "%EXE%" set EXE=F:\Resonance\client\Builds\Win64\Resonance.exe
start "" "%EXE%"
