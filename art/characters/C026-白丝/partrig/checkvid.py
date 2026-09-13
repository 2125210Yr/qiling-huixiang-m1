import os
import subprocess
from pathlib import Path
import numpy as np
import cv2

d = Path(__file__).resolve().parent / "out"
mp4 = d / "C026_see_rig.mp4"
tiles = []
for t in (0.0, 3.0, 6.0, 9.0):
    png = d / ("f_%.1f.png" % t)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(mp4),
                    "-vf", "select=eq(n\\,%d)" % int(t * 60), "-vframes", "1",
                    str(png)], check=True)
    im = cv2.imdecode(np.fromfile(str(png), np.uint8), cv2.IMREAD_COLOR)
    os.remove(png)
    im = cv2.resize(im, (440, 440), interpolation=cv2.INTER_AREA)
    cv2.putText(im, "t=%.1f" % t, (10, 28), cv2.FONT_HERSHEY_SIMPLEX, 0.8,
                (60, 255, 255), 2)
    tiles.append(im)
s = np.hstack(tiles)
cv2.imencode(".png", s)[1].tofile(str(d / "see_video_check.png"))
print("wrote out/see_video_check.png")
print("mean BGR of the strip:", np.round(s.reshape(-1, 3).mean(axis=0), 1))
