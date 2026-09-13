"""Render official Core samples; explicitly reject unsupported mask/blend features."""
import json,sys,math
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
sys.path.insert(0,'F:/天命之子/圣诞勒达_c397/red-hair-work/vendor')
import moderngl
from scan_rigs import HERE,read

class Renderer:
    def __init__(self,width=384,height=640):
        self.ctx=ctx=moderngl.create_standalone_context(require=330)
        self.size=(width,height)
        self.fbo=ctx.simple_framebuffer(self.size,components=4)
        self.prog=ctx.program(vertex_shader='''#version 330
in vec2 position;in vec2 uv;out vec2 tc;uniform vec2 origin;uniform float ppu;uniform vec2 canvas;
void main(){vec2 p=vec2(origin.x+position.x*ppu,origin.y-position.y*ppu);gl_Position=vec4(p.x/canvas.x*2.-1.,1.-p.y/canvas.y*2.,0.,1.);tc=vec2(uv.x,1.-uv.y);}
''',fragment_shader='''#version 330
uniform sampler2D atlas;uniform float opacity;uniform vec3 multiplyColor;uniform vec3 screenColor;in vec2 tc;out vec4 color;
void main(){color=texture(atlas,tc);color.rgb*=multiplyColor;color.rgb=color.rgb+screenColor-color.rgb*screenColor;color.a*=opacity;}
''')
        ctx.enable(moderngl.BLEND)
        ctx.blend_func=(moderngl.SRC_ALPHA,moderngl.ONE_MINUS_SRC_ALPHA,moderngl.ONE,moderngl.ONE_MINUS_SRC_ALPHA)

    def load(self,entry,meta):
        assert all(not m['masks'] and m['blendMode']==0 for m in meta['meshes']),entry['id']
        self.meta=meta;self.resources=[];self.textures=[]
        p=self.prog;p['origin'].value=tuple(meta['origin']);p['ppu'].value=meta['ppu'];p['canvas'].value=tuple(meta['canvas'])
        for name in entry['textures']:
            im=Image.open(Path(entry['source'])/name).convert('RGBA')
            tex=self.ctx.texture(im.size,4,im.tobytes());tex.filter=(moderngl.LINEAR,moderngl.LINEAR);tex.repeat_x=False;tex.repeat_y=False;self.textures.append(tex)
        for m in meta['meshes']:
            arr=np.column_stack([np.zeros((len(m['uv'])//2,2)),np.array(m['uv']).reshape(-1,2)]).astype('f4')
            vb=self.ctx.buffer(arr.tobytes());ib=self.ctx.buffer(np.array(m['indices'],dtype='u2').tobytes())
            vao=self.ctx.vertex_array(p,[(vb,'2f 2f','position','uv')],ib,index_element_size=2)
            self.resources.append((arr,vb,ib,vao))

    def frame(self,frame,only=None):
        self.fbo.use();self.fbo.clear(.91,.92,.93,1)
        for k in sorted(range(len(self.resources)),key=lambda k:(frame['orders'][k],k)):
            if frame['opacities'][k]<=0 or (only is not None and k not in only):continue
            arr,vb,ib,vao=self.resources[k];arr[:,:2]=np.array(frame['positions'][k]).reshape(-1,2);vb.write(arr.tobytes())
            self.prog['opacity'].value=frame['opacities'][k]
            self.prog['multiplyColor'].value=tuple(frame['multiply'][k][:3]);self.prog['screenColor'].value=tuple(frame['screen'][k][:3])
            self.textures[self.meta['meshes'][k]['texture']].use(0);vao.render()
        return Image.frombytes('RGBA',self.size,self.fbo.read(components=4)).transpose(Image.Transpose.FLIP_TOP_BOTTOM).convert('RGB')

    def unload(self):
        for arr,vb,ib,vao in self.resources:vao.release();ib.release();vb.release()
        for tex in self.textures:tex.release()

def main():
    inventory=read(HERE/'inventory.json');thumbs=HERE/'thumbnails';thumbs.mkdir(exist_ok=True)
    sheets=HERE/'catalog';sheets.mkdir(exist_ok=True)
    renderer=Renderer(240,400);tiles=[]
    font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
    for entry in inventory:
        with (HERE/'cache'/(entry['id']+'.jsonl')).open(encoding='utf-8') as f:meta=json.loads(next(f));frame=json.loads(next(f))
        renderer.load(entry,meta);im=renderer.frame(frame);renderer.unload();im.save(thumbs/(entry['id']+'.png'))
        tile=Image.new('RGB',(240,430),'white');tile.paste(im,(0,30));ImageDraw.Draw(tile).text((10,6),entry['id'],font=font,fill='black');tiles.append(tile)
    for start in range(0,len(tiles),24):
        sheet=Image.new('RGB',(240*6,430*4),'white')
        for i,tile in enumerate(tiles[start:start+24]):sheet.paste(tile,((i%6)*240,(i//6)*430))
        sheet.save(sheets/f'catalog-{start//24+1:02d}.jpg',quality=91)
    evidence=dict(models=len(inventory),coreSnapshots=len(tiles),renderer=renderer.ctx.info['GL_RENDERER'],maskedMeshes=0,blendModes=[0],pose='first standard Idle motion at t=0; defaults where no motion',sourceReadOnly=True)
    (HERE/'scan-evidence.json').write_text(json.dumps(evidence,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(evidence,ensure_ascii=False))

if __name__=='__main__':main()
