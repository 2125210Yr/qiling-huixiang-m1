"""Check declared coverage, source immutability, motion interpolation and deliverable links."""
import json,hashlib,math,re
from pathlib import Path
from scan_rigs import HERE,read,sample_curve

inventory=read(HERE/'inventory.json');report=read(HERE/'candidate-evidence.json')
assert len(inventory)==144
hashes=0;curves=0
for entry in inventory:
    folder=Path(entry['source'])
    for filename,digest in entry['sha256'].items():
        assert hashlib.sha256((folder/filename).read_bytes()).hexdigest()==digest,(entry['id'],filename)
        hashes+=1
    assert not entry['ignoredMotionCurves'],entry['id']
    if entry['sampleMotion']:
        motion=read(folder/entry['sampleMotion'])
        for c in motion['Curves']:
            s=c['Segments'];assert math.isclose(sample_curve(s,s[0]),s[1],abs_tol=1e-5)
            assert math.isclose(sample_curve(s,s[-2]),s[-1],abs_tol=1e-5),(entry['id'],c['Id'])
            assert all(math.isfinite(sample_curve(s,motion['Meta']['Duration']*n/24)) for n in range(25));curves+=1
    with (HERE/'cache'/(entry['id']+'.jsonl')).open(encoding='utf-8') as f:
        meta=json.loads(next(f));frame=json.loads(next(f))
    assert len(meta['meshes'])==entry['meshCount']
    assert all(not m['masks'] and m['blendMode']==0 for m in meta['meshes'])
    assert all(math.isfinite(v) for positions in frame['positions'] for v in positions)
for r in report:
    assert not r['ignoredMotionCurves']
    with (HERE/'cache'/(r['id']+'-detail.jsonl')).open(encoding='utf-8') as f:
        meta=json.loads(next(f));frames=[json.loads(line) for line in f]
    assert len(frames)==r['poses']
    assert all(math.isfinite(v) for frame in frames for positions in frame['positions'] for v in positions)
    assert sum(f['name'].startswith('idle-') for f in frames)==72
    for suffix in ['-baseline.png','-idle.gif','-endpoints.jpg']:assert (HERE/'candidates'/(r['id']+suffix)).is_file()
for link in re.findall(r'(?:src|href)="([^"]+)"',(HERE/'review.html').read_text(encoding='utf-8')):
    assert (HERE/link).resolve().is_file(),link
approved=hashlib.sha256((HERE.parent/'concept-v003.png').read_bytes()).hexdigest()
assert approved=='154e70ae1392b7ae85a8c271391f01cea2b9b4ee8a2cc77bd56ee27c8c4f1607'
result=dict(ok=True,models=len(inventory),sourceHashesUnchanged=hashes,meshCount=sum(e['meshCount'] for e in inventory),candidateModels=len(report),candidateParameters=sum(r['parameters'] for r in report),candidatePoses=sum(r['poses'] for r in report),sampledIdleFrames=72*len(report),motionCurvesChecked=curves,approvedImageUnchanged=True,checkedScope='Core load/finite vertices; all declared scalar keys and interval midpoints for candidates; original idle; static review links; not a new-character rig acceptance test')
(HERE/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False))
