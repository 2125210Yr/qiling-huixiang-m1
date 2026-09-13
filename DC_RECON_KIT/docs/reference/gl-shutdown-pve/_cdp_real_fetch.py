import asyncio, json, urllib.request, base64
from pathlib import Path
DEST=Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG=DEST/"_cdp_real_fetch.log"
def w(m):
  with open(LOG,"a",encoding="utf-8") as f: f.write(m+"\n")
  print(m, flush=True)
LOG.write_text("START\n", encoding="utf-8")

TARGETS=[
  ("aSbBuFD12HY","aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
  ("hgqXY5M9gFk","hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
  ("IRDqNAhKKr4","IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]

async def main():
  import websockets
  tabs=json.loads(urllib.request.urlopen("http://127.0.0.1:9222/json/list", timeout=5).read())
  pages=[t for t in tabs if t.get("type")=="page" and "youtube" in (t.get("url") or "")]
  if not pages:
    pages=[t for t in tabs if t.get("type")=="page"]
  page=pages[0]
  w(f"attach {page.get('url')} {page.get('title')}")
  async with websockets.connect(page["webSocketDebuggerUrl"], max_size=50_000_000) as ws:
    mid=0
    pending={}
    events=[]
    async def send(method, params=None, timeout=90):
      nonlocal mid
      mid+=1
      fut=asyncio.get_event_loop().create_future()
      pending[mid]=fut
      await ws.send(json.dumps({"id":mid,"method":method,"params":params or {}}))
      return await asyncio.wait_for(fut, timeout=timeout)
    async def reader():
      async for raw in ws:
        msg=json.loads(raw)
        if "id" in msg and msg["id"] in pending:
          pending[msg["id"]].set_result(msg)
        else:
          events.append(msg)
    task=asyncio.create_task(reader())
    await send("Network.enable")
    await send("Page.enable")
    await send("Runtime.enable")
    # check login on home
    home=await send("Runtime.evaluate", {"expression":"""
(() => {
  const avatar = !!document.querySelector('#avatar-btn, button#avatar-btn, img#img');
  const signIn = [...document.querySelectorAll('a,button')].some(e => (e.innerText||'').includes('登录') || (e.innerText||'').includes('Sign in'));
  return {title: document.title, avatarLikely: avatar, hasSignInText: signIn, cookieSID: document.cookie.includes('SID=')};
})()
""", "returnByValue": True})
    w("home "+json.dumps(((home.get("result") or {}).get("result") or {}).get("value"), ensure_ascii=False))

    proxy=urllib.request.ProxyHandler({"http":"http://127.0.0.1:7890","https":"http://127.0.0.1:7890"})
    opener=urllib.request.build_opener(proxy)

    for vid, outname in TARGETS:
      w(f"=== {vid} ===")
      events.clear()
      await send("Page.navigate", {"url": f"https://www.youtube.com/watch?v={vid}"})
      await asyncio.sleep(8)
      # dismiss dialogs: click 取消 if present
      await send("Runtime.evaluate", {"expression":"""
(() => {
  const btns=[...document.querySelectorAll('button, yt-button-shape button, tp-yt-paper-button')];
  for (const b of btns) {
    const t=(b.innerText||'').trim();
    if (t==='取消' || t==='Cancel') { b.click(); return 'clicked-cancel'; }
  }
  return 'no-cancel';
})()
""", "returnByValue": True})
      await asyncio.sleep(2)
      # try play
      await send("Runtime.evaluate", {"expression":"document.querySelector('video')?.play?.(); 'ok'", "returnByValue": True})
      await asyncio.sleep(5)
      res=await send("Runtime.evaluate", {"expression":"""
(() => {
  const pr = window.ytInitialPlayerResponse;
  const v = document.querySelector('video');
  if (!pr) return {missing:true, title:document.title, videoSrc: v && v.src};
  const st = pr.playabilityStatus || {};
  const sd = pr.streamingData || {};
  const all = (sd.formats||[]).concat(sd.adaptiveFormats||[]);
  return {
    title: (pr.videoDetails||{}).title || document.title,
    status: st.status,
    reason: st.reason,
    formats: (sd.formats||[]).length,
    adaptive: (sd.adaptiveFormats||[]).length,
    urls: all.filter(f=>f.url).map(f=>({itag:f.itag,height:f.height,mime:(f.mimeType||'').slice(0,50),url:f.url,clen:f.contentLength})),
    videoSrc: v && (v.currentSrc||v.src||''),
    signedInUI: !![...document.querySelectorAll('#avatar-btn, button#avatar-btn')].length
  };
})()
""", "returnByValue": True})
      val=((res.get("result") or {}).get("result") or {}).get("value")
      w(json.dumps({k:val[k] for k in val if k!='urls'}, ensure_ascii=False) if isinstance(val,dict) else str(val))
      if isinstance(val,dict):
        w(f"url_candidates={len(val.get('urls') or [])} videoSrc={str(val.get('videoSrc') or '')[:100]}")
      # network googlevideo
      gv=[]
      for ev in events:
        p=ev.get("params") or {}
        u=(p.get("request") or {}).get("url") or (p.get("response") or {}).get("url") or ""
        if "googlevideo.com" in u and "videoplayback" in u:
          gv.append(u)
      w(f"gv={len(gv)}")
      pick=None
      if isinstance(val,dict) and val.get("urls"):
        cands=val["urls"]
        # prefer progressive from formats: often have audioQuality
        prog=[c for c in cands if c.get("height") and 'audio' not in (c.get('mime') or '')]
        # actually formats with both audio+video have height and typically itag 18/22
        both=[c for c in cands if (c.get("height") or 0) in (144,240,360,480,720,1080)]
        pool=both or cands
        pool=sorted(pool, key=lambda x: (0 if (x.get("height") or 999)<=480 else 1, abs((x.get("height") or 999)-360)))
        pick=pool[0]["url"]
        w(f"pick itag={pool[0].get('itag')} h={pool[0].get('height')} mime={pool[0].get('mime')}")
      elif gv:
        pick=gv[0]
      if not pick:
        shot=await send("Page.captureScreenshot", {"format":"png"})
        (DEST/f"_cdp_fail_{vid}.png").write_bytes(base64.b64decode(shot["result"]["data"]))
        w("NO STREAM shot saved")
        continue
      out=DEST/outname
      w(f"downloading {out.name}")
      req=urllib.request.Request(pick, headers={
        "User-Agent":"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36",
        "Referer":"https://www.youtube.com/",
      })
      with opener.open(req, timeout=300) as resp, open(out,"wb") as fo:
        total=0
        while True:
          chunk=resp.read(256*1024)
          if not chunk: break
          fo.write(chunk); total+=len(chunk)
          if total and total%(10*1024*1024)<256*1024:
            w(f"  {total}")
      w(f"SAVED {out.name} size={out.stat().st_size}")
    task.cancel()
  w("ALL_DONE")

asyncio.run(main())
