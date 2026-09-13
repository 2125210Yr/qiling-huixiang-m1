import os
import shutil

root = "F:\\" + "\u5929\u547d\u4e4b\u5b50"
tmp = os.path.join(root, "client", "Temp")
cap = os.path.join(root, "client", "captures")
dest = os.path.join(root, "DC_RECON_KIT", "docs", "reference", "gl-shutdown-pve", "our_slice", "shots_20260913d")
logs = os.path.join(root, "DC_RECON_KIT", "m1", "G0", "BASELINE_LOGS")
os.makedirs(dest, exist_ok=True)

names = [
    "01_home.png", "ui_home.png",
    "02_roster.png", "02_characters.png", "ui_roster.png",
    "04_team.png", "ui_team.png",
    "03_inspect.png", "ui_inspect.png",
    "06_battle.png", "ui_battle.png",
    "06g_speed.png", "06h_auto.png", "06i_pause.png", "06j_auto_hit.png",
    "06a_charge.png", "06b_tap.png", "06c_slide.png", "06e_control.png",
    "06d_drive_select.png", "06f_kill.png",
    "07_drive.png", "08_fever.png", "10_wave.png",
    "09_victory.png", "09_result.png", "12_levelup.png", "13_rematch.png", "11_home_return.png",
]
copied = []
missing = []
for n in names:
    src = os.path.join(cap, n)
    if os.path.isfile(src):
        shutil.copy2(src, os.path.join(dest, n))
        copied.append(n)
    else:
        missing.append(n)

durable = os.path.join(cap, "vs-smoke.result.txt")
if os.path.isfile(durable):
    shutil.copy2(durable, os.path.join(logs, "vs-smoke-timing-20260913d.txt"))

for leaf in ("vs-smoke.request", "vs-smoke.running", "vs-smoke.quit"):
    p = os.path.join(tmp, leaf)
    if os.path.isfile(p):
        os.remove(p)
        print("removed", leaf)

print("copied", len(copied), "missing", missing)
print("dest", dest)
print("unity leftover request cleared")
