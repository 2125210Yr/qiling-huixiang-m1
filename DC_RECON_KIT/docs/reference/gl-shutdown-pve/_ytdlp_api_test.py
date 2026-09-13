import os,sys,traceback
LOG=r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_ytdlp_api_test.log"
def w(msg):
  with open(LOG,"a",encoding="utf-8") as f:
    f.write(msg+"\n"); f.flush()
  print(msg, flush=True)
open(LOG,"w",encoding="utf-8").write("START\n")
os.environ["HTTP_PROXY"]="http://127.0.0.1:7890"
os.environ["HTTPS_PROXY"]="http://127.0.0.1:7890"
w("boot")
try:
  import yt_dlp
  w("imported "+getattr(yt_dlp.version,"__version__", "?"))
except Exception as e:
  w("import fail "+repr(e)); raise
opts={
  "proxy":"http://127.0.0.1:7890",
  "cookiefile":r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_yt_cookies.txt",
  "skip_download":True,
  "noplaylist":True,
  "quiet":True,
  "no_warnings":False,
  "verbose":False,
  "socket_timeout":20,
  "retries":1,
  "extractor_retries":1,
}
url="https://www.youtube.com/watch?v=hgqXY5M9gFk"
w("extract start")
try:
  with yt_dlp.YoutubeDL(opts) as ydl:
    info=ydl.extract_info(url, download=False)
  w("OK %s | %s | %s | formats=%s" % (info.get("id"), info.get("title"), info.get("duration"), len(info.get("formats") or [])))
except Exception as e:
  w("FAIL %s %s" % (type(e).__name__, e))
  with open(LOG,"a",encoding="utf-8") as f:
    traceback.print_exc(file=f)
w("DONE")
