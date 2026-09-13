@echo off
cd /d "F:\????\DC_RECON_KIT\docs\reference\gl-shutdown-pve"
echo START %DATE% %TIME% > _ytdlp_api_test.log
"C:\Users\Administrator\AppData\Local\Programs\Python\Python313\python.exe" -u _ytdlp_api_test.py >> _ytdlp_api_test.log 2>&1
echo EXIT=%ERRORLEVEL% >> _ytdlp_api_test.log
