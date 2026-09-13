import os
import subprocess

root = "F:\\" + "\u5929\u547d\u4e4b\u5b50"
log = os.path.join(root, "DC_RECON_KIT", "m1", "G0", "BASELINE_LOGS", "editor-playmode-20260913-patha5.log")
os.makedirs(os.path.dirname(log), exist_ok=True)
if os.path.isfile(log):
    try:
        os.remove(log)
    except OSError:
        log = os.path.join(os.path.dirname(log), "editor-playmode-20260913-patha4b.log")
        print("log locked, using", log)
unity = r"D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
proj = os.path.join(root, "client")
print("unity", os.path.isfile(unity), "proj", os.path.isdir(proj))
subprocess.Popen([unity, "-projectPath", proj, "-logFile", log], cwd=proj)
print("launched", log)
