"""Inventory the user-selected c000..c049 pool and prepare original idle samples."""
import json, re, hashlib, subprocess
from pathlib import Path

HERE=Path(__file__).resolve().parent
POOL=Path('F:/天命之子/单机dc1.1整合包/live2d_extracted/recovered')
JAVA=Path('C:/Program Files/Microsoft/jdk-17.0.20.101-hotspot/bin')
CORE=Path('F:/Live2D Cubism 5.3/app/lib/Live2DCubismCore.jar')
NATIVES=Path('F:/Live2D Cubism 5.3/app/dll64')

def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))

def sample_curve(seg,t):
    """motion3 segment types: linear, cubic Bezier, stepped, inverse stepped."""
    x0,y0=seg[:2]; i=2
    if t<=x0:return y0
    while i<len(seg):
        kind=int(seg[i]); i+=1
        if kind==1:
            x1,y1,x2,y2,x3,y3=seg[i:i+6]; i+=6
            if t<=x3:
                lo,hi=0.,1.
                for _ in range(36):
                    u=(lo+hi)/2;v=1-u
                    x=v*v*v*x0+3*v*v*u*x1+3*v*u*u*x2+u*u*u*x3
                    if x<t:lo=u
                    else:hi=u
                u=(lo+hi)/2;v=1-u
                return v*v*v*y0+3*v*v*u*y1+3*v*u*u*y2+u*u*u*y3
            x0,y0=x3,y3
        elif kind in (0,2,3):
            x1,y1=seg[i:i+2];i+=2
            if t<=x1:
                if kind==2:return y0 if t<x1 else y1
                if kind==3:return y1
                return y0+(y1-y0)*(t-x0)/(x1-x0) if x1>x0 else y1
            x0,y0=x1,y1
        else:raise ValueError(f'unknown motion segment {kind}')
    return y0

def motion_pose(path,t,ids,part_ids=()):
    motion=read(path)
    result={c['Id']:sample_curve(c['Segments'],t) for c in motion['Curves'] if c['Target']=='Parameter' and c['Id'] in ids}
    result.update({'part:'+c['Id']:sample_curve(c['Segments'],t) for c in motion['Curves'] if c['Target']=='PartOpacity' and c['Id'] in part_ids})
    return result

def run_core(manifest):
    classes=HERE/'classes';classes.mkdir(exist_ok=True)
    flags=dict(check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    subprocess.run([str(JAVA/'javac.exe'),'-encoding','UTF-8','-classpath',str(CORE),'-d',str(classes),str(HERE/'RigSnapshot.java')],**flags)
    subprocess.run([str(JAVA/'java.exe'),'-Xmx2g','-Dfile.encoding=UTF-8','-Djava.library.path='+str(NATIVES),'-cp',str(CORE)+';'+str(classes),'RigSnapshot',str(manifest)],**flags)

def main():
    cache=HERE/'cache';cache.mkdir(exist_ok=True)
    inventory=[]; jobs=[]
    for folder in sorted(POOL.iterdir()):
        match=re.fullmatch(r'c(\d{3})_\d+_full',folder.name)
        if not match or int(match[1])>=50:continue
        spec=read(folder/'rig-spec-v4.json'); model=read(folder/'model.model3.json')
        refs=model['FileReferences']; params=spec['parameters'];ids={p['id'] for p in params}
        idle=refs.get('Motions',{}).get('Idle',[])
        standard=[m for m in idle if m['File'].endswith('_idle.motion3.json')]
        chosen=(standard or idle or [None])[0]
        part_ids={p['id'] for p in spec.get('parts',[])}
        pose=motion_pose(folder/chosen['File'],0,ids,part_ids) if chosen else {}
        tsv=cache/(folder.name+'.tsv');tsv.write_text('idle-start\t'+'\t'.join(f'{k}={v:.9g}' for k,v in pose.items())+'\n',encoding='utf-8')
        snapshot=cache/(folder.name+'.jsonl')
        jobs.append('\t'.join(map(str,[folder/refs['Moc'],snapshot,tsv])))
        entry=dict(id=folder.name,source=str(folder),parameters=params,meshCount=len(spec['drawables']),deformerCount=len(spec['deformers']),partCount=len(spec.get('parts',[])),textures=refs['Textures'],motions=refs.get('Motions',{}),sampleMotion=chosen['File'] if chosen else None,sha256={f:hashlib.sha256((folder/f).read_bytes()).hexdigest() for f in ['model.moc3','rig-spec-v4.json']})
        entry['partIds']=sorted(part_ids)
        curves=read(folder/chosen['File'])['Curves'] if chosen else []
        entry['ignoredMotionCurves']=[dict(target=c['Target'],id=c['Id']) for c in curves if not (c['Target']=='Parameter' and c['Id'] in ids or c['Target']=='PartOpacity' and c['Id'] in part_ids)]
        inventory.append(entry)
    (HERE/'inventory.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2),encoding='utf-8')
    manifest=cache/'manifest.tsv';manifest.write_text('\n'.join(jobs),encoding='utf-8')
    print(f'Inventory: {len(inventory)} full models',flush=True)
    run_core(manifest)

if __name__=='__main__':main()
