Set sh = CreateObject("WScript.Shell")
sh.Run """C:\Users\Administrator\AppData\Local\Programs\Python\Python313\Scripts\yt-dlp.exe"" -f ""bv*[height<=480]+ba/b[height<=480]/b"" --no-playlist --proxy http://127.0.0.1:7890 --force-ipv4 --legacy-server-connect --socket-timeout 30 -o ""F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.%(ext)s"" ""https://www.youtube.com/watch?v=aSbBuFD12HY""", 1, True
sh.Run """C:\Users\Administrator\AppData\Local\Programs\Python\Python313\Scripts\yt-dlp.exe"" -f ""bv*[height<=480]+ba/b[height<=480]/b"" --no-playlist --proxy http://127.0.0.1:7890 --force-ipv4 --legacy-server-connect --socket-timeout 30 -o ""F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\hgqXY5M9gFk_GL_ND_Robin_Boss.%(ext)s"" ""https://www.youtube.com/watch?v=hgqXY5M9gFk""", 1, True
sh.Run """C:\Users\Administrator\AppData\Local\Programs\Python\Python313\Scripts\yt-dlp.exe"" -f ""bv*[height<=480]+ba/b[height<=480]/b"" --no-playlist --proxy http://127.0.0.1:7890 --force-ipv4 --legacy-server-connect --socket-timeout 30 -o ""F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.%(ext)s"" ""https://www.youtube.com/watch?v=IRDqNAhKKr4""", 1, True
Set fso = CreateObject("Scripting.FileSystemObject")
Set f = fso.CreateTextFile("F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_vbs_fetch_done.txt", True)
f.WriteLine "done"
f.Close
