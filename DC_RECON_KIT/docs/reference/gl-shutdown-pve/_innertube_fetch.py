import json, os, sys, urllib.request, urllib.error, ssl
PROXY="http://127.0.0.1:7890"
DEST=r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve"
handler=urllib.request.ProxyHandler({"http":PROXY,"https":PROXY})
opener=urllib.request.build_opener(handler)
urllib.request.install_opener(opener)

def post(url, data, headers):
    req=urllib.request.Request(url, data=json.dumps(data).encode(), headers=headers, method="POST")
    with urllib.request.urlopen(req, timeout=40) as r:
        return json.load(r)

def get_android_player(video_id):
    # Android innertube
    body={
      "context":{"client":{"clientName":"ANDROID","clientVersion":"19.09.37","androidSdkVersion":30,"hl":"en","gl":"US"}},
      "videoId":video_id,
      "contentCheckOk":True,
      "racyCheckOk":True
    }
    headers={
      "User-Agent":"com.google.android.youtube/19.09.37 (Linux; U; Android 11) gzip",
      "Content-Type":"application/json",
      "X-YouTube-Client-Name":"3",
      "X-YouTube-Client-Version":"19.09.37",
    }
    url="https://www.youtube.com/youtubei/v1/player?key=AIzaSyA8eiZmM1FaDVzWBN4bPUGFwVzE9HkWxxA&prettyPrint=false"
    return post(url, body, headers)

def pick_url(player):
    streaming=player.get("streamingData") or {}
    formats=(streaming.get("formats") or []) + (streaming.get("adaptiveFormats") or [])
    # prefer progressive mp4 <=480
    progressive=[f for f in formats if f.get("url") and str(f.get("mimeType","")).startswith("video/mp4") and f.get("audioQuality")]
    progressive=sorted(progressive, key=lambda f: int(f.get("height") or 0))
    for f in progressive:
        h=int(f.get("height") or 0)
        if 1 <= h <= 480:
            return f["url"], f.get("height"), f.get("qualityLabel")
    if progressive:
        f=progressive[-1]
        return f["url"], f.get("height"), f.get("qualityLabel")
    # fallback any with url
    for f in formats:
        if f.get("url") and "video" in str(f.get("mimeType","")):
            return f["url"], f.get("height"), f.get("qualityLabel")
    return None, None, None

def download(url, path):
    req=urllib.request.Request(url, headers={"User-Agent":"Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=120) as r, open(path,"wb") as out:
        total=0
        while True:
            chunk=r.read(1024*256)
            if not chunk: break
            out.write(chunk)
            total += len(chunk)
            if total % (5*1024*1024) < 256*1024:
                print(f"wrote {total}", flush=True)
    return total

items=[
 ("aSbBuFD12HY","aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
 ("hgqXY5M9gFk","hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
 ("IRDqNAhKKr4","IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]
os.makedirs(DEST, exist_ok=True)
log_path=os.path.join(DEST,"FETCH_LOG.txt")
def log(m):
    print(m, flush=True)
    with open(log_path,"a",encoding="utf-8") as f:
        f.write(m+"\n")

log("==== INNERTUBE_FETCH_START ====")
for vid,name in items:
    out=os.path.join(DEST,name)
    if os.path.exists(out) and os.path.getsize(out)>100000:
        log(f"EXISTS {vid} size={os.path.getsize(out)}")
        continue
    try:
        log(f"PLAYER {vid}")
        player=get_android_player(vid)
        status=((player.get("playabilityStatus") or {}).get("status"))
        title=((player.get("videoDetails") or {}).get("title"))
        log(f"status={status} title={title}")
        url,h,q=pick_url(player)
        if not url:
            log(f"NO_URL {vid} keys={list((player.get('streamingData') or {}).keys())}")
            # dump status reason
            log(str(player.get("playabilityStatus"))[:500])
            continue
        log(f"URL_OK height={h} q={q}")
        size=download(url,out)
        log(f"SUCCESS {vid} size={size}")
    except Exception as e:
        log(f"FAIL {vid} {type(e).__name__}: {e}")
log("==== INNERTUBE_FETCH_END ====")
for vid,name in items:
    p=os.path.join(DEST,name)
    if os.path.exists(p):
        log(f"ROOT {os.path.getsize(p)} {name}")
