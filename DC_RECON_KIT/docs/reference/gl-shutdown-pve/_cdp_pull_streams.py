"""Pull playable YouTube streams from the live logged-in CDP Chrome.

Do NOT inject stale cookies. Do NOT overwrite existing remux until the new
file is verified >=720p (P0) or is a real non-stub P2.
"""
import json
import traceback
import urllib.request
from pathlib import Path

from playwright.sync_api import sync_playwright

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_cdp_pull_streams.log"

P2_ID = "IRDqNAhKKr4"
P0_ID = "aSbBuFD12HY"
P2_OUT = DEST / "IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4"
P0_OUT = DEST / "aSbBuFD12HY_GL_EN_Gameplay_Part1_202308_hires.mp4"


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


DUMP_JS = """() => {
  const pr = window.ytInitialPlayerResponse || {};
  const st = pr.playabilityStatus || {};
  const vd = pr.videoDetails || {};
  const sd = pr.streamingData || {};
  const v = document.querySelector('video');
  const all = (sd.formats || []).concat(sd.adaptiveFormats || []);
  return {
    title: vd.title || document.title,
    status: st.status,
    reason: st.reason,
    length: Number(vd.lengthSeconds || 0),
    videoW: v && v.videoWidth,
    videoH: v && v.videoHeight,
    items: all.map(f => ({
      itag: f.itag,
      h: f.height,
      w: f.width,
      fps: f.fps,
      br: f.bitrate,
      mime: (f.mimeType || '').slice(0, 70),
      hasUrl: !!f.url,
      clen: f.contentLength,
      quality: f.quality,
      audio: f.audioQuality || null,
      url: f.url || null
    }))
  };
}"""


def pick_progressive(items):
    cands = [
        x
        for x in items
        if x.get("hasUrl")
        and x.get("h")
        and x.get("url")
        and "audio" not in (x.get("mime") or "")
        and "mp4a" in (x.get("mime") or "")
    ]
    if not cands:
        cands = [x for x in items if x.get("hasUrl") and x.get("url") and x.get("h")]
    if not cands:
        return None
    return sorted(cands, key=lambda x: (x.get("h") or 0), reverse=True)[0]


def download(context, url, dest: Path, label: str) -> bool:
    w(f"GET {label} -> {dest.name}")
    try:
        resp = context.request.get(
            url,
            timeout=600_000,
            headers={
                "Referer": "https://www.youtube.com/",
                "Origin": "https://www.youtube.com",
            },
        )
        w(f"  status={resp.status} headers_ct={resp.headers.get('content-type')} len={resp.headers.get('content-length')}")
        if resp.status != 200:
            body = resp.text()[:300]
            w(f"  body={body}")
            return False
        dest.write_bytes(resp.body())
        w(f"  SAVED {dest.name} size={dest.stat().st_size}")
        return dest.stat().st_size > 500_000
    except Exception:
        w("  FAIL " + traceback.format_exc())
        return False


def dump_page(page):
    info = page.evaluate(DUMP_JS)
    slim = {k: info.get(k) for k in ("title", "status", "reason", "length", "videoW", "videoH")}
    w(json.dumps(slim, ensure_ascii=False))
    for it in info.get("items") or []:
        w(
            f"  itag={it.get('itag')} {it.get('w')}x{it.get('h')} fps={it.get('fps')} "
            f"audio={it.get('audio')} hasUrl={it.get('hasUrl')} mime={it.get('mime')} clen={it.get('clen')}"
        )
    return info


def main() -> None:
    LOG.write_text("START\n", encoding="utf-8")
    with sync_playwright() as p:
        browser = p.chromium.connect_over_cdp("http://127.0.0.1:9222")
        context = browser.contexts[0]
        pages = context.pages
        p2 = None
        for pg in pages:
            if P2_ID in (pg.url or ""):
                p2 = pg
                break
        if p2 is None:
            w("NO P2 TAB — open watch?v=IRDqNAhKKr4 first")
            return
        w(f"P2 tab {p2.url}")
        info = dump_page(p2)
        pick = pick_progressive(info.get("items") or [])
        if pick:
            w(f"P2 pick itag={pick.get('itag')} {pick.get('w')}x{pick.get('h')} clen={pick.get('clen')}")
            tmp = DEST / "_tmp_p2_itag18.mp4"
            if download(context, pick["url"], tmp, f"P2 itag {pick.get('itag')}"):
                if tmp.stat().st_size > 500_000:
                    if P2_OUT.exists() and P2_OUT.stat().st_size < 1_000_000:
                        w("P2 dest was stub/missing — will replace after verify")
                    tmp.replace(P2_OUT)
                    w(f"P2 LANDED {P2_OUT.name} size={P2_OUT.stat().st_size}")
        else:
            w("P2 no progressive URL")

        # P0 in a new tab — do not navigate the working P2 tab away
        w("=== open P0 ===")
        p0 = context.new_page()
        p0.goto(f"https://www.youtube.com/watch?v={P0_ID}", wait_until="domcontentloaded", timeout=120000)
        p0.wait_for_timeout(8000)
        info0 = dump_page(p0)
        pick0 = pick_progressive(info0.get("items") or [])
        if pick0:
            w(f"P0 pick itag={pick0.get('itag')} {pick0.get('w')}x{pick0.get('h')} clen={pick0.get('clen')}")
            if (pick0.get("h") or 0) >= 720:
                download(context, pick0["url"], P0_OUT, f"P0 itag {pick0.get('itag')}")
            else:
                w(f"P0 progressive only {pick0.get('h')}p — keep as evidence, not T27 unlock")
                dest = DEST / f"_tmp_p0_itag{pick0.get('itag')}.mp4"
                download(context, pick0["url"], dest, f"P0 itag {pick0.get('itag')} (not hires)")
        else:
            w("P0 no progressive URL")
        w("ALL_DONE")


if __name__ == "__main__":
    main()
