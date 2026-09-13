import http.cookiejar, json, re, urllib.request
from pathlib import Path
LOG=Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_watch_with_cookies.log")
def w(m):
  LOG.write_text(LOG.read_text(encoding="utf-8")+m+"\n", encoding="utf-8") if LOG.exists() else LOG.write_text(m+"\n", encoding="utf-8")
  print(m, flush=True)
LOG.write_text("START\n", encoding="utf-8")
PROXY="http://127.0.0.1:7890"
cj=http.cookiejar.MozillaCookieJar(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_yt_cookies.txt")
cj.load(ignore_discard=True, ignore_expires=True)
w(f"cookies loaded {len(list(cj))}")
opener=urllib.request.build_opener(
  urllib.request.ProxyHandler({"http":PROXY,"https":PROXY}),
  urllib.request.HTTPCookieProcessor(cj),
)
UA="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
vid="hgqXY5M9gFk"
url=f"https://www.youtube.com/watch?v={vid}&hl=en"
req=urllib.request.Request(url, headers={"User-Agent":UA,"Accept-Language":"en-US,en;q=0.9"})
w("fetch watch")
html=opener.open(req, timeout=30).read().decode("utf-8","replace")
w(f"html len={len(html)}")
m=re.search(r"ytInitialPlayerResponse\s*=\s*(\{.+?\})\s*;", html)
if not m:
  w("NO player response")
  # save snippet
  Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_watch_snip.html").write_text(html[:5000], encoding="utf-8")
  raise SystemExit(2)
p=json.loads(m.group(1))
st=p.get("playabilityStatus") or {}
w(f"status={st.get('status')} reason={st.get('reason')}")
sd=p.get("streamingData") or {}
fmts=sd.get("formats") or []
ad=sd.get("adaptiveFormats") or []
w(f"formats={len(fmts)} adaptive={len(ad)}")
# pick progressive mp4 <=480 or best progressive
cands=[]
for f in fmts+ad:
  u=f.get("url")
  if not u: continue
  h=f.get("height") or 0
  mime=f.get("mimeType") or ""
  cands.append((h, mime, u, f.get("itag"), f.get("contentLength")))
cands.sort(key=lambda x: (0 if "video/mp4" in x[1] and "audio" not in (x[1]) else 1, abs((x[0] or 999)-360)))
w(f"url candidates={len(cands)}")
for c in cands[:8]:
  w(f" cand h={c[0]} itag={c[3]} mime={c[1][:40]} clen={c[4]} url={c[2][:80]}")
# save json summary
Path(r"F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve\_player_summary.json").write_text(
  json.dumps({"status":st,"n_formats":len(fmts),"n_adaptive":len(ad),"first_urls":[{"h":c[0],"itag":c[3],"mime":c[1],"url":c[2]} for c in cands[:5]]}, ensure_ascii=False, indent=2),
  encoding="utf-8")
w("DONE")
