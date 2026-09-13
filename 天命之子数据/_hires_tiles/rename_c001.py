import json

p = r"F:\天命之子\client\Assets\Content\catalog.json"
with open(p, encoding="utf-8") as f:
    data = json.load(f)
for c in data.get("chars", []):
    if c.get("id") == "C001":
        c["name"] = "冰刃"
        print("renamed", c["id"], c["name"])
        break
else:
    raise SystemExit("C001 missing")
with open(p, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, separators=(",", ":"))
print("wrote", p)
