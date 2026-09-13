"""Probe YT playability via Chrome CDP. No yt-dlp. No download."""
import json
import time
import urllib.request
from pathlib import Path

from playwright.sync_api import sync_playwright

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_probe_playable.log"
COOKIES = json.loads((DEST / "_yt_cookies_min.json").read_text(encoding="utf-8"))
IDS = ["aSbBuFD12HY", "hgqXY5M9gFk", "IRDqNAhKKr4"]


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


def cdp_up() -> bool:
    try:
        urllib.request.urlopen("http://127.0.0.1:9222/json/version", timeout=2).read()
        return True
    except Exception:
        return False


def main() -> None:
    LOG.write_text("START\n", encoding="utf-8")
    if not cdp_up():
        w("CDP_DOWN")
        return
    with sync_playwright() as p:
        browser = p.chromium.connect_over_cdp("http://127.0.0.1:9222")
        context = browser.contexts[0] if browser.contexts else browser.new_context()
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
        try:
            context.add_cookies(pw_cookies)
            w("cookies %d" % len(pw_cookies))
        except Exception as e:
            w("cookie_fail %s" % e)
        page = context.pages[0] if context.pages else context.new_page()
        try:
            page.set_viewport_size({"width": 900, "height": 1600})
        except Exception as e:
            w("viewport %s" % e)
        for vid in IDS:
            url = "https://www.youtube.com/watch?v=" + vid
            w("=== " + vid)
            try:
                page.goto(url, wait_until="domcontentloaded", timeout=90000)
                page.wait_for_timeout(7000)
                info = page.evaluate(
                    """() => {
  const pr = window.ytInitialPlayerResponse || {};
  const v = document.querySelector('video');
  let box = null;
  if (v) {
    const r = v.getBoundingClientRect();
    box = {w: Math.round(r.width), h: Math.round(r.height), vw: v.videoWidth, vh: v.videoHeight};
  }
  return {
    status: (pr.playabilityStatus || {}).status,
    reason: (pr.playabilityStatus || {}).reason || '',
    title: (pr.videoDetails || {}).title || document.title,
    length: Number((pr.videoDetails || {}).lengthSeconds || 0),
    logged: !!(document.querySelector('#avatar-btn') || document.querySelector('button[aria-label*=\"Account\"]')),
    box: box
  };
}"""
                )
                w(json.dumps(info, ensure_ascii=False))
            except Exception as e:
                w("FAIL " + str(e))
        w("DONE")


if __name__ == "__main__":
    main()
