import os
import shutil
import subprocess
import sys

src = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"
out = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_frames_p0_tap330"
os.makedirs(out, exist_ok=True)
ff = shutil.which("ffmpeg")
print("ffmpeg", ff)
if not ff or not os.path.isfile(src):
    sys.exit(2)
pattern = os.path.join(out, "seq_%02d.png")
# Slide tutorial + nearby idle/charge: 330-365
cmd = [ff, "-hide_banner", "-loglevel", "error", "-y",
       "-ss", "330", "-t", "36", "-i", src, "-vf", "fps=1", pattern]
print("run")
r = subprocess.run(cmd, timeout=180)
print("exit", r.returncode)
for name in os.listdir(out):
    if not name.startswith("seq_"):
        continue
    n = int(name[4:6])
    dest = os.path.join(out, "p0_t%03d.png" % (329 + n))
    srcp = os.path.join(out, name)
    if os.path.isfile(dest):
        os.remove(dest)
    os.rename(srcp, dest)
kept = sorted(x for x in os.listdir(out) if x.endswith(".png"))
print("n", len(kept))
print("files", ",".join(kept))
sys.exit(r.returncode)
