import json
import traceback
from pathlib import Path

from playwright.sync_api import sync_playwright

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_pw_cdp_fetch.log"
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


def main() -> None:
    LOG.write_text("START\n", encoding="utf-8")
    with sync_playwright() as p:
        browser = p.chromium.connect_over_cdp("http://127.0.0.1:9222")
        context = browser.contexts[0] if browser.contexts else browser.new_context()
        # refresh cookies on context
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
        w(f"cookies_added={len(pw_cookies)} contexts={len(browser.contexts)} pages={len(context.pages)}")

        page = context.pages[0] if context.pages else context.new_page()

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
                page.wait_for_timeout(3000)
                val = page.evaluate(
                    """() => {
  const pr=window.ytInitialPlayerResponse;
  if(!pr) return {missing:true, title:document.title};
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
                    page.screenshot(path=str(DEST / f"_pw_fail_{vid}.png"))
                    continue
                cands = val["urls"]
                prog = [
                    c
                    for c in cands
                    if c.get("height")
                    and "video/mp4" in (c.get("mime") or "")
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
                    f"pick itag={pick.get('itag')} h={pick.get('height')} mime={pick.get('mime')}"
                )
                # Download via browser request context (same cookies/TLS as Chrome)
                w("browser request download...")
                resp = context.request.get(
                    pick["url"],
                    headers={
                        "Referer": "https://www.youtube.com/",
                        "Origin": "https://www.youtube.com",
                    },
                    timeout=600000,
                )
                w(f"http={resp.status}")
                if resp.status != 200:
                    w(f"body_snip={resp.text()[:300]}")
                    continue
                body = resp.body()
                out.write_bytes(body)
                w(f"SAVED {out.name} size={out.stat().st_size}")
            except Exception:
                w("FAIL " + traceback.format_exc())
        w("ALL_DONE")


if __name__ == "__main__":
    main()
