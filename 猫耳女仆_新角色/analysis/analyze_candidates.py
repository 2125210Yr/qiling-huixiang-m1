"""Measure all parameter keys, interval midpoints and original idle movement."""
import json,math
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from scan_rigs import HERE,read,motion_pose,run_core
from render_rigs import Renderer

SELECTED={
 'c020_01_full':['LEG_L_ROTATE','LEG_R_ROTATE','FOOT_L_ROTATE','FOOT_R_ROTATE','BREATH','BODY_ROTATE'],
 'c030_00_full':['PARAM_LEG_R1_X','PARAM_BD_LO_Y','PARAM_BD_UP_Y','PARAM_FACE_X','PARAM_H_FR2_ROT','PARAM_FR2_BEND'],
 'c034_01_full':['LEG_L_Y','LEG_R_D','FOOT_L','FOOT_R','POSITION_Y','HAIR_B_W'],
 'c032_01_full':['BREATH','BODY_LOWER_Y','HAIR_WAVE','HEAD_ROTATE','EYE_CONTROL','MOUTH_CONTROL'],
 'c031_02_full':['PARAM_EAR','PARAM_TAIL_X','PARAM_TAIL_Y','PARAM_HAIR_BACK','PARAM_SKIRT_Y','PARAM_LEG_L_Y'],
}

def probes(p):
    keys=sorted(set([p['min'],p['max'],p['default'],*p['keys']]))
    return sorted(set(keys+[(a+b)/2 for a,b in zip(keys,keys[1:])]))

def main():
    entries={r['id']:r for r in read(HERE/'inventory.json')};jobs=[];info={}
    out=HERE/'candidates';out.mkdir(exist_ok=True)
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
    for id in SELECTED:
        entry=entries[id];params=entry['parameters'];ids={p['id'] for p in params}
        path=__import__('pathlib').Path(entry['source']);motion=read(path/entry['sampleMotion'])
        part_ids=entry['partIds']
        duration=motion['Meta']['Duration'];base=motion_pose(path/entry['sampleMotion'],0,ids,part_ids)
        poses=[('baseline',base)]
        for p in params:
            for label in ('min','max'):poses.append((p['id']+'@'+label,{**base,p['id']:p[label]}))
            for n,value in enumerate(probes(p)):poses.append((p['id']+f'@probe{n}',{**base,p['id']:value}))
        for n in range(72):poses.append((f'idle-{n:03d}',motion_pose(path/entry['sampleMotion'],duration*n/72,ids,part_ids)))
        tsv=HERE/'cache'/(id+'-detail.tsv');tsv.write_text('\n'.join(name+'\t'+'\t'.join(f'{k}={v:.9g}' for k,v in pose.items()) for name,pose in poses),encoding='utf-8')
        snapshot=HERE/'cache'/(id+'-detail.jsonl');jobs.append('\t'.join(map(str,[path/'model.moc3',snapshot,tsv])))
        info[id]=dict(duration=duration,poses=len(poses),ignoredMotionCurves=entry['ignoredMotionCurves'])
    manifest=HERE/'cache'/'candidate-manifest.tsv';manifest.write_text('\n'.join(jobs),encoding='utf-8');run_core(manifest)
    renderer=Renderer(300,500);report=[]
    for id,selected in SELECTED.items():
        entry=entries[id]
        with (HERE/'cache'/(id+'-detail.jsonl')).open(encoding='utf-8') as f:meta=json.loads(next(f));frames={r['name']:r for r in map(json.loads,f)}
        renderer.load(entry,meta);baseline=frames['baseline'];stats=[]
        for p in entry['parameters']:
            affected=[]
            for k,m in enumerate(meta['meshes']):
                b=np.array(baseline['positions'][k]).reshape(-1,2);maxdist=0.;opacityDelta=0.
                for n,value in enumerate(probes(p)):
                    f=frames[p['id']+f'@probe{n}'];pos=np.array(f['positions'][k]).reshape(-1,2)
                    if max(baseline['opacities'][k],f['opacities'][k])>.05:maxdist=max(maxdist,float(np.linalg.norm(pos-b,axis=1).max()*meta['ppu']))
                    opacityDelta=max(opacityDelta,abs(f['opacities'][k]-baseline['opacities'][k]))
                if maxdist>.1 or opacityDelta>.001:
                    px=np.column_stack([meta['origin'][0]+b[:,0]*meta['ppu'],meta['origin'][1]-b[:,1]*meta['ppu']])
                    affected.append(dict(id=m['id'],maxDisplacementPx=round(maxdist,3),opacityDelta=round(opacityDelta,5),baselineBoundsPx=[*np.round(px.min(axis=0),2),*np.round(px.max(axis=0),2)]))
            affected.sort(key=lambda x:x['maxDisplacementPx'],reverse=True)
            stats.append(dict(id=p['id'],probedValues=probes(p),affectedMeshes=len(affected),maxDisplacementPx=max((m['maxDisplacementPx'] for m in affected),default=0),meshes=affected))
        renderer.frame(baseline).save(out/(id+'-baseline.png'))
        idle=[renderer.frame(f) for name,f in frames.items() if name.startswith('idle-')]
        idle[0].save(out/(id+'-idle.gif'),save_all=True,append_images=idle[1:],duration=round(info[id]['duration']*1000/72),loop=0,optimize=False)
        sheet=Image.new('RGB',(900,530*len(selected)),'white')
        for row,pid in enumerate(selected):
            for col,label in enumerate(('min','baseline','max')):
                frame=baseline if label=='baseline' else frames[pid+'@'+label]
                tile=Image.new('RGB',(300,530),'white');tile.paste(renderer.frame(frame),(0,30));ImageDraw.Draw(tile).text((5,6),pid.replace('PARAM_','')+' '+label,font=font,fill='black');sheet.paste(tile,(col*300,row*530))
        sheet.save(out/(id+'-endpoints.jpg'),quality=90)
        renderer.unload()
        report.append(dict(id=id,**info[id],parameters=len(entry['parameters']),meshes=entry['meshCount'],deformers=entry['deformerCount'],endpointEffects=stats))
    (HERE/'candidate-evidence.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps([dict(id=r['id'],poses=r['poses'],duration=r['duration'],ignoredMotionCurves=r['ignoredMotionCurves']) for r in report],ensure_ascii=False))

if __name__=='__main__':main()
