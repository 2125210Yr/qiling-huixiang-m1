import asyncio
import base64
import http.cookiejar
import json
import traceback
import urllib.request
from pathlib import Path

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_cdp_inject_fetch.log"
COOKIES = json.loads((DEST / "_yt_cookies_min.json").read_text(encoding="utf-8"))

TARGETS = [
    ("aSbBuFD12HY", "aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
    ("hgqXY5M9gFk", "hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
    ("IRDqNAhKKr4", "IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


def build_opener():
    cj = http.cookiejar.MozillaCookieJar(str(DEST / "_yt_cookies.txt"))
    cj.load(ignore_discard=True, ignore_expires=True)
    return urllib.request.build_opener(
        urllib.request.ProxyHandler(
            {"http": "http://127.0.0.1:7890", "https": "http://127.0.0.1:7890"}
        ),
        urllib.request.HTTPCookieProcessor(cj),
    )


async def main() -> None:
    import websockets

    LOG.write_text("START\n", encoding="utf-8")
    tabs = json.loads(
        urllib.request.urlopen("http://127.0.0.1:9222/json/list", timeout=5).read()
    )
    pages = [t for t in tabs if t.get("type") == "page"]
    page = pages[0]
    w(f"attach {page.get('url')}")

    async with websockets.connect(page["webSocketDebuggerUrl"], max_size=50_000_000) as ws:
        mid = 0

        async def call(method, params=None, timeout=180):
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

        ok = 0
        for c in COOKIES:
            domain = c["domain"]
            params = {
                "name": c["name"],
                "value": c["value"],
                "domain": domain,
                "path": c.get("path") or "/",
                "secure": bool(c.get("secure")),
                "httpOnly": bool(c.get("httpOnly")),
                "url": "https://www.youtube.com/"
                if "youtube" in domain
                else "https://www.google.com/",
            }
            if c.get("expires"):
                params["expires"] = float(c["expires"])
            res = await call("Network.setCookie", params)
            if (res.get("result") or {}).get("success"):
                ok += 1
        w(f"cookies_set_ok={ok}/{len(COOKIES)}")

        opener = build_opener()

        for vid, outname in TARGETS:
            out = DEST / outname
            if out.exists() and out.stat().st_size > 500_000:
                w(f"SKIP exists {out.name} size={out.stat().st_size}")
                continue
            w(f"=== {vid} ===")
            try:
                await call(
                    "Page.navigate", {"url": f"https://www.youtube.com/watch?v={vid}"}
                )
                await asyncio.sleep(10)
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
  if(!pr) return {missing:true};
  const st=pr.playabilityStatus||{};
  const sd=pr.streamingData||{};
  const all=(sd.formats||[]).concat(sd.adaptiveFormats||[]);
  return {
    status: st.status, reason: st.reason,
    formats: (sd.formats||[]).length, adaptive:(sd.adaptiveFormats||[]).length,
    urlN: all.filter(f=>f.url).length,
    title:(pr.videoDetails||{}).title,
    urls: all.filter(f=>f.url).map(f=>({itag:f.itag,height:f.height,mime:f.mimeType,url:f.url,clen:f.contentLength,fps:f.fps}))
  };
})()""",
                        "returnByValue": True,
                    },
                )
                val = res["result"]["result"]["value"]
                w(
                    json.dumps(
                        {
                            k: val.get(k)
                            for k in (
                                "status",
                                "reason",
                                "formats",
                                "adaptive",
                                "urlN",
                                "title",
                            )
                        },
                        ensure_ascii=False,
                    )
                )
                if not val.get("urlN"):
                    w("NO STREAM")
                    continue
                cands = val["urls"]
                # Prefer progressive mp4 (has audio) — typically itag 18
                prog = [
                    c
                    for c in cands
                    if c.get("height")
                    and "video/mp4" in (c.get("mime") or "")
                    and "avc1" in (c.get("mime") or "")
                    and "mp4a" in (c.get("mime") or "")
                ]
                if not prog:
                    prog = [
                        c
                        for c in cands
                        if c.get("height") and "video/mp4" in (c.get("mime") or "")
                    ]
                prog = sorted(
                    prog,
                    key=lambda x: (
                        0 if (x.get("height") or 999) <= 480 else 1,
                        abs((x.get("height") or 999) - 360),
                    ),
                )
                pick = prog[0] if prog else cands[0]
                w(
                    f"pick itag={pick.get('itag')} h={pick.get('height')} mime={pick.get('mime')} clen={pick.get('clen')}"
                )

                # Download via page fetch to inherit cookies / referer (chunked to avoid huge CDP payload)
                # Fallback: urllib with cookie jar
                try:
                    req = urllib.request.Request(
                        pick["url"],
                        headers={
                            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/155.0.0.0 Safari/537.36",
                            "Referer": "https://www.youtube.com/",
                            "Origin": "https://www.youtube.com",
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
                            if total % (5 * 1024 * 1024) < 256 * 1024:
                                w(f"  {total}")
                    w(f"SAVED {out.name} size={out.stat().st_size}")
                except Exception as e:
                    w(f"urllib fail: {type(e).__name__}: {e}")
                    # Browser-side download using Fetch + streaming to file via multiple evaluate reads is hard;
                    # use CDP Browser.setDownloadBehavior + navigate to url
                    w("try CDP download")
                    dl_dir = str(DEST / "_chrome_dl")
                    Path(dl_dir).mkdir(exist_ok=True)
                    await call(
                        "Browser.setDownloadBehavior",
                        {"behavior": "allow", "downloadPath": dl_dir, "eventsEnabled": True},
                    )
                    # open url in same tab briefly
                    await call("Page.navigate", {"url": pick["url"]})
                    await asyncio.sleep(30)
                    files = list(Path(dl_dir).glob("*"))
                    w(f"dl files={[f.name+':'+str(f.stat().st_size) for f in files]}")
                    if files:
                        biggest = max(files, key=lambda f: f.stat().st_size)
                        biggest.replace(out)
                        w(f"SAVED via CDP {out.name} size={out.stat().st_size}")
            except Exception:
                w("FAIL " + traceback.format_exc())
        w("ALL_DONE")


if __name__ == "__main__":
    asyncio.run(main())
