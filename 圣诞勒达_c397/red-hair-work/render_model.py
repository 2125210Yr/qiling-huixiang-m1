"""Render the unchanged official-Core mesh samples with original/red textures."""
import sys,json
from pathlib import Path
import numpy as np
from PIL import Image
sys.path.insert(0,str(Path(__file__).resolve().parent/'vendor'))
import moderngl
work=Path(__file__).resolve().parent
root=work.parent
out=root/'c397_02_redhair'
records=[json.loads(s) for s in (work/'core-samples.jsonl').read_text().splitlines()]
meta=records[0]; frames=records[1:]
assert all(not m['masks'] and m['blendMode']==0 for m in meta['meshes'])
ctx=moderngl.create_standalone_context(require=330)
w,h=960,1600
fbo=ctx.simple_framebuffer((w,h),components=4);fbo.use()
prog=ctx.program(vertex_shader='''#version 330
in vec2 position;in vec2 uv;out vec2 tc;uniform vec2 origin;uniform float ppu;
void main(){vec2 p=vec2(origin.x+position.x*ppu,origin.y-position.y*ppu);gl_Position=vec4(p.x/960.*2.-1.,1.-p.y/1600.*2.,0.,1.);tc=vec2(uv.x,1.-uv.y);}
''',fragment_shader='''#version 330
uniform sampler2D atlas;uniform float opacity;in vec2 tc;out vec4 color;
void main(){color=texture(atlas,tc);color.a*=opacity;}
''')
prog['origin'].value=tuple(meta['origin']);prog['ppu'].value=meta['ppu']
resources=[]
for m in meta['meshes']:
 v=np.column_stack([np.zeros((len(m['uv'])//2,2)),np.array(m['uv']).reshape(-1,2)]).astype('f4')
 vb=ctx.buffer(v.tobytes());ib=ctx.buffer(np.array(m['indices'],dtype='u2').tobytes())
 vao=ctx.vertex_array(prog,[(vb,'2f 2f','position','uv')],ib,index_element_size=2);resources.append((v,vb,vao))
ctx.enable(moderngl.BLEND);ctx.blend_func=(moderngl.SRC_ALPHA,moderngl.ONE_MINUS_SRC_ALPHA,moderngl.ONE,moderngl.ONE_MINUS_SRC_ALPHA)
def render(frame,texture):
 fbo.clear(.91,.92,.93,1);texture.use(0)
 for k in sorted(range(len(resources)),key=lambda k:frame['orders'][k]):
  if frame['opacities'][k]<=0:continue
  v,vb,vao=resources[k];v[:,:2]=np.array(frame['positions'][k]).reshape(-1,2);vb.write(v.tobytes());prog['opacity'].value=frame['opacities'][k];vao.render()
 return Image.frombytes('RGBA',(w,h),fbo.read(components=4)).transpose(Image.Transpose.FLIP_TOP_BOTTOM).convert('RGB')
textures=[]
for model in ['c397_02_moc3','c397_02_redhair']:
 im=Image.open(root/model/'textures/texture_00.png').convert('RGBA');tex=ctx.texture(im.size,4,im.tobytes());tex.filter=(moderngl.LINEAR,moderngl.LINEAR);tex.repeat_x=False;tex.repeat_y=False;textures.append(tex)
render(frames[1],textures[0]).save(out/'preview-original.png')
render(frames[1],textures[1]).save(out/'preview-redhair.png')
sheet=Image.new('RGB',(w*2,h))
for i,t in enumerate(textures):sheet.paste(render(frames[1],t),(w*i,0))
sheet.save(out/'before-after.png')
selected=[f for f in frames if f['name'] in ['idle-000','idle-015','idle-030','idle-045','idle-060','attack-004','hit-004','banner-004']]
sheet=Image.new('RGB',(480*4,800*2))
for i,f in enumerate(selected):sheet.paste(render(f,textures[1]).resize((480,800)),((i%4)*480,(i//4)*800))
sheet.save(out/'motion-contact.png')
idle=[render(f,textures[1]).resize((480,800)) for f in frames if f['name'].startswith('idle-')]
idle[0].save(out/'redhair-idle.gif',save_all=True,append_images=idle[1:],duration=124,loop=0)
print(json.dumps({'renderer':ctx.info.get('GL_RENDERER'),'corePoses':len(frames),'idleFrames':len(idle),'contactPoses':len(selected),'maskedMeshes':0,'blendModes':[0]}))
