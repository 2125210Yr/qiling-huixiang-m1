import os

root = "F:\\" + "\u5929\u547d\u4e4b\u5b50"
tmp = os.path.join(root, "client", "Temp")
os.makedirs(tmp, exist_ok=True)
req = os.path.join(tmp, "vs-smoke.request")
running = os.path.join(tmp, "vs-smoke.running")
result = os.path.join(tmp, "vs-smoke.result.txt")
print("tmp", tmp, "exists", os.path.isdir(tmp))
print("running", os.path.isfile(running))
print("result", os.path.isfile(result))
if os.path.isfile(result):
    try:
        os.remove(result)
    except OSError as e:
        print("result_rm", e)
if os.path.isfile(running):
    print("ALREADY RUNNING")
else:
    with open(req, "w", encoding="utf-8") as f:
        f.write("1")
    print("wrote", req, os.path.isfile(req), os.path.getsize(req))
