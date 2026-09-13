# -*- coding: utf-8 -*-
"""Re-pick soul-carta art (drop shared placeholder) and child portraits (drop V* stats panels)."""
from __future__ import annotations

import csv
import hashlib
import json
import os
import re
from concurrent.futures import ThreadPoolExecutor, as_completed
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
USER = os.path.join(ROOT, "天命之子数据")
PAGES_CN = os.path.join(ROOT, "docs", "reference", "gamekee", "pages")
PAGES_JP = os.path.join(ROOT, "docs", "reference", "gamekee", "dcj", "pages")
INDEX_JP = os.path.join(ROOT, "docs", "reference", "gamekee", "dcj", "catalog_index.csv")
CSV_CARTA = os.path.join(USER, "魂之歌牌.csv")
CSV_CHILD = os.path.join(USER, "汇总表.csv")
AVATARS = os.path.join(USER, "avatars")

PLACEHOLDER_BITS = ("988768.png",)
PLACEHOLDER_MD5 = "2bb7341e8fd7"

UA = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
)

JP_ALIAS = {
    "nightmare": "nightmare",
    "hero": "hero",
    "时间结束": "time over",
    "跳高高": "高跳",
    "战略家的夜晚": "战略家之夜",
    "梦幻队伍": "梦之队",
    "海咲": "美咲",
    "认真的员工": "认真的劳动者",
    "小小袭击者": "小小的袭击者",
    "午后列车": "下午的列车",
    "秘密的约会": "秘密约会",
    "最甜蜜的求婚": "甜蜜求婚",
    "海中偶像": "海洋偶像",
    "女神的洗礼": "女神的洗礼",
    "今天是好孩子魔亚哦": "今天是好孩子魔亚哦",
    "享乐之城": "享乐之城",
    "露娜": "露娜",
    "女天狗": "女天狗",
    "心": "心",
}


def abs_url(u: str) -> str:
    u = (u or "").strip()
    if not u:
        return ""
    if u.startswith("//"):
        return "https:" + u
    if u.startswith("http://"):
        return "https://" + u[len("http://"):]
    if u.startswith("https://"):
        return u
    return "https://" + u.lstrip("/")


def norm_name(s: str) -> str:
    t = (s or "").strip().lower()
    t = t.replace("！", "!").replace("？", "?").replace(" ", "").replace("　", "")
    t = re.sub(r"[·・.。，,、:：]", "", t)
    return JP_ALIAS.get(t, t)


def load_json(path: str) -> dict:
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def page_urls(path: str) -> list[str]:
    if not os.path.isfile(path):
        return []
    try:
        data = (load_json(path).get("data") or {})
    except Exception:
        return []
    out = []
    for t in str(data.get("thumb") or "").split(","):
        u = abs_url(t)
        if u:
            out.append(u)
    content = data.get("content") or ""
    for m in re.finditer(r"(?:https?:)?//[^\"'\s>]+\.(?:png|jpg|jpeg|webp)", content, re.I):
        out.append(abs_url(m.group(0)))
    seen, uniq = set(), []
    for u in out:
        if u not in seen:
            seen.add(u)
            uniq.append(u)
    return uniq


def score_carta_url(url: str) -> float:
    low = (url or "").lower()
    if not low:
        return -1
    if any(b in low for b in PLACEHOLDER_BITS):
        return -1
    if ".gif" in low.split("?")[0]:
        return -1
    if "/w_250/h_250/" in low or "h_250,w_250" in low:
        return 100
    if "/w_135/h_135/" in low:
        return 96
    if "/w_128/h_128/" in low:
        return 35
    if re.search(r"/w_24[6-9]/", low):
        return 92
    if "m00/" in low:
        return 90
    if "/w_250/h_417/" in low or "/w_614/" in low:
        return 72
    if "cdnimg-v2" in low and "/images/" in low:
        return 80
    if low.split("?")[0].endswith(".png"):
        return 40
    return 10


def pick_best(urls: list[str]) -> str:
    best, best_s = "", -1.0
    for u in urls:
        s = score_carta_url(u)
        if s > best_s:
            best, best_s = u, s
    return best if best_s >= 40 else ""


def is_image_bytes(data: bytes) -> bool:
    if len(data) < 12:
        return False
    if data[:8] == b"\x89PNG\r\n\x1a\n":
        return True
    if data[:2] == b"\xff\xd8":
        return True
    if data[:4] == b"RIFF" and data[8:12] == b"WEBP":
        return True
    return False


def fetch_image(url: str) -> bytes:
    req = Request(
        url,
        headers={
            "User-Agent": UA,
            "Referer": "https://www.gamekee.com/",
            "Accept": "image/avif,image/webp,image/png,image/*,*/*;q=0.8",
        },
    )
    with urlopen(req, timeout=25) as resp:
        data = resp.read()
    if not is_image_bytes(data):
        raise RuntimeError("not-image %s" % url[:80])
    if hashlib.md5(data).hexdigest()[:12] == PLACEHOLDER_MD5:
        raise RuntimeError("placeholder-hash")
    return data


def url_alts(url: str) -> list[str]:
    seen, out = set(), []
    cands = [
        url,
        url.replace("cdnimg.gamekee.com", "cdnimg-v2.gamekee.com"),
        url.replace("cdnimg-v2.gamekee.com", "cdnimg.gamekee.com"),
        url.replace("cdnimg01.gamekee.com", "cdnimg-v2.gamekee.com"),
        url.replace("cdnimg02.gamekee.com", "cdnimg-v2.gamekee.com"),
    ]
    if "?" in url:
        cands.append(url.split("?")[0])
    for u in cands:
        if u and u not in seen:
            seen.add(u)
            out.append(u)
    return out


def download_to(url: str, dest: str) -> str:
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    last = "fail"
    for u in url_alts(url):
        try:
            data = fetch_image(u)
            tmp = dest + ".part"
            with open(tmp, "wb") as f:
                f.write(data)
            os.replace(tmp, dest)
            return "ok"
        except (HTTPError, URLError, TimeoutError, OSError, RuntimeError) as e:
            last = "fail:%s" % e
    return last


def load_jp_hana() -> dict[str, str]:
    """normalized JP 花牌 name -> page id"""
    out = {}
    if not os.path.isfile(INDEX_JP):
        return out
    with open(INDEX_JP, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            blob = " ".join([(r.get("path") or ""), (r.get("section") or ""), (r.get("name") or "")])
            if "花牌" not in blob:
                continue
            cid = (r.get("content_id") or "").strip()
            name = (r.get("name") or "").strip()
            if not cid or not name or name in ("花牌", "五星花牌", "四星花牌", "三星花牌"):
                continue
            out[norm_name(name)] = cid
    return out


def jp_id_for(cn_name: str, jp_map: dict[str, str]) -> str:
    k = norm_name(cn_name)
    if k in jp_map:
        return jp_map[k]
    for jk, cid in jp_map.items():
        if k and (k in jk or jk in k) and min(len(k), len(jk)) >= 2:
            return cid
    return ""


def md5_12(path: str) -> str:
    h = hashlib.md5()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()[:12]


def fix_cartas() -> None:
    with open(CSV_CARTA, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
        fields = list(rows[0].keys()) if rows else []
    jp_map = load_jp_hana()
    print("jp hana names", len(jp_map))

    jobs = []
    for r in rows:
        cid = (r.get("content_id") or "").strip()
        name = r.get("name") or ""
        urls = page_urls(os.path.join(PAGES_CN, "%s.json" % cid))
        best = pick_best(urls)
        src = "cn"
        if not best:
            jid = jp_id_for(name, jp_map)
            if jid:
                urls = page_urls(os.path.join(PAGES_JP, "%s.json" % jid))
                best = pick_best(urls)
                src = "jp:%s" % jid
        r["avatar_url"] = best
        dest = os.path.join(AVATARS, "%s.png" % cid)
        local = os.path.join(USER, r.get("avatar_file") or "")
        need = True
        if best and os.path.isfile(local) and os.path.getsize(local) > 2000:
            if md5_12(local) != PLACEHOLDER_MD5:
                need = False
        if best and need:
            jobs.append((best, dest, r, src))
        elif not best:
            print("NO-IMG", name, cid)
        r["avatar_file"] = "avatars/%s.png" % cid if (best or os.path.isfile(dest)) else (r.get("avatar_file") or "")

    print("carta re-download", len(jobs))
    ok = fail = 0
    with ThreadPoolExecutor(max_workers=8) as ex:
        futs = {ex.submit(download_to, url, dest): (url, dest, rec, src) for url, dest, rec, src in jobs}
        for fut in as_completed(futs):
            url, dest, rec, src = futs[fut]
            st = fut.result()
            if st == "ok":
                ok += 1
                rec["avatar_file"] = "avatars/%s.png" % rec["content_id"]
            else:
                fail += 1
                print("FAIL", rec.get("name"), src, st, url[:90])
    print("carta download ok", ok, "fail", fail)

    # drop leftover placeholder files from the table
    still = 0
    for r in rows:
        p = os.path.join(USER, r.get("avatar_file") or "")
        if os.path.isfile(p) and md5_12(p) == PLACEHOLDER_MD5:
            still += 1
            r["avatar_file"] = ""
            print("STILL-PLACEHOLDER", r.get("name"))
    print("still placeholder", still)

    with open(CSV_CARTA, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)


G_RE = re.compile(r"^g([54321])_(\d+)_(.+)\.png$", re.I)


def rar_digit(s: str) -> str:
    t = str(s or "")
    for ch in "54321":
        if t.startswith(ch):
            return ch
    return ""


def index_g_files() -> list[tuple[str, str, str]]:
    """(rarity digit, name stem, relpath)"""
    out = []
    folder = os.path.join(USER, "avatars_561")
    if not os.path.isdir(folder):
        return out
    for fn in os.listdir(folder):
        m = G_RE.match(fn)
        if not m:
            continue
        out.append((m.group(1), m.group(3), "avatars_561/" + fn))
    out.sort(key=lambda x: -len(x[1]))
    return out


def match_g_name(rar: str, name: str, gfiles: list[tuple[str, str, str]], used: set[str]) -> str:
    n = (name or "").strip()
    if not n or not rar:
        return ""
    best, best_s = "", 0
    for r, stem, path in gfiles:
        if r != rar or path in used or len(stem) < 2:
            continue
        if stem in n or n.endswith(stem):
            s = len(stem) + (10 if n.endswith(stem) else 0)
            if s > best_s:
                best, best_s = path, s
    return best


def file_ok(rel: str) -> bool:
    if not rel:
        return False
    p = os.path.join(USER, rel.replace("\\", "/"))
    return os.path.isfile(p) and os.path.getsize(p) > 1500


def is_v_panel(rel: str) -> bool:
    b = os.path.basename(rel.replace("\\", "/"))
    return bool(re.match(r"^V[1-5]_\d+", b))


def pick_child_avatar(r: dict, gfiles: list[tuple[str, str, str]], used: set[str]) -> str:
    rar = rar_digit(r.get("rarity"))
    try:
        idx = int(r.get("idx"))
    except (TypeError, ValueError):
        idx = -1
    cur = (r.get("avatar_file") or "").replace("\\", "/")
    if (cur.startswith("avatars/") or cur.startswith("avatars_wiki/")) and file_ok(cur) and not is_v_panel(cur):
        return cur
    g = match_g_name(rar, r.get("name") or "", gfiles, used)
    if g and file_ok(g):
        used.add(g)
        return g
    if rar and idx >= 0:
        icon = "icons/R%s_%03d.png" % (rar, idx)
        if file_ok(icon):
            return icon
        face = "icons/F%s_%03d.png" % (rar, idx)
        if file_ok(face):
            return face
    if cur and file_ok(cur) and not is_v_panel(cur) and not cur.startswith("avatars_561/"):
        return cur
    return ""


OCR_BAD = re.compile(
    r"SKILLINFORMATION|TIER\s*1|基碰能力|基碴能力|之一擎|INFORMATION|IOMAL",
    re.I,
)


def skill_clean(t: str) -> str:
    s = (t or "").strip()
    if not s:
        return ""
    if s.count("|") >= 3:
        return ""
    if OCR_BAD.search(s):
        return ""
    if s.endswith("：") or s.endswith(":"):
        return ""
    return s


def name_clean(r: dict, gmap: dict[tuple[str, int], str]) -> str:
    name = (r.get("name") or "").strip()
    wiki = (r.get("wiki_name") or "").strip()
    if wiki and (not name or name_is_garbage(name)):
        return wiki
    if name_is_garbage(name):
        return wiki or ""
    return name


def name_is_garbage(name: str) -> bool:
    n = (name or "").strip()
    if not n or len(n) <= 1:
        return True
    if n.endswith("：") or n.endswith(":"):
        return True
    if "|" in n:
        return True
    if re.search(r"我以为|完全没|可以直接|SKILL|TIER|INFORMATION", n):
        return True
    if n.count("三") >= 3:
        return True
    return False


def fix_children() -> None:
    gfiles = index_g_files()
    used: set[str] = set()
    print("g-files", len(gfiles))
    with open(CSV_CHILD, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))
        fields = list(rows[0].keys())
    n_av = n_name = n_sk = 0
    for r in rows:
        av = pick_child_avatar(r, gfiles, used)
        if av != (r.get("avatar_file") or ""):
            n_av += 1
            r["avatar_file"] = av
        nm = name_clean(r, gfiles)
        if nm != (r.get("name") or ""):
            n_name += 1
            r["name"] = nm
        for k in ("auto_text", "tap_text", "slide_text", "drive_text", "leader_text"):
            c = skill_clean(r.get(k) or "")
            if c != (r.get(k) or ""):
                n_sk += 1
                r[k] = c
    empty = sum(1 for r in rows if not r.get("avatar_file"))
    print("child avatar changed", n_av, "name", n_name, "skill fields cleaned", n_sk, "empty av", empty)
    with open(CSV_CHILD, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)


def main() -> None:
    fix_cartas()
    fix_children()


if __name__ == "__main__":
    main()
