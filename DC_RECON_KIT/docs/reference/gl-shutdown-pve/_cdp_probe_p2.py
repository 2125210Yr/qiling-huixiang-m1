"""Probe existing YouTube tab on CDP 9222. Do not navigate away."""
import asyncio, json, urllib.request, base64
from pathlib import Path

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_cdp_probe_p2.log"


def w(m):
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


async def main():
    import websockets

    LOG.write_text("START\n", encoding="utf-8")
    tabs = json.loads(urllib.request.urlopen("http://127.0.0.1:9222/json/list", timeout=5).read())
    pages = [t for t in tabs if t.get("type") == "page" and "watch?v=" in (t.get("url") or "")]
    if not pages:
        w("NO_WATCH_TAB")
        for t in tabs:
            w(f"  {t.get('type')} {t.get('title')} {t.get('url')}")
        return
    # Prefer Eternal Vow / IRDq
    page = pages[0]
    for t in pages:
        if "IRDqNAhKKr4" in (t.get("url") or "") or "Eternal" in (t.get("title") or ""):
            page = t
            break
    w(f"attach {page.get('url')} | {page.get('title')}")
    async with websockets.connect(page["webSocketDebuggerUrl"], max_size=80_000_000) as ws:
        mid = 0
        pending = {}

        async def reader():
            async for raw in ws:
                msg = json.loads(raw)
                if "id" in msg and msg["id"] in pending:
                    pending[msg["id"]].set_result(msg)

        async def send(method, params=None, timeout=30):
            nonlocal mid
            mid += 1
            fut = asyncio.get_event_loop().create_future()
            pending[mid] = fut
            await ws.send(json.dumps({"id": mid, "method": method, "params": params or {}}))
            return await asyncio.wait_for(fut, timeout=timeout)

        task = asyncio.create_task(reader())
        await send("Page.enable")
        await send("Runtime.enable")
        res = await send(
            "Runtime.evaluate",
            {
                "expression": """
(() => {
  const pr = window.ytInitialPlayerResponse;
  const v = document.querySelector('video');
  const st = (pr && pr.playabilityStatus) || {};
  const vd = (pr && pr.videoDetails) || {};
  const sd = (pr && pr.streamingData) || {};
  const all = ((sd.formats||[]).concat(sd.adaptiveFormats||[]));
  const qs = [];
  try {
    const p = document.querySelector('#movie_player');
    if (p && p.getAvailableQualityLevels) qs.push(...p.getAvailableQualityLevels());
  } catch (e) {}
  return {
    title: document.title,
    href: location.href,
    cookieSID: document.cookie.includes('SID='),
    avatar: !!document.querySelector('#avatar-btn, button#avatar-btn'),
    signIn: [...document.querySelectorAll('a,button')].some(e => /登录|Sign in/i.test(e.innerText||'')),
    status: st.status,
    reason: st.reason,
    videoTitle: vd.title,
    lengthSec: vd.lengthSeconds,
    videoW: v && v.videoWidth,
    videoH: v && v.videoHeight,
    clientW: v && v.clientWidth,
    clientH: v && v.clientHeight,
    ready: v && v.readyState,
    paused: v && v.paused,
    current: v && v.currentTime,
    dur: v && v.duration,
    src: v && (v.currentSrc||v.src||'').slice(0,160),
    qualities: qs,
    formats: (sd.formats||[]).map(f=>({itag:f.itag,h:f.height,mime:(f.mimeType||'').slice(0,40),hasUrl:!!f.url})),
    adaptiveH: [...new Set((sd.adaptiveFormats||[]).map(f=>f.height).filter(Boolean))].sort((a,b)=>b-a)
  };
})()
""",
                "returnByValue": True,
            },
        )
        val = ((res.get("result") or {}).get("result") or {}).get("value")
        w(json.dumps(val, ensure_ascii=False, indent=2))
        shot = await send("Page.captureScreenshot", {"format": "png"})
        png = DEST / "_probe_p2_now.png"
        png.write_bytes(base64.b64decode(shot["result"]["data"]))
        w(f"shot {png} bytes={png.stat().st_size}")
        task.cancel()
    w("DONE")


asyncio.run(main())
