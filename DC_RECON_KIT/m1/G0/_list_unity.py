import subprocess

cmd = [
    "wmic",
    "process",
    "where",
    "name='Unity.exe'",
    "get",
    "ProcessId,CommandLine",
    "/FORMAT:LIST",
]
out = subprocess.check_output(cmd, encoding="utf-8", errors="replace")
safe = out.encode("ascii", errors="replace").decode("ascii")
print(safe)
