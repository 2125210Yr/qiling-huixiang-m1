import json
import os
import shutil
import subprocess
import sys

src = r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"
ff = shutil.which("ffprobe") or shutil.which("ffmpeg")
print("tool", ff)
print("src", os.path.isfile(src), os.path.getsize(src) if os.path.isfile(src) else 0)
if not ff:
    sys.exit(2)
probe = ff.replace("ffmpeg.exe", "ffprobe.exe").replace("ffmpeg.EXE", "ffprobe.EXE")
if not os.path.isfile(probe):
    probe = shutil.which("ffprobe")
cmd = [probe, "-v", "error", "-select_streams", "v:0",
       "-show_entries", "stream=width,height,r_frame_rate,avg_frame_rate,duration,nb_frames,codec_name",
       "-of", "json", src]
r = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
print(r.stdout)
if r.returncode != 0:
    print(r.stderr)
sys.exit(r.returncode)
