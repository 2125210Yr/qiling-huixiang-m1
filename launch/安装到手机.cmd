@echo off
chcp 65001 >nul
set ADB=D:\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
set APK=%~dp0..\dist\android\契灵回响.apk
if not exist "%APK%" set APK=F:\Resonance\client\Builds\Resonance.apk
echo Installing %APK%
"%ADB%" start-server
"%ADB%" devices
"%ADB%" install -r "%APK%"
if errorlevel 1 (
  echo 安装失败。请插上手机、解锁、允许USB调试后重试。
  pause
  exit /b 1
)
"%ADB%" shell am start -n com.resonance.verticalslice/com.unity3d.player.UnityPlayerGameActivity
echo 已安装并启动。
pause
