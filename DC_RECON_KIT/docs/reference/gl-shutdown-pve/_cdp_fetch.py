import asyncio, json, urllib.request
from pathlib import Path
DEST=Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG=DEST/"_cdp_fetch.log"
def w(m):
  with open(LOG,"a",encoding="utf-8") as f: f.write(m+"\n")
  print(m, flush=True)
LOG.write_text("START\n", encoding="utf-8")

TARGETS=[
  ("hgqXY5M9gFk","hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
  ("aSbBuFD12HY","aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
  ("IRDqNAhKKr4","IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]

async def main():
  import websockets
  tabs=json.loads(urllib.request.urlopen("http://127.0.0.1:9223/json/list", timeout=5).read())
  page=[t for t in tabs if t.get("type")=="page"][0]
  wsurl=page["webSocketDebuggerUrl"]
  w(f"attach {page['id']}")
  async with websockets.connect(wsurl, max_size=50_000_000) as ws:
    msg_id=0
    pending={}
    events=[]
    async def send(method, params=None):
      nonlocal msg_id
      msg_id+=1
      mid=msg_id
      fut=asyncio.get_event_loop().create_future()
      pending[mid]=fut
      await ws.send(json.dumps({"id":mid,"method":method,"params":params or {}}))
      return await asyncio.wait_for(fut, timeout=60)
    async def reader():
      async for raw in ws:
        msg=json.loads(raw)
        if "id" in msg and msg["id"] in pending:
          pending[msg["id"]].set_result(msg)
        elif msg.get("method"):
          events.append(msg)
    task=asyncio.create_task(reader())
    await send("Network.enable")
    await send("Page.enable")
    await send("Runtime.enable")
    for vid, outname in TARGETS:
      w(f"=== {vid} ===")
      events.clear()
      await send("Page.navigate", {"url": f"https://www.youtube.com/watch?v={vid}"})
      # wait for load / player
      for i in range(30):
        await asyncio.sleep(1)
        # evaluate player
      res=await send("Runtime.evaluate", {"expression": """
(() => {
  const pr = window.ytInitialPlayerResponse;
  if (!pr) return {missing:true, title: document.title};
  const st = pr.playabilityStatus || {};
  const sd = pr.streamingData || {};
  const all = (sd.formats||[]).concat(sd.adaptiveFormats||[]);
  return {
    title: (pr.videoDetails||{}).title || document.title,
    status: st.status,
    reason: st.reason,
    formats: (sd.formats||[]).length,
    adaptive: (sd.adaptiveFormats||[]).length,
    urls: all.filter(f=>f.url).map(f=>({itag:f.itag,height:f.height,mime:(f.mimeType||'').slice(0,40),url:f.url})),
    signedIn: !!(document.cookie||'').includes('SID=')
  };
})()
""", "returnByValue": True})
      val=((res.get("result") or {}).get("result") or {}).get("value")
      w(json.dumps(val, ensure_ascii=False)[:800] if val else str(res)[:400])
      # collect googlevideo from network events
      gv=[]
      for ev in events:
        if ev.get("method") in ("Network.responseReceived","Network.requestWillBeSent"):
          p=ev.get("params") or {}
          req=p.get("request") or {}
          resp=p.get("response") or {}
          u=req.get("url") or resp.get("url") or ""
          if "googlevideo.com" in u and "videoplayback" in u:
            gv.append(u)
      w(f"googlevideo hits={len(gv)}")
      urls=[]
      if val and val.get("urls"):
        urls=[u["url"] for u in val["urls"] if u.get("url")]
      if not urls and gv:
        urls=[gv[0]]
      if not urls:
        w("NO STREAM")
        continue
      # pick first progressive-ish (prefer ones with mime video/mp4 from val)
      pick=None
      if val and val.get("urls"):
        cands=val["urls"]
        # prefer height<=480 with video/mp4
        cands=sorted(cands, key=lambda x: (0 if (x.get("height") or 999)<=480 else 1, abs((x.get("height") or 999)-360)))
        pick=cands[0]["url"]
      else:
        pick=urls[0]
      out=DEST/outname
      w(f"download -> {out.name}")
      # download via urllib through proxy
      proxy=urllib.request.ProxyHandler({"http":"http://127.0.0.1:7890","https":"http://127.0.0.1:7890"})
      opener=urllib.request.build_opener(proxy)
      req=urllib.request.Request(pick, headers={
        "User-Agent":"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36",
        "Referer":"https://www.youtube.com/",
      })
      with opener.open(req, timeout=180) as resp, open(out,"wb") as fo:
        total=0
        while True:
          chunk=resp.read(256*1024)
          if not chunk: break
          fo.write(chunk); total+=len(chunk)
          if total % (8*1024*1024) < 256*1024:
            w(f"  {total} bytes")
      w(f"saved size={out.stat().st_size}")
    task.cancel()
  w("ALL_DONE")

asyncio.run(main())
