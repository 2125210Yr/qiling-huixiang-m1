"""src_img vs the layer composite, side by side, so the order can be judged."""
import sys
from pathlib import Path
import numpy as np
import cv2

sys.path.insert(0, str(Path(__file__).resolve().parent))
from partrig import Skeleton, render, imread_unicode
import seerig as S

HERE = Path(__file__).resolve().parent

m = S.load_see_model()
sk = Skeleton(m)
sk.resolve()
c = render(m, sk)
got = np.clip(c[..., :3] * 255, 0, 255).astype(np.uint8)

src = imread_unicode(S.SEE_DIR / "src_img.png")
a = src[..., 3:4].astype(np.float32) / 255.0
ref = np.clip(src[..., :3].astype(np.float32) * a, 0, 255).astype(np.uint8)

d = np.abs(c[..., :3] - src[..., :3].astype(np.float32) / 255.0 * a).max(axis=2)
heat = np.clip(d * 2, 0, 1)
heat_img = (np.dstack([heat, heat * 0.2, 1 - heat]) * 255).astype(np.uint8)

sheet = np.hstack([ref, got, heat_img])
sheet = cv2.resize(sheet, None, fx=0.55, fy=0.55, interpolation=cv2.INTER_AREA)
cv2.imencode(".png", sheet[..., ::-1])[1].tofile(str(HERE / "out" / "see_cmp.png"))
print("wrote out/see_cmp.png  (src | layer composite | diff heat)")
print("per-part own coverage:")
for n in m.order:
    print("   %-12s %7d px" % (n, int((m.parts[n].img[..., 3] > 0.03).sum())))
