import base64
import json
import traceback
from pathlib import Path

from playwright.sync_api import sync_playwright

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_pw_mediacapture.log"
COOKIES = json.loads((DEST / "_yt_cookies_min.json").read_text(encoding="utf-8"))

# shortest first; durations used as record budget fallback
TARGETS = [
    ("hgqXY5M9gFk", "hgqXY5M9gFk_GL_ND_Robin_Boss.webm", 90),
    ("aSbBuFD12HY", "aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.webm", 620),
    # IRDq ~46min — run after first two land
]


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


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

        for vid, outname, budget in TARGETS:
            out = DEST / outname
            mp4 = out.with_suffix(".mp4")
            if (mp4.exists() and mp4.stat().st_size > 500_000) or (
                out.exists() and out.stat().st_size > 500_000
            ):
                w(f"SKIP exists")
                continue
            w(f"=== {vid} budget={budget}s ===")
            try:
                page.goto(
                    f"https://www.youtube.com/watch?v={vid}",
                    wait_until="domcontentloaded",
                    timeout=120000,
                )
                page.wait_for_timeout(8000)
                info = page.evaluate(
                    """() => {
  const pr=window.ytInitialPlayerResponse||{};
  return {
    status: (pr.playabilityStatus||{}).status,
    title: (pr.videoDetails||{}).title,
    length: Number((pr.videoDetails||{}).lengthSeconds||0)
  };
}"""
                )
                w(json.dumps(info, ensure_ascii=False))
                if info.get("status") != "OK":
                    w("not playable")
                    continue
                length = info.get("length") or budget
                record_s = min(int(length) + 3, budget)

                # Start recorder in page, store chunks on window
                page.evaluate(
                    """async () => {
  const v = document.querySelector('video');
  if (!v) throw new Error('no video');
  v.muted = true;
  v.currentTime = 0;
  await v.play();
  const stream = v.captureStream();
  let mime = 'video/webm;codecs=vp9,opus';
  if (!MediaRecorder.isTypeSupported(mime)) mime = 'video/webm;codecs=vp8,opus';
  if (!MediaRecorder.isTypeSupported(mime)) mime = 'video/webm';
  const rec = new MediaRecorder(stream, { mimeType: mime, videoBitsPerSecond: 2500000 });
  window.__recChunks = [];
  window.__recMime = mime;
  window.__recDone = false;
  window.__recError = null;
  rec.ondataavailable = (e) => { if (e.data && e.data.size) window.__recChunks.push(e.data); };
  rec.onerror = (e) => { window.__recError = String(e.error || e); };
  rec.onstop = () => { window.__recDone = true; };
  window.__rec = rec;
  rec.start(1000);
  return { mime, rs: v.readyState, dur: v.duration };
}"""
                )
                w(f"recording {record_s}s ...")
                # poll playback; stop at end
                page.wait_for_timeout(record_s * 1000)
                page.evaluate(
                    """async () => {
  const v = document.querySelector('video');
  if (v && !v.ended) { try { v.pause(); } catch(e){} }
  if (window.__rec && window.__rec.state !== 'inactive') window.__rec.stop();
  // wait up to 5s for onstop
  for (let i=0;i<50;i++){
    if (window.__recDone) break;
    await new Promise(r=>setTimeout(r,100));
  }
  return { done: window.__recDone, n: (window.__recChunks||[]).length, err: window.__recError };
}"""
                )
                meta = page.evaluate(
                    "() => ({ done: window.__recDone, n: (window.__recChunks||[]).length, err: window.__recError, mime: window.__recMime })"
                )
                w(f"rec meta {meta}")
                if not meta.get("n"):
                    w("no chunks")
                    continue

                # Build blob in page and stream out as base64 slices
                total = page.evaluate(
                    """async () => {
  const blob = new Blob(window.__recChunks, { type: window.__recMime || 'video/webm' });
  window.__recBlob = blob;
  return blob.size;
}"""
                )
                w(f"blob size={total}")
                # read in 256KB slices via FileReader through arrayBuffer slices
                chunk = 256 * 1024
                with open(out, "wb") as fo:
                    offset = 0
                    while offset < total:
                        end = min(offset + chunk, total)
                        b64 = page.evaluate(
                            """async ({start, end}) => {
  const blob = window.__recBlob.slice(start, end);
  const buf = new Uint8Array(await blob.arrayBuffer());
  const step = 0x8000;
  let s = '';
  for (let i = 0; i < buf.length; i += step) {
    s += String.fromCharCode.apply(null, buf.subarray(i, Math.min(i+step, buf.length)));
  }
  return btoa(s);
}""",
                            {"start": offset, "end": end},
                        )
                        fo.write(base64.b64decode(b64))
                        offset = end
                        if offset % (2 * 1024 * 1024) < chunk:
                            w(f"  wrote {offset}/{total}")
                w(f"SAVED {out.name} size={out.stat().st_size}")
                # try ffmpeg remux to mp4
                import shutil
                import subprocess

                ffmpeg = shutil.which("ffmpeg")
                if ffmpeg:
                    mp4_out = out.with_suffix(".mp4")
                    # handoff names use .mp4
                    handoff_mp4 = DEST / outname.replace(".webm", ".mp4")
                    r = subprocess.run(
                        [
                            ffmpeg,
                            "-y",
                            "-i",
                            str(out),
                            "-c",
                            "copy",
                            str(handoff_mp4),
                        ],
                        capture_output=True,
                        text=True,
                    )
                    w(f"ffmpeg exit={r.returncode} size={handoff_mp4.stat().st_size if handoff_mp4.exists() else 0}")
                    if r.returncode != 0:
                        w(r.stderr[-500:])
            except Exception:
                w("FAIL " + traceback.format_exc())
        w("ALL_DONE")


if __name__ == "__main__":
    main()
