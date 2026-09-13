import os, traceback
from pathlib import Path
os.environ["HTTP_PROXY"]="http://127.0.0.1:7890"
os.environ["HTTPS_PROXY"]="http://127.0.0.1:7890"
LOG=Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_ytdlp_irdq.log")
def w(m):
  with open(LOG,"a",encoding="utf-8") as f: f.write(m+"\n")
  print(m, flush=True)
LOG.write_text("START\n",encoding="utf-8")
import yt_dlp
DEST=Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve")
opts={
  "proxy":"http://127.0.0.1:7890",
  "cookiefile":str(DEST/"_yt_cookies.txt") if (DEST/"_yt_cookies.txt").exists() else None,
  "outtmpl":str(DEST/"IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.%(ext)s"),
  "format":"bv*[height<=480]+ba/b[height<=480]/b",
  "noplaylist":True,
  "socket_timeout":25,
  "retries":1,
  "quiet":False,
  "verbose":False,
}
# prefer min json cookies via netscape we have
if (DEST/"_yt_cookies.txt").exists():
  opts["cookiefile"]=str(DEST/"_yt_cookies.txt")
w("opts cookie="+str(opts.get("cookiefile")))
try:
  with yt_dlp.YoutubeDL(opts) as ydl:
    info=ydl.extract_info("https://www.youtube.com/watch?v=IRDqNAhKKr4", download=True)
  w("OK "+str(info.get("title"))+" "+str(info.get("duration")))
except Exception as e:
  w("FAIL "+type(e).__name__+": "+str(e))
  with open(LOG,"a",encoding="utf-8") as f: traceback.print_exc(file=f)
