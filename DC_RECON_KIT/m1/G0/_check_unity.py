import os
import subprocess

def run(args):
    try:
        return subprocess.check_output(args, text=True, stderr=subprocess.STDOUT, timeout=20)
    except Exception as e:
        return str(e)

print("UNITY")
print(run(["tasklist", "/FI", "IMAGENAME eq Unity.exe", "/FO", "CSV", "/NH"]))
print("editor_instance", os.path.isfile(r"F:\天命之子\client\Library\EditorInstance.json"))
print("unity_exe", os.path.isfile(r"D:\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"))
