import asyncio
import base64
import json
import urllib.request
from pathlib import Path

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_cdp_sysproxy.log"


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


TARGETS = [
    ("aSbBuFD12HY", "aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
    ("hgqXY5M9gFk", "hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
    ("IRDqNAhKKr4", "IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]


async def main() -> None:
    import websockets

    LOG.write_text("START\n", encoding="utf-8")
    tabs = json.loads(
        urllib.request.urlopen("http://127.0.0.1:9222/json/list", timeout=5).read()
    )
    pages = [
        t
        for t in tabs
        if t.get("type") == "page" and "youtube" in (t.get("url") or "")
    ]
    if not pages:
        pages = [t for t in tabs if t.get("type") == "page"]
    page = pages[0]
    w(f"attach {page.get('url')} | {page.get('title')}")

    async with websockets.connect(page["webSocketDebuggerUrl"], max_size=50_000_000) as ws:
        mid = 0

        async def call(method, params=None, timeout=90):
            nonlocal mid
            mid += 1
            await ws.send(json.dumps({"id": mid, "method": method, "params": params or {}}))
            while True:
                msg = json.loads(await asyncio.wait_for(ws.recv(), timeout=timeout))
                if msg.get("id") == mid:
                    return msg

        await call("Network.enable")
        await call("Page.enable")
        await call("Runtime.enable")

        home = await call(
            "Runtime.evaluate",
            {
                "expression": """(() => ({
  title: document.title,
  avatar: !!document.querySelector('#avatar-btn'),
  signIn: [...document.querySelectorAll('a,button')].some(e=>/登录|Sign in/.test((e.innerText||'').trim())),
  sid: document.cookie.includes('SID='),
  cookieLen: document.cookie.length
}))()""",
                "returnByValue": True,
            },
        )
        w("home " + json.dumps(home["result"]["result"]["value"], ensure_ascii=False))

        opener = urllib.request.build_opener(
            urllib.request.ProxyHandler(
                {"http": "http://127.0.0.1:7890", "https": "http://127.0.0.1:7890"}
            )
        )

        for vid, outname in TARGETS:
            w(f"=== {vid} ===")
            await call("Page.navigate", {"url": f"https://www.youtube.com/watch?v={vid}"})
            await asyncio.sleep(10)
            await call(
                "Runtime.evaluate",
                {
                    "expression": """(() => {
  for (const b of document.querySelectorAll('button')) {
    const t=(b.innerText||'').trim();
    if (t==='取消' || t==='Cancel') { b.click(); return 'cancel'; }
  }
  return 'none';
})()""",
                    "returnByValue": True,
                },
            )
            await asyncio.sleep(2)
            await call(
                "Runtime.evaluate",
                {
                    "expression": "document.querySelector('video')?.play?.(); 'play'",
                    "returnByValue": True,
                },
            )
            await asyncio.sleep(4)
            res = await call(
                "Runtime.evaluate",
                {
                    "expression": """(() => {
  const pr=window.ytInitialPlayerResponse;
  const v=document.querySelector('video');
  if(!pr) return {missing:true, title:document.title};
  const st=pr.playabilityStatus||{};
  const sd=pr.streamingData||{};
  const all=(sd.formats||[]).concat(sd.adaptiveFormats||[]);
  return {
    avatar: !!document.querySelector('#avatar-btn'),
    status: st.status, reason: st.reason,
    formats: (sd.formats||[]).length, adaptive:(sd.adaptiveFormats||[]).length,
    urlN: all.filter(f=>f.url).length,
    videoSrc: (v&&(v.currentSrc||v.src))||'',
    title:(pr.videoDetails||{}).title,
    urls: all.filter(f=>f.url).map(f=>({itag:f.itag,height:f.height,mime:(f.mimeType||'').slice(0,50),url:f.url,clen:f.contentLength}))
  };
})()""",
                    "returnByValue": True,
                },
            )
            val = res["result"]["result"]["value"]
            summary = {k: val.get(k) for k in ("avatar", "status", "reason", "formats", "adaptive", "urlN", "title", "videoSrc")}
            w(json.dumps(summary, ensure_ascii=False))
            if not val.get("urlN"):
                shot = await call("Page.captureScreenshot", {"format": "png"})
                (DEST / f"_cdp_sysproxy_fail_{vid}.png").write_bytes(
                    base64.b64decode(shot["result"]["data"])
                )
                w("NO STREAM")
                continue
            cands = val["urls"]
            prog = [c for c in cands if c.get("height") and "video/mp4" in (c.get("mime") or "")]
            prog = sorted(
                prog,
                key=lambda x: (0 if (x.get("height") or 999) <= 480 else 1, abs((x.get("height") or 999) - 360)),
            )
            pick = prog[0] if prog else cands[0]
            w(f"pick itag={pick.get('itag')} h={pick.get('height')} mime={pick.get('mime')}")
            out = DEST / outname
            req = urllib.request.Request(
                pick["url"],
                headers={
                    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36",
                    "Referer": "https://www.youtube.com/",
                },
            )
            with opener.open(req, timeout=300) as resp, open(out, "wb") as fo:
                total = 0
                while True:
                    chunk = resp.read(256 * 1024)
                    if not chunk:
                        break
                    fo.write(chunk)
                    total += len(chunk)
                    if total % (10 * 1024 * 1024) < 256 * 1024:
                        w(f"  {total}")
            w(f"SAVED {out.name} size={out.stat().st_size}")
        w("ALL_DONE")


if __name__ == "__main__":
    asyncio.run(main())
