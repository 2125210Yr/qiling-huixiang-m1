import os
import time

root = "F:\\" + "\u5929\u547d\u4e4b\u5b50"
log = os.path.join(root, "DC_RECON_KIT", "m1", "G0", "BASELINE_LOGS", "editor-playmode-20260913-patha5.log")
req = os.path.join(root, "client", "Temp", "vs-smoke.request")
running = os.path.join(root, "client", "Temp", "vs-smoke.running")
result = os.path.join(root, "client", "Temp", "vs-smoke.result.txt")
durable = os.path.join(root, "client", "captures", "vs-smoke.result.txt")


def read(path):
    try:
        return open(path, encoding="utf-8", errors="replace").read()
    except OSError:
        return ""


started = time.time()
print("wait log", flush=True)
while time.time() - started < 20 * 60:
    if os.path.isfile(log) and os.path.getsize(log) > 200:
        break
    time.sleep(2)
else:
    print("NO LOG", flush=True)
    raise SystemExit(4)

wrote = False
deadline = started + 28 * 60
print("watching", log, flush=True)
while time.time() < deadline:
    body = read(log)
    if "ChangeMode(safe_mode)" in body or "ModeService[safe_mode]" in body:
        print("SAFE MODE", flush=True)
        raise SystemExit(5)
    if "Tundra build failed" in body or "Script Compilation Error" in body:
        print("COMPILE FAIL", flush=True)
        for line in body.splitlines():
            if "error CS" in line:
                print(line, flush=True)
        raise SystemExit(3)
    compiled_ok = "Tundra build success" in body and "error CS" not in body
    loaded = "Loading completed" in body or "Reload assemblies" in body
    if (not wrote) and compiled_ok and loaded and not os.path.isfile(running):
        time.sleep(12)
        os.makedirs(os.path.dirname(req), exist_ok=True)
        try:
            if os.path.isfile(result):
                os.remove(result)
        except OSError:
            pass
        with open(req, "w", encoding="utf-8") as f:
            f.write("1")
        wrote = True
        print("wrote request", flush=True)
    out = read(result)
    if not out and os.path.isfile(durable) and os.path.getmtime(durable) > started:
        out = read(durable)
    if not out:
        i = body.rfind("[VS-SMOKE]")
        if i >= 0:
            chunk = body[i:i + 8000]
            if "\nPASS" in chunk or "PASS\nscreens=" in chunk or "\nFAIL" in chunk:
                out = chunk
    if out:
        print("RESULT", flush=True)
        print(out[:4000], flush=True)
        ok = out.startswith("PASS") or "\nPASS" in out[:200]
        raise SystemExit(0 if ok and "FAIL timeout" not in out[:80] else 1)
    time.sleep(4)
print("TIMEOUT wrote", wrote, flush=True)
raise SystemExit(2)
