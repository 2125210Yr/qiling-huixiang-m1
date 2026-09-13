import json, os, re, sys, urllib.request
PROXY="http://127.0.0.1:7890"
DEST=r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve"
handler=urllib.request.ProxyHandler({"http":PROXY,"https":PROXY})
opener=urllib.request.build_opener(handler)
urllib.request.install_opener(opener)
UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"

def log(m):
    print(m, flush=True)
    with open(os.path.join(DEST,"FETCH_LOG.txt"),"a",encoding="utf-8") as f:
        f.write(m+"\n")

def fetch_player(video_id):
    url=f"https://www.youtube.com/watch?v={video_id}&bpctr=9999999999&has_verified=1"
    req=urllib.request.Request(url, headers={"User-Agent":UA,"Accept-Language":"en-US,en;q=0.9"})
    html=urllib.request.urlopen(req, timeout=45).read().decode("utf-8","replace")
    m=re.search(r"ytInitialPlayerResponse\s*=\s*(\{.+?\})\s*;", html)
    if not m:
        m=re.search(r"var ytInitialPlayerResponse\s*=\s*(\{.+?\});", html)
    if not m:
        raise RuntimeError("ytInitialPlayerResponse not found")
    return json.loads(m.group(1))

def pick(player):
    sd=player.get("streamingData") or {}
    formats=(sd.get("formats") or [])+(sd.get("adaptiveFormats") or [])
    # progressive mp4 with audio
    prog=[]
    for f in formats:
        if not f.get("url"):
            continue
        mt=str(f.get("mimeType") or "")
        if mt.startswith("video/mp4") and "audioQuality" in f:  # progressive usually has audioQuality? actually progressive has both in one
            prog.append(f)
        elif mt.startswith("video/mp4") and f.get("audioChannels"):
            prog.append(f)
    # classic progressive: has both video and audio in formats (not adaptive)
    prog2=[f for f in (sd.get("formats") or []) if f.get("url") and "video/mp4" in str(f.get("mimeType",""))]
    cand=prog2 or prog
    cand=sorted(cand, key=lambda f: int(f.get("height") or 0))
    for f in cand:
        h=int(f.get("height") or 0)
        if h and h<=480:
            return f
    return cand[-1] if cand else None

def download(url, path):
    req=urllib.request.Request(url, headers={"User-Agent":UA})
    with urllib.request.urlopen(req, timeout=180) as r, open(path,"wb") as out:
        n=0
        while True:
            b=r.read(256*1024)
            if not b: break
            out.write(b); n+=len(b)
            if n and n%(8*1024*1024)<256*1024:
                print(f"  bytes={n}", flush=True)
    return n

items=[
 ("aSbBuFD12HY","aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
 ("hgqXY5M9gFk","hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
 ("IRDqNAhKKr4","IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]
log("==== WATCHPAGE_FETCH_START ====")
for vid,name in items:
    path=os.path.join(DEST,name)
    if os.path.exists(path) and os.path.getsize(path)>100000:
        log(f"EXISTS {vid} {os.path.getsize(path)}"); continue
    try:
        log(f"WATCH {vid}")
        player=fetch_player(vid)
        st=(player.get("playabilityStatus") or {}).get("status")
        title=(player.get("videoDetails") or {}).get("title")
        log(f"status={st} title={title}")
        f=pick(player)
        if not f:
            log(f"NO_FMT {vid} streamingKeys={list((player.get('streamingData') or {}).keys())}")
            continue
        log(f"fmt height={f.get('height')} q={f.get('qualityLabel')} mime={f.get('mimeType')}")
        size=download(f["url"], path)
        log(f"SUCCESS {vid} size={size}")
    except Exception as e:
        log(f"FAIL {vid} {type(e).__name__}: {e}")
log("==== WATCHPAGE_FETCH_END ====")
for vid,name in items:
    p=os.path.join(DEST,name)
    if os.path.exists(p):
        log(f"ROOT {os.path.getsize(p)} {name}")
