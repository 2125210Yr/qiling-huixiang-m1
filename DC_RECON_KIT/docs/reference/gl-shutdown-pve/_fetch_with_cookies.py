import http.cookiejar, json, re, urllib.request, sys
from pathlib import Path
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
DEST=Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG=DEST/"_fetch_with_cookies.log"
def w(m):
  with open(LOG,"a",encoding="utf-8") as f:
    f.write(m+"\n")
  print(m, flush=True)
LOG.write_text("START\n", encoding="utf-8")
PROXY="http://127.0.0.1:7890"
cj=http.cookiejar.MozillaCookieJar(str(DEST/"_yt_cookies.txt"))
cj.load(ignore_discard=True, ignore_expires=True)
w(f"cookies={len(list(cj))}")
opener=urllib.request.build_opener(
  urllib.request.ProxyHandler({"http":PROXY,"https":PROXY}),
  urllib.request.HTTPCookieProcessor(cj),
)
UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"

def player(vid):
  url=f"https://www.youtube.com/watch?v={vid}&hl=en"
  req=urllib.request.Request(url, headers={"User-Agent":UA,"Accept-Language":"en-US"})
  html=opener.open(req, timeout=45).read().decode("utf-8","replace")
  m=re.search(r"ytInitialPlayerResponse\s*=\s*(\{.+?\})\s*;", html)
  if not m:
    return None, f"no_player len={len(html)}"
  p=json.loads(m.group(1))
  return p, None

TARGETS=[
  ("hgqXY5M9gFk","hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
  ("aSbBuFD12HY","aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
  ("IRDqNAhKKr4","IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]

def pick_url(sd):
  fmts=(sd.get("formats") or []) + (sd.get("adaptiveFormats") or [])
  # prefer progressive mp4 with audio (formats usually)
  progressive=[]
  video_only=[]
  for f in fmts:
    u=f.get("url")
    if not u: 
      continue  # skip cipher for now
    mime=f.get("mimeType") or ""
    h=f.get("height") or 0
    if "video/mp4" in mime and "audio" not in (f.get("mimeType") or "") and f.get("audioQuality") is None and "mp4a" not in mime:
      # adaptive video-only
      if "avc1" in mime or "mp4" in mime:
        video_only.append((h,u,f))
    if f.get("audioQuality") or ("audio" in mime and "video" in mime) or (f.get("width") and f.get("audioSampleRate")):
      progressive.append((h,u,f))
    # classic formats list entries have both
    if f in (sd.get("formats") or []) and u:
      progressive.append((h or 0,u,f))
  # unique by url
  seen=set(); prog=[]
  for h,u,f in progressive:
    if u in seen: continue
    seen.add(u); prog.append((h,u,f))
  if prog:
    # prefer <=480 closest to 360
    prog.sort(key=lambda x: (0 if 1<= (x[0] or 0) <=480 else 1, abs((x[0] or 999)-360)))
    return prog[0]
  # fallback any with url
  anyu=[]
  for f in fmts:
    if f.get("url"):
      anyu.append((f.get("height") or 0, f["url"], f))
  if anyu:
    anyu.sort(key=lambda x: abs((x[0] or 999)-360))
    return anyu[0]
  return None

for vid, outname in TARGETS:
  w(f"=== {vid} ===")
  try:
    p, err = player(vid)
  except Exception as e:
    w(f"fetch err {e}"); continue
  if err:
    w(err); continue
  st=(p.get("playabilityStatus") or {}).get("status")
  title=((p.get("videoDetails") or {}).get("title") or "")[:80]
  w(f"status={st} title={title}")
  sd=p.get("streamingData") or {}
  w(f"formats={len(sd.get('formats') or [])} adaptive={len(sd.get('adaptiveFormats') or [])}")
  picked=pick_url(sd)
  if not picked:
    # dump whether signatureCipher present
    ciph=sum(1 for f in (sd.get("formats") or [])+(sd.get("adaptiveFormats") or []) if f.get("signatureCipher") or f.get("cipher"))
    w(f"NO url; cipher_entries={ciph}")
    continue
  h,u,f=picked
  w(f"pick height={h} itag={f.get('itag')} mime={(f.get('mimeType') or '')[:50]}")
  out=DEST/outname
  if out.exists() and out.stat().st_size>1_000_000:
    w(f"already {out.name} size={out.stat().st_size}"); continue
  req=urllib.request.Request(u, headers={"User-Agent":UA, "Referer":"https://www.youtube.com/"})
  w("download start")
  with opener.open(req, timeout=120) as resp, open(out, "wb") as fo:
    total=0
    while True:
      chunk=resp.read(1024*256)
      if not chunk: break
      fo.write(chunk); total+=len(chunk)
      if total and total % (5*1024*1024) < 256*1024:
        w(f"  wrote {total}")
  w(f"saved {out.name} size={out.stat().st_size}")
w("ALL_DONE")
