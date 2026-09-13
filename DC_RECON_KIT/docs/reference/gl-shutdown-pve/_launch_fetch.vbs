Set sh = CreateObject("WScript.Shell")
sh.Run "cmd.exe /c ""F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_run_fetch.bat""", 0, False
Set fso = CreateObject("Scripting.FileSystemObject")
Set f = fso.CreateTextFile("F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_vbs_launched.txt", True)
f.WriteLine "launched"
f.Close
