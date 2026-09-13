import os
import shutil
import subprocess
import sys

src_p0 = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"
src_p1 = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"
base = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_frames_timing30"
ff = shutil.which("ffmpeg")
if not ff:
    sys.exit(2)
# Tight 30fps windows around first/last visible from 10fps pass.
jobs = [
    (src_p0, "showtime", "358.95", "1.70"),
    (src_p0, "phase", "64.15", "2.30"),
    (src_p1, "robin_qte", "53.50", "3.00") if os.path.isfile(src_p1) else None,
]
os.makedirs(base, exist_ok=True)
for job in jobs:
    if job is None:
        print("skip robin, no p1")
        continue
    src, name, ss, dur = job
    if not os.path.isfile(src):
        print("missing", src)
        continue
    out = os.path.join(base, name)
    os.makedirs(out, exist_ok=True)
    pattern = os.path.join(out, "f_%03d.png")
    cmd = [ff, "-hide_banner", "-loglevel", "error", "-y",
           "-ss", ss, "-t", dur, "-i", src, "-vf", "fps=30", pattern]
    print("run", name)
    r = subprocess.run(cmd, timeout=180)
    print(name, "exit", r.returncode, "n", len(os.listdir(out)))
sys.exit(0)
