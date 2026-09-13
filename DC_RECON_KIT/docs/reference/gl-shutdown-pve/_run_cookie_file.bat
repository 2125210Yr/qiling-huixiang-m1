@echo off
set HTTP_PROXY=http://127.0.0.1:7890
set HTTPS_PROXY=http://127.0.0.1:7890
cd /d "F:\????\DC_RECON_KIT\docs\reference\gl-shutdown-pve"
echo START %DATE% %TIME% > _cookie_file_meta.log
"C:\Users\Administrator\AppData\Local\Programs\Python\Python313\Scripts\yt-dlp.exe" --proxy http://127.0.0.1:7890 --cookies "_yt_cookies.txt" --skip-download --no-playlist --print "%%(id)s|%%(title)s|%%(duration)s|%%(availability)s" "https://www.youtube.com/watch?v=hgqXY5M9gFk" >> _cookie_file_meta.log 2>&1
echo EXIT=%ERRORLEVEL% >> _cookie_file_meta.log
echo DONE %DATE% %TIME% >> _cookie_file_meta.log
