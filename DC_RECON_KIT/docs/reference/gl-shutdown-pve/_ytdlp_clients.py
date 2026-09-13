import os
import traceback
from pathlib import Path

os.environ["HTTP_PROXY"] = "http://127.0.0.1:7890"
os.environ["HTTPS_PROXY"] = "http://127.0.0.1:7890"

DEST = Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
LOG = DEST / "_ytdlp_now.log"


def w(m: str) -> None:
    with open(LOG, "a", encoding="utf-8") as f:
        f.write(m + "\n")
    print(m, flush=True)


def main() -> None:
    LOG.write_text("START\n", encoding="utf-8")
    import yt_dlp

    w("yt-dlp " + yt_dlp.version.__version__)
    for client in ["android", "ios", "tv", "web"]:
        w("=== try client " + client + " meta ===")
        opts = {
            "proxy": "http://127.0.0.1:7890",
            "cookiefile": str(DEST / "_yt_cookies.txt"),
            "skip_download": True,
            "quiet": True,
            "no_warnings": False,
            "socket_timeout": 30,
            "retries": 2,
            "extractor_args": {"youtube": {"player_client": [client]}},
        }
        try:
            with yt_dlp.YoutubeDL(opts) as ydl:
                info = ydl.extract_info(
                    "https://www.youtube.com/watch?v=hgqXY5M9gFk", download=False
                )
            fmts = info.get("formats") or []
            w(
                f"OK title={info.get('title')} dur={info.get('duration')} formats={len(fmts)}"
            )
            with_url = [
                f for f in fmts if f.get("url") and f.get("vcodec") != "none"
            ]
            w(f"with_video_url={len(with_url)}")
            for f in with_url[:8]:
                w(
                    f"  id={f.get('format_id')} h={f.get('height')} ext={f.get('ext')} "
                    f"a={f.get('acodec')} v={f.get('vcodec')}"
                )
            break
        except Exception as e:
            w(f"FAIL {client}: {type(e).__name__}: {e}")
            with open(LOG, "a", encoding="utf-8") as f:
                traceback.print_exc(file=f)


if __name__ == "__main__":
    main()
