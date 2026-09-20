@echo off
chcp 65001 >nul
set "EXE=%~dp0..\client\Builds\Win64\Resonance.exe"
if not exist "%EXE%" set "EXE=%~dp0..\dist\windows\Resonance.exe"
if not exist "%EXE%" (
    echo 未找到游戏构建，请先生成 Windows Player。
    exit /b 1
)
start "" "%EXE%"
