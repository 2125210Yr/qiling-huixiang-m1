import os
import shutil
import subprocess
import sys

src = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"
base = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_frames_timing"
ff = shutil.which("ffmpeg")
if not ff or not os.path.isfile(src):
    sys.exit(2)
# 10 fps: enough to bound cue length to 0.1s
windows = [
    ("showtime", "358.5", "4.0"),
    ("phase", "63.5", "3.5"),
    ("victory", "389.5", "8.0"),
]
for name, ss, dur in windows:
    out = os.path.join(base, name)
    os.makedirs(out, exist_ok=True)
    pattern = os.path.join(out, "f_%03d.png")
    cmd = [ff, "-hide_banner", "-loglevel", "error", "-y",
           "-ss", ss, "-t", dur, "-i", src, "-vf", "fps=10", pattern]
    print("run", name)
    r = subprocess.run(cmd, timeout=180)
    print(name, "exit", r.returncode, "n", len(os.listdir(out)))
sys.exit(0)
