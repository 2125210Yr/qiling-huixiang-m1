"""Sample the exported moc3 through official Core and render visual checks."""
import json,sys,shutil,hashlib,random
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT.parents[1]/'analysis'))
from scan_rigs import run_core,sample_curve
from render_rigs import Renderer
def read(p):return json.loads(p.read_text(encoding='utf-8'))
spec=read(ROOT/'rig-spec-v4.json');model=ROOT/'export-v001/model';cache=ROOT/'cache';cache.mkdir(exist_ok=True)
(model/'motions').mkdir(exist_ok=True);shutil.copy2(ROOT/'motions/CatMaid_idle.motion3.json',model/'motions/CatMaid_idle.motion3.json')
model3=read(model/'model.model3.json');model3['FileReferences']['Motions']={'Idle':[{'File':'motions/CatMaid_idle.motion3.json','FadeInTime':.2,'FadeOutTime':.2}]};(model/'model.model3.json').write_text(json.dumps(model3,ensure_ascii=False,indent=2),encoding='utf-8')
defaults={p['id']:p['default'] for p in spec['parameters']};poses=[('default',defaults)]
for p in spec['parameters']:
    keys=p['keys'];values=sorted(set(keys+[(a+b)/2 for a,b in zip(keys,keys[1:])]))
    for n,v in enumerate(values):poses.append((p['id']+f'-probe{n}',{**defaults,p['id']:v}))
motion=read(ROOT/'motions/CatMaid_idle.motion3.json')
for n in range(241):poses.append((f'idle-{n:03d}',{c['Id']:sample_curve(c['Segments'],n/30) for c in motion['Curves']}))
rng=random.Random(397031)
for n in range(32):poses.append((f'joint-{n:02d}',{p['id']:rng.uniform(p['min'],p['max']) for p in spec['parameters']}))
tsv=cache/'poses.tsv';tsv.write_text('\n'.join(name+'\t'+'\t'.join(f'{k}={v:.9g}' for k,v in values.items()) for name,values in poses),encoding='utf-8')
snapshot=cache/'core-samples.jsonl';manifest=cache/'manifest.tsv';manifest.write_text('\t'.join(map(str,[model/'model.moc3',snapshot,tsv])),encoding='utf-8');run_core(manifest)
with snapshot.open(encoding='utf-8') as f:meta=json.loads(next(f));frames=[json.loads(line) for line in f]
assert len(frames)==len(poses)
renderer=Renderer(768,1152);renderer.load(dict(id='CatMaid',source=str(model),textures=model3['FileReferences']['Textures']),meta)
frameByName={f['name']:f for f in frames}
renderer.frame(frameByName['default']).save(ROOT/'verification/core-default.png')
selected=['default','idle-030','idle-072','idle-110','idle-160','idle-183','idle-215','joint-07'];sheet=Image.new('RGB',(384*4,606*2),'white');font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
for n,name in enumerate(selected):
    im=renderer.frame(frameByName[name]).resize((384,576));x=(n%4)*384;y=(n//4)*606;sheet.paste(im,(x,y+30));ImageDraw.Draw(sheet).text((x+12,y+5),name,font=font,fill='black')
sheet.save(ROOT/'verification/motion-contact.jpg',quality=93)
# Saved anchors allow the browser's renderer to be independently compared to Core.
anchors=[]
for name in ['default','idle-030','idle-072','idle-183','joint-00','joint-01','joint-07','joint-15','joint-31']:
    f=frameByName[name];values=dict(poses[[n for n,(label,_) in enumerate(poses) if label==name][0]][1]);anchors.append(dict(name=name,values=values,meshes=[dict(id=m['id'],positionsPx=np.column_stack([meta['origin'][0]+np.array(f['positions'][k]).reshape(-1,2)[:,0]*meta['ppu'],meta['origin'][1]-np.array(f['positions'][k]).reshape(-1,2)[:,1]*meta['ppu']]).ravel().tolist(),opacity=f['opacities'][k]) for k,m in enumerate(meta['meshes'])]))
(ROOT/'verification/core-anchors.json').write_text(json.dumps(anchors,separators=(',',':')),encoding='utf-8')
native=read(model/'verification.json');assert native['passed']
result=dict(ok=True,parameters=len(spec['parameters']),meshes=len(spec['drawables']),vertices=sum(len(m['positions'])//2 for m in spec['drawables']),sampledPoses=len(poses),idleFrames=241,randomJointPoses=32,nativeExporterSampleCount=native['core']['sampleCount'],nativeIndexedWindingPassed=native['core']['coreIndexedSignedAreasVerified'],nativeDefaultResetNoDrift=native['core']['resetNoDrift'],nativeMaxPositionError=native['core']['maxPositionError'],coreVersion=native['core']['mocVersion'],coreAnchors=len(anchors),renderer=renderer.ctx.info['GL_RENDERER'],cmoReread=native['cmo3']['reread'],sourceApprovedHash=hashlib.sha256((ROOT.parents[1]/'concept-v003.png').read_bytes()).hexdigest(),limitations=['First motion draft; fine edge cleanup and blink transition need further refinement','No Cubism Editor 5.3 UI open/save test','Finite sampled joint states, not exhaustive global combinations'])
(ROOT/'verification/native-summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False));renderer.unload()
