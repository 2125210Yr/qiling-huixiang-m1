import os
import shutil
import subprocess
import sys

src = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"
out = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_frames_p0_death60"
os.makedirs(out, exist_ok=True)
ff = shutil.which("ffmpeg")
print("ffmpeg", ff)
print("src_exists", os.path.isfile(src), os.path.getsize(src) if os.path.isfile(src) else 0)
if not ff:
    sys.exit(2)
pattern = os.path.join(out, "seq_%02d.png")
cmd = [ff, "-hide_banner", "-loglevel", "error", "-y",
       "-ss", "60", "-t", "31", "-i", src, "-vf", "fps=1", pattern]
print("run", " ".join(cmd))
r = subprocess.run(cmd, timeout=180)
print("exit", r.returncode)
# rename seq_01 -> p0_t60
for name in os.listdir(out):
    if not name.startswith("seq_"):
        continue
    n = int(name[4:6])
    dest = os.path.join(out, "p0_t%02d.png" % (59 + n))
    srcp = os.path.join(out, name)
    if os.path.isfile(dest):
        os.remove(dest)
    os.rename(srcp, dest)
kept = sorted(x for x in os.listdir(out) if x.endswith(".png"))
print("n", len(kept))
print("files", ",".join(kept))
sys.exit(r.returncode)
