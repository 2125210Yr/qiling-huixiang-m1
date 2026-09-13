import os
import time
import sys

log = r"F:\天命之子\DC_RECON_KIT\m1\G0\BASELINE_LOGS\editor-playmode-20260913-patha2.log"
req = r"F:\天命之子\client\Temp\vs-smoke.request"
running = r"F:\天命之子\client\Temp\vs-smoke.running"
result = r"F:\天命之子\client\Temp\vs-smoke.result.txt"
durable = r"F:\天命之子\client\captures\vs-smoke.result.txt"

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
    body = open(log, encoding="utf-8", errors="replace").read()
    if "ChangeMode(safe_mode)" in body or "ModeService[safe_mode]" in body:
        print("SAFE MODE", flush=True)
        raise SystemExit(5)
    fresh_err = False
    if "error CS" in body:
        # only fail if an error appears after the last successful compile marker, or any CS after load start
        fresh_err = "Tundra build failed" in body or "Script Compilation Error" in body
    if fresh_err:
        print("COMPILE FAIL", flush=True)
        for line in body.splitlines():
            if "error CS" in line:
                print(line, flush=True)
        raise SystemExit(3)
    loaded = "Loading completed" in body and "safe_mode" not in body
    compiled_ok = (
        "script compilation time" in body
        and "Tundra build failed" not in body
        and "error CS" not in body
    )
    if (not wrote) and loaded and compiled_ok and not os.path.isfile(running):
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
    out = ""
    try:
        out = open(result, encoding="utf-8", errors="replace").read()
    except OSError:
        out = ""
    if not out and wrote and os.path.isfile(durable) and os.path.getmtime(durable) > started:
        out = open(durable, encoding="utf-8", errors="replace").read()
    if out:
        print("RESULT", flush=True)
        print(out, flush=True)
        raise SystemExit(0 if out.startswith("PASS") else 1)
    time.sleep(4)
print("TIMEOUT wrote", wrote, flush=True)
raise SystemExit(2)
