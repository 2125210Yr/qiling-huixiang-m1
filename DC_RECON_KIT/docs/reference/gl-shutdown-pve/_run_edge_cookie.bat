@echo off
set HTTP_PROXY=http://127.0.0.1:7890
set HTTPS_PROXY=http://127.0.0.1:7890
cd /d "F:\????\DC_RECON_KIT\docs\reference\gl-shutdown-pve"
"C:\Users\Administrator\AppData\Local\Programs\Python\Python313\Scripts\yt-dlp.exe" --proxy http://127.0.0.1:7890 --cookies-from-browser edge --skip-download --print "%%(id)s|%%(title)s|%%(duration)s" --no-playlist https://www.youtube.com/watch?v=hgqXY5M9gFk > "_edge_cookie_run.log" 2>&1
echo EXIT=%ERRORLEVEL% >> "_edge_cookie_run.log"
