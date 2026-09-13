import asyncio
import json
import urllib.request
from pathlib import Path

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_cdp_netcapture.log"

TARGETS = [
    ("hgqXY5M9gFk", "hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
    ("aSbBuFD12HY", "aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
    ("IRDqNAhKKr4", "IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


async def main() -> None:
    import websockets

    LOG.write_text("START\n", encoding="utf-8")
    # Prefer Playwright cookies already in Chrome work profile; just capture network
    tabs = json.loads(
        urllib.request.urlopen("http://127.0.0.1:9222/json/list", timeout=5).read()
    )
    pages = [t for t in tabs if t.get("type") == "page"]
    page = pages[0]
    w(f"attach {page.get('url')}")

    async with websockets.connect(page["webSocketDebuggerUrl"], max_size=50_000_000) as ws:
        mid = 0
        pending = {}
        events = []

        async def reader():
            async for raw in ws:
                msg = json.loads(raw)
                if "id" in msg and msg["id"] in pending:
                    pending[msg["id"]].set_result(msg)
                else:
                    events.append(msg)

        async def call(method, params=None, timeout=120):
            nonlocal mid
            mid += 1
            fut = asyncio.get_event_loop().create_future()
            pending[mid] = fut
            await ws.send(json.dumps({"id": mid, "method": method, "params": params or {}}))
            return await asyncio.wait_for(fut, timeout=timeout)

        task = asyncio.create_task(reader())
        await call("Network.enable")
        await call("Page.enable")
        await call("Runtime.enable")

        # Load cookies from Profile5 export into browser
        cookies = json.loads((DEST / "_yt_cookies_min.json").read_text(encoding="utf-8"))
        ok = 0
        for c in cookies:
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
        w(f"cookies_ok={ok}/{len(cookies)}")

        for vid, outname in TARGETS:
            out = DEST / outname
            if out.exists() and out.stat().st_size > 500_000:
                w(f"SKIP {out.name}")
                continue
            w(f"=== {vid} ===")
            events.clear()
            await call("Page.navigate", {"url": f"https://www.youtube.com/watch?v={vid}"})
            await asyncio.sleep(6)
            await call(
                "Runtime.evaluate",
                {
                    "expression": """(() => {
  const v=document.querySelector('video');
  if(v){ v.muted=true; v.play?.(); }
  return {src:(v&&(v.currentSrc||v.src))||'', ready:v&&v.readyState};
})()""",
                    "returnByValue": True,
                },
            )
            # let it buffer
            await asyncio.sleep(15)
            # seek to force more requests
            await call(
                "Runtime.evaluate",
                {
                    "expression": """(() => {
  const v=document.querySelector('video');
  if(v && v.duration){ v.currentTime = Math.min(20, v.duration/3); }
  return v ? {t:v.currentTime, d:v.duration, rs:v.readyState} : null;
})()""",
                    "returnByValue": True,
                },
            )
            await asyncio.sleep(10)

            # Collect googlevideo URLs from network
            seen = []
            req_id_for_url = {}
            for ev in events:
                method = ev.get("method")
                params = ev.get("params") or {}
                if method == "Network.requestWillBeSent":
                    req = params.get("request") or {}
                    u = req.get("url") or ""
                    if "googlevideo.com" in u and "videoplayback" in u:
                        rid = params.get("requestId")
                        seen.append(u)
                        req_id_for_url[u] = rid
                if method == "Network.responseReceived":
                    resp = params.get("response") or {}
                    u = resp.get("url") or ""
                    if "googlevideo.com" in u and "videoplayback" in u:
                        w(
                            f"resp status={resp.get('status')} mime={resp.get('mimeType')} len={resp.get('headers',{}).get('content-length') or resp.get('headers',{}).get('Content-Length')} url={u[:120]}"
                        )

            w(f"gv_requests={len(seen)}")
            if not seen:
                st = await call(
                    "Runtime.evaluate",
                    {
                        "expression": """(() => {
  const pr=window.ytInitialPlayerResponse||{};
  return {status:(pr.playabilityStatus||{}).status, title:(pr.videoDetails||{}).title};
})()""",
                        "returnByValue": True,
                    },
                )
                w("player " + json.dumps(st["result"]["result"]["value"], ensure_ascii=False))
                continue

            # Prefer itag=18 or largest content-length successful response; use first unique URL with itag=18
            pick = None
            for u in seen:
                if "itag=18" in u:
                    pick = u
                    break
            if not pick:
                pick = seen[0]
            w(f"capture_url itag18={'itag=18' in pick} len={len(pick)}")

            # Download using Page.evaluate fetch of the CAPTURED url (already n-signed)
            # Stream via repeated Range? First try getResponseBody if finished
            rid = req_id_for_url.get(pick)
            body = None
            if rid:
                try:
                    # wait for loadingFinished
                    await asyncio.sleep(2)
                    res = await call("Network.getResponseBody", {"requestId": rid})
                    result = res.get("result") or {}
                    if result.get("base64Encoded"):
                        import base64

                        body = base64.b64decode(result.get("body") or "")
                    else:
                        body = (result.get("body") or "").encode("utf-8", "replace")
                    w(f"getResponseBody bytes={len(body) if body else 0}")
                except Exception as e:
                    w(f"getResponseBody fail: {e}")

            if not body or len(body) < 100_000:
                # in-page fetch of captured URL
                w("page fetch captured url")
                import base64

                meta = await call(
                    "Runtime.evaluate",
                    {
                        "expression": f"""
(async () => {{
  const url = {json.dumps(pick)};
  const r = await fetch(url);
  if (!r.ok) return {{err: r.status}};
  const buf = new Uint8Array(await r.arrayBuffer());
  const chunk = 0x8000;
  let s = '';
  for (let i = 0; i < buf.length; i += chunk) {{
    s += String.fromCharCode.apply(null, buf.subarray(i, Math.min(i+chunk, buf.length)));
  }}
  return {{n: buf.length, b64: btoa(s)}};
}})()
""",
                        "awaitPromise": True,
                        "returnByValue": True,
                    },
                    timeout=600,
                )
                val = ((meta.get("result") or {}).get("result") or {}).get("value") or {}
                if val.get("err"):
                    w(f"fetch err {val}")
                    continue
                body = base64.b64decode(val.get("b64") or "")
                w(f"fetched n={val.get('n')} body={len(body)}")

            if body and len(body) > 50_000:
                out.write_bytes(body)
                w(f"SAVED {out.name} size={out.stat().st_size}")
            else:
                w("body too small")
        task.cancel()
        w("ALL_DONE")


if __name__ == "__main__":
    asyncio.run(main())
