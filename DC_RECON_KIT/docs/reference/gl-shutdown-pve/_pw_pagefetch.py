import base64
import json
import traceback
from pathlib import Path

from playwright.sync_api import sync_playwright

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_pw_pagefetch.log"
COOKIES = json.loads((DEST / "_yt_cookies_min.json").read_text(encoding="utf-8"))

# shortest first
TARGETS = [
    ("hgqXY5M9gFk", "hgqXY5M9gFk_GL_ND_Robin_Boss.mp4"),
    ("aSbBuFD12HY", "aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4"),
    ("IRDqNAhKKr4", "IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"),
]

CHUNK = 256 * 1024


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


def page_fetch_to_file(page, url: str, out: Path) -> int:
    """Download url via page.fetch (browser proxy/cookies) in Range chunks."""
    meta = page.evaluate(
        """async (url) => {
  const head = await fetch(url, { method: 'GET', headers: { Range: 'bytes=0-0' } });
  const cr = head.headers.get('content-range') || '';
  const cl = head.headers.get('content-length');
  let total = null;
  const m = /\\/(\\d+)$/.exec(cr);
  if (m) total = parseInt(m[1], 10);
  else if (cl) total = parseInt(cl, 10);
  return { status: head.status, total, cr, ok: head.ok };
}""",
        url,
    )
    w(f"range probe {meta}")
    total = meta.get("total")
    if not total:
        # full download in one shot via arrayBuffer -> base64 may OOM; try streaming in page to data URL no
        # fallback: single fetch as base64 if small
        b64 = page.evaluate(
            """async (url) => {
  const r = await fetch(url);
  if (!r.ok) return { err: r.status };
  const buf = new Uint8Array(await r.arrayBuffer());
  const chunk = 0x8000;
  let s = '';
  for (let i = 0; i < buf.length; i += chunk) {
    s += String.fromCharCode.apply(null, buf.subarray(i, Math.min(i + chunk, buf.length)));
  }
  return { b64: btoa(s), n: buf.length };
}""",
            url,
        )
        if b64.get("err"):
            raise RuntimeError(f"fetch status {b64['err']}")
        data = base64.b64decode(b64["b64"])
        out.write_bytes(data)
        return len(data)

    with open(out, "wb") as fo:
        offset = 0
        while offset < total:
            end = min(offset + CHUNK - 1, total - 1)
            part = page.evaluate(
                """async ({url, start, end}) => {
  const r = await fetch(url, { headers: { Range: `bytes=${start}-${end}` } });
  if (!(r.ok || r.status === 206)) return { err: r.status };
  const buf = new Uint8Array(await r.arrayBuffer());
  const chunk = 0x8000;
  let s = '';
  for (let i = 0; i < buf.length; i += chunk) {
    s += String.fromCharCode.apply(null, buf.subarray(i, Math.min(i + chunk, buf.length)));
  }
  return { b64: btoa(s), n: buf.length };
}""",
                {"url": url, "start": offset, "end": end},
            )
            if part.get("err"):
                raise RuntimeError(f"range {offset}-{end} status {part['err']}")
            fo.write(base64.b64decode(part["b64"]))
            offset = end + 1
            if offset == end + 1 and offset % (2 * 1024 * 1024) < CHUNK:
                w(f"  {offset}/{total}")
            elif offset % (2 * 1024 * 1024) < CHUNK:
                w(f"  {offset}/{total}")
    return total


def main() -> None:
    LOG.write_text("START\n", encoding="utf-8")
    with sync_playwright() as p:
        browser = p.chromium.connect_over_cdp("http://127.0.0.1:9222")
        context = browser.contexts[0]
        pw_cookies = []
        for c in COOKIES:
            item = {
                "name": c["name"],
                "value": c["value"],
                "domain": c["domain"],
                "path": c.get("path") or "/",
                "secure": bool(c.get("secure")),
                "httpOnly": bool(c.get("httpOnly")),
            }
            if c.get("expires"):
                item["expires"] = float(c["expires"])
            pw_cookies.append(item)
        context.add_cookies(pw_cookies)
        page = context.pages[0] if context.pages else context.new_page()
        w(f"pages={len(context.pages)}")

        for vid, outname in TARGETS:
            out = DEST / outname
            if out.exists() and out.stat().st_size > 500_000:
                w(f"SKIP {out.name} size={out.stat().st_size}")
                continue
            w(f"=== {vid} ===")
            try:
                page.goto(
                    f"https://www.youtube.com/watch?v={vid}",
                    wait_until="domcontentloaded",
                    timeout=120000,
                )
                page.wait_for_timeout(8000)
                try:
                    page.evaluate("document.querySelector('video')?.play?.()")
                except Exception:
                    pass
                page.wait_for_timeout(2000)
                val = page.evaluate(
                    """() => {
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
    urls: all.filter(f=>f.url).map(f=>({itag:f.itag,height:f.height,mime:f.mimeType,url:f.url,clen:f.contentLength}))
  };
}"""
                )
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
                prog = [
                    c
                    for c in cands
                    if c.get("height")
                    and "video/mp4" in (c.get("mime") or "")
                    and "mp4a" in (c.get("mime") or "")
                ] or [
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
                w(f"pick itag={pick.get('itag')} h={pick.get('height')}")
                n = page_fetch_to_file(page, pick["url"], out)
                w(f"SAVED {out.name} size={out.stat().st_size} reported={n}")
            except Exception:
                w("FAIL " + traceback.format_exc())
        w("ALL_DONE")


if __name__ == "__main__":
    main()
