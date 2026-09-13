import os
import sys
import time

log_path = r"F:\天命之子\DC_RECON_KIT\m1\G0\BASELINE_LOGS\editor-playmode-timing.log"
req = r"F:\天命之子\client\Temp\vs-smoke.request"
running = r"F:\天命之子\client\Temp\vs-smoke.running"
result = r"F:\天命之子\client\Temp\vs-smoke.result.txt"
durable = r"F:\天命之子\client\captures\vs-smoke.result.txt"

def read(path):
    try:
        return open(path, encoding="utf-8", errors="replace").read()
    except OSError:
        return ""

def ready(log):
    if "error CS" in log:
        return False
    return "CompileScripts:" in log and "FinalizeReload" in log

wrote = False
started = time.time()
deadline = started + 12 * 60
print("watch start", flush=True)
while time.time() < deadline:
    log = read(log_path)
    if "error CS" in log and time.time() - started > 45:
        print("COMPILE FAIL")
        for line in log.splitlines():
            if "error CS" in line:
                print(line)
        raise SystemExit(3)
    if (not wrote) and ready(log) and not os.path.isfile(running) and not os.path.isfile(result):
        # settle after first compile/reload
        if log.count("FinalizeReload") >= 1:
            time.sleep(8)
            os.makedirs(os.path.dirname(req), exist_ok=True)
            if os.path.isfile(result):
                try:
                    os.remove(result)
                except OSError:
                    pass
            mode = "lose" if (len(sys.argv) > 1 and sys.argv[1].lower() == "lose") else "1"
            with open(req, "w", encoding="utf-8") as f:
                f.write(mode)
            print("mode", mode, flush=True)
            wrote = True
            print("wrote request", flush=True)
    body = read(result)
    if (not body) and os.path.isfile(durable) and os.path.getmtime(durable) > started:
        body = read(durable)
    if body:
        print("RESULT")
        print(body)
        raise SystemExit(0 if body.startswith("PASS") else 1)
    time.sleep(3)
print("TIMEOUT")
print("wrote", wrote)
print(read(log_path)[-2000:])
raise SystemExit(2)
