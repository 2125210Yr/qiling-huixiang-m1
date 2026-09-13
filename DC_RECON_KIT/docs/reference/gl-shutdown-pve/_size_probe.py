import os, json, time
d = r'F:\天命之子\DC_RECON_KIT\docs\reference\gl-shutdown-pve'
targets = [
  ('aSbBuFD12HY', 'aSbBuFD12HY_GL_EN_Gameplay_Part1_202308.mp4'),
  ('hgqXY5M9gFk', 'hgqXY5M9gFk_GL_ND_Robin_Boss.mp4'),
  ('IRDqNAhKKr4', 'IRDqNAhKKr4_GL_EternalVow_NormalHard_480p.mp4'),
]
listing = os.listdir(d)
all_media = []
for fn in listing:
  if fn.lower().endswith(('.mp4', '.part')) or '.part.' in fn.lower() or fn.endswith('.ytdl'):
    all_media.append({'name': fn, 'size': os.path.getsize(os.path.join(d, fn))})

summary = []
for vid, name in targets:
  p = os.path.join(d, name)
  exists = os.path.isfile(p)
  size = os.path.getsize(p) if exists else 0
  siblings = [m for m in all_media if vid in m['name'] and m['name'] != name]
  ok = exists and size > 100000
  summary.append({
    'Id': vid,
    'Status': 'SUCCESS' if ok else 'FAILURE',
    'ExitCode': 0 if ok else (1 if exists else -1),
    'Size': size,
    'Exists': exists,
    'LastError': '' if ok else ('missing' if not exists else f'too_small:{size}'),
    'siblings': siblings,
  })

open(os.path.join(d, '_size_probe.json'), 'w', encoding='utf-8').write(
  json.dumps({'ts': time.strftime('%Y-%m-%d %H:%M:%S'), 'all_media': all_media, 'summary': summary}, ensure_ascii=False, indent=2)
)
open(os.path.join(d, '_size_probe.txt'), 'w', encoding='utf-8').write(
  '\n'.join(f"{m['size']}\t{m['name']}" for m in all_media) + ('\n' if all_media else 'NONE\n')
)
open(os.path.join(d, '_fetch_summary.json'), 'w', encoding='utf-8').write(
  json.dumps(summary, ensure_ascii=False, indent=2)
)
lines = []
for r in summary:
  for k, v in r.items():
    lines.append(f'{k} : {v}')
  lines.append('---')
open(os.path.join(d, '_fetch_results.txt'), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
with open(os.path.join(d, 'FETCH_LOG.txt'), 'a', encoding='utf-8') as f:
  f.write(f"\n==== SIZE_PROBE {time.strftime('%Y-%m-%d %H:%M:%S')} ====\n")
  for m in all_media:
    f.write(f"{m['size']}\t{m['name']}\n")
  if not all_media:
    f.write('(no mp4/part)\n')
  for s in summary:
    f.write(f"{s['Status']} id={s['Id']} exit={s['ExitCode']} size={s['Size']} exists={s['Exists']}\n")
  f.write('==== SIZE_PROBE END ====\n')
print(json.dumps(summary, ensure_ascii=False, indent=2))
