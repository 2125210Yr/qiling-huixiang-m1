"""Author new character meshes and bounded keyforms from the registered PSD layer assets."""
import json,math,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT.parents[1]/'analysis'))
from scan_rigs import sample_curve
data=json.loads((ROOT/'layers.json').read_text(encoding='utf-8'));W,H=data['width'],data['height'];ATLAS=data['atlasSize']
def smooth(x):x=np.clip(x,0,1);return x*x*(3-2*x)
def param(id,name,keys=(-1,0,1)):return dict(id=id,name=name,min=keys[0],max=keys[-1],default=0,keys=list(keys))
parameters=[param('ParamHover','悬浮'),param('ParamBreath','呼吸'),param('ParamLegScreenL','画面左腿轻抬'),param('ParamLegScreenR','画面右腿轻抬'),param('ParamAnkleScreenL','画面左脚踝'),param('ParamAnkleScreenR','画面右脚踝'),param('ParamHairScreenL','画面左后发'),param('ParamHairScreenR','画面右后发'),param('ParamTailSway','尾巴轻摆'),param('ParamEarTwitch','猫耳微动'),param('ParamBlink','自然眨眼',(0,.5,1)),param('ParamSkirtSway','裙摆余动')]
byid={p['id']:p for p in parameters}
hover=dict(id='D_Hover',name='人物与桌子共同悬浮',type='rotation',parent=None,partId=None,coordinateSpace='canvas',origin=[0,0],angle=0,scale=1,baseAngle=0,opacity=1,bindings=['ParamHover'],keyforms=[dict(values=[v],origin=[0,-12*v],angle=0,scale=1,opacity=1) for v in [-1,0,1]])
rows,cols=12,8
lattice=np.array([(x*W/cols,y*H/rows) for y in range(rows+1) for x in range(cols+1)],dtype=float)
def breathing(points,v):
    result=points.copy();x,y=points.T
    weight=1-smooth((y-500)/280)
    result[:,1]-=3.5*v*weight
    result[:,0]+=(x-512)*.0025*v*smooth((y-230)/130)*(1-smooth((y-540)/210))
    return result
breath=dict(id='D_Breath',name='呼吸形变：手掌和坐点固定',type='warp',parent='D_Hover',partId=None,coordinateSpace='rotation-local',rows=rows,columns=cols,lattice=lattice.ravel().tolist(),opacity=1,bindings=['ParamBreath'],keyforms=[dict(values=[v],lattice=breathing(lattice,v).ravel().tolist(),opacity=1) for v in [-1,0,1]])

def shape(id,points,values):
    result=points.copy();x,y=points.T;opacity=1
    if id=='GroundShadow':
        v=values[0];result[:,0]=522+(x-522)*(1+.035*v);result[:,1]=1450+(y-1450)*(1+.06*v);opacity=.82-.12*v
    elif id.startswith('LegScreen'):
        lift,ankle=values;left=id.endswith('L');w=smooth((y-710)/340)
        result[:,1]-=13*lift*w;result[:,0]+=(1 if left else -1)*2.2*lift*w
        ax,ay=(555,970) if left else (626,1022);weight=smooth((y-(ay-100))/105)
        a=math.radians(1.4*ankle);dx=x-ax;dy=y-ay
        result[:,0]+=weight*(dx*(math.cos(a)-1)-dy*math.sin(a))
        result[:,1]+=weight*(dx*math.sin(a)+dy*(math.cos(a)-1))
    elif id.startswith('HairScreen'):
        v=values[0];w=smooth((y-540)/780);result[:,0]+=7*v*w;result[:,1]+=1.5*v*w
    elif id=='Tail':
        v=values[0];w=smooth((730-y)/290);result[:,0]+=5*v*w;result[:,1]-=3.5*v*w
    elif id=='Head':
        v=values[0];w=(1-smooth((y-65)/120))*np.clip(abs(x-465)/100,0,1);result[:,1]-=2.1*v*w;result[:,0]+=np.sign(x-465)*.8*v*w
    elif id=='Skirt':
        v=values[0];w=smooth((y-740)/350);result[:,0]+=np.sign(x-512)*1.6*v*w;result[:,1]+=1.2*v*w
    elif id.startswith('EyeClosed'):opacity=values[0]
    return result,float(opacity)

meshes=[]
for layer in data['layers']:
    id=layer['id'];width,height=layer['width'],layer['height'];step=layer['step'];mapping={};points=[];indices=[]
    def vertex(gx,gy):
        key=(gx,gy)
        if key not in mapping:
            mapping[key]=len(points);points.append([layer['left']+min(gx*step,width),layer['top']+min(gy*step,height)])
        return mapping[key]
    for gx,gy in layer['occupied']:
        a,b,c,d=vertex(gx,gy),vertex(gx+1,gy),vertex(gx,gy+1),vertex(gx+1,gy+1);indices.extend([a,b,c,b,d,c])
    points=np.array(points,dtype=float)
    if id=='GroundShadow':axes=['ParamHover'];parent=None
    elif id in ['Table','TableUnderpaint']:axes=[];parent='D_Hover'
    elif id.startswith('LegScreen'):axes=['Param'+id,'ParamAnkle'+id[3:]];parent='D_Breath'
    elif id.startswith('HairScreen'):axes=['Param'+id];parent='D_Breath'
    elif id=='Tail':axes=['ParamTailSway'];parent='D_Breath'
    elif id=='Head':axes=['ParamEarTwitch'];parent='D_Breath'
    elif id=='Skirt':axes=['ParamSkirtSway'];parent='D_Breath'
    elif id.startswith('EyeClosed'):axes=['ParamBlink'];parent='D_Breath'
    else:axes=[];parent='D_Breath'
    normalization=np.array([W,H]) if parent=='D_Breath' else np.ones(2)
    base=points/normalization
    uv=(points-np.array([layer['left'],layer['top']])+np.array([layer['atlasX'],layer['atlasY']]))/ATLAS
    forms=[]
    if axes:
        grids=[byid[a]['keys'] for a in axes]
        from itertools import product
        for reverse_values in product(*grids[::-1]):
            values=list(reverse_values[::-1]);formed,opacity=shape(id,points,values)
            forms.append(dict(values=values,deltas=((formed-points)/normalization).ravel().tolist(),opacity=opacity))
    base_opacity=0 if id.startswith('EyeClosed') else .82 if id=='GroundShadow' else 1
    meshes.append(dict(id=id,name=layer['title'],parent=parent,partId='P_'+id,coordinateSpace='warp-normalized' if parent=='D_Breath' else 'rotation-local' if parent else 'canvas',positions=base.ravel().tolist(),uvs=uv.ravel().tolist(),indices=indices,bindings=axes,keyforms=forms,drawOrder=layer['order'],opacity=base_opacity,maskIds=[]))
spec=dict(schemaVersion=4,canvasSize=[W,H],atlas='atlas.png',parameters=parameters,deformers=[hover,breath],drawables=meshes,parts=[dict(id='P_'+m['id'],name=m['name'],children=[m['id']]) for m in meshes],rootOrder=['P_'+m['id'] for m in meshes[::-1]])
(ROOT/'rig-spec-v4.json').write_text(json.dumps(spec,ensure_ascii=False,separators=(',',':')),encoding='utf-8')

# Retarget source motion signals to the new control ranges, not old mesh positions or UVs.
POOL=Path('F:/天命之子/单机dc1.1整合包/live2d_extracted/recovered')
FPS=30;DURATION=8;times=np.linspace(0,DURATION,int(FPS*DURATION)+1);tracks={};derivation=[]
for target,source_id,source_param,amplitude in [('ParamBreath','c032_01_full','BREATH',.6),('ParamLegScreenL','c020_01_full','LEG_L_ROTATE',.55),('ParamLegScreenR','c020_01_full','LEG_R_ROTATE',.55),('ParamAnkleScreenL','c020_01_full','FOOT_L_ROTATE',.4),('ParamAnkleScreenR','c020_01_full','FOOT_R_ROTATE',.4)]:
    stem=source_id.removesuffix('_full');file=POOL/source_id/'motions'/(stem+'_idle.motion3.json');motion=json.loads(file.read_text(encoding='utf-8'))
    curve=next((c for c in motion['Curves'] if c['Target']=='Parameter' and c['Id']==source_param),None)
    raw=np.array([sample_curve(curve['Segments'],motion['Meta']['Duration']*float(t)/DURATION) for t in times]) if curve else np.zeros(len(times));span=float(np.ptp(raw))
    if span>1e-5:values=(raw-(float(raw.max())+float(raw.min()))/2)/(span/2)*amplitude;method='source curve recentered, amplitude limited and retimed to 8 seconds'
    else:values=np.sin(2*np.pi*times/DURATION+len(derivation)*.45)*amplitude;method='source control constant/missing; new bounded sinusoidal draft track'
    # Blend any source end-value mismatch out over the last 0.5 seconds.
    values-=smooth((times-(DURATION-.5))/.5)*(values[-1]-values[0]);values[-1]=values[0];tracks[target]=values
    derivation.append(dict(target=target,sourceFile=str(file),sourceParameter=source_param,sourceRange=[float(raw.min()),float(raw.max())],amplitude=amplitude,method=method))
phase=2*np.pi*times/DURATION
tracks.update(ParamHover=.7*np.sin(phase),ParamHairScreenL=.65*np.sin(phase-.65),ParamHairScreenR=.6*np.sin(phase-.95),ParamTailSway=.65*np.sin(phase-1.1),ParamSkirtSway=.5*np.sin(phase-.4),ParamEarTwitch=.42*np.exp(-((times-4.2)/.25)**2))
tracks['ParamBlink']=np.maximum(np.maximum(0,1-abs(times-2.4)/.14),np.maximum(0,1-abs(times-6.1)/.14))
curves=[]
for p in parameters:
    values=np.clip(tracks[p['id']],p['min'],p['max']);values[-1]=values[0];segments=[0,float(values[0])]
    for t,value in zip(times[1:],values[1:]):segments.extend([0,round(float(t),6),round(float(value),7)])
    curves.append(dict(Target='Parameter',Id=p['id'],Segments=segments))
motion=dict(Version=3,Meta=dict(Duration=DURATION,Fps=FPS,Loop=True,AreBeziersRestricted=True,CurveCount=len(curves),TotalSegmentCount=(len(times)-1)*len(curves),TotalPointCount=len(times)*len(curves),UserDataCount=0,TotalUserDataSize=0),Curves=curves)
(ROOT/'motions').mkdir(exist_ok=True);(ROOT/'motions/CatMaid_idle.motion3.json').write_text(json.dumps(motion,separators=(',',':')),encoding='utf-8')
(ROOT/'motion-derivation.json').write_text(json.dumps(dict(duration=DURATION,fps=FPS,retargetedTracks=derivation,newAuthoredTracks=['ParamHover','ParamHairScreenL','ParamHairScreenR','ParamTailSway','ParamSkirtSway','ParamEarTwitch','ParamBlink'],note='Source motion timing/control structure reused; all target meshes, UVs and keyforms authored for approved image.'),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(parameters=len(parameters),meshes=len(meshes),deformers=2,vertices=sum(len(m['positions'])//2 for m in meshes),triangles=sum(len(m['indices'])//3 for m in meshes),motionSeconds=DURATION)))
