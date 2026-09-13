"""Create the local review and traceable mapping, without editing illustration pixels."""
import json,html
from pathlib import Path
from scan_rigs import HERE,read

ROLES=[
 ('c020_01_full','腿与脚踝 · 主参考','左右小腿各有腿旋转与脚踝变形，3 × 3 关键形状。新图的赤脚和正面透视需重建。','LEG_L_ROTATE / LEG_R_ROTATE / FOOT_L_ROTATE / FOOT_R_ROTATE'),
 ('c032_01_full','呼吸与女仆服装 · 主参考','呼吸、五官和头发控制可作组织参考。新长裙、手掌支撑和双马尾需要单独适配。','BREATH / EYE_CONTROL / MOUTH_CONTROL / HAIR_WAVE'),
 ('c031_02_full','猫耳与尾巴 · 主参考','耳朵与尾巴独立响应。尾巴横向控制是多关键值运动，不能只按首尾判断。','PARAM_EAR / PARAM_TAIL_X / PARAM_TAIL_Y'),
 ('c034_01_full','坐姿层级 · 备用','腿、脚和坐骑有独立层级。仅参考关节关系，专属爪足与坐骑不迁入。','LEG_R_U / LEG_R_D / FOOT_R / POSITION_Y'),
 ('c030_00_full','双膝坐姿 · 备用','坐姿与眼睛控制可供比较，独立腿控制不足以直接满足左右脚分别抬动。','PARAM_LEG_R1_X / PARAM_EYE_L_OPEN / PARAM_EYE_R_OPEN'),
]

def main():
    inventory={e['id']:e for e in read(HERE/'inventory.json')}
    mapping=dict(status='preproduction_mapping_not_implemented_rig',approvedImage='../concept-v003.png',coordinateConvention='All new left/right labels mean screen-left/screen-right; source IDs retain original naming.',sourcePolicy='read-only',groups=[
        dict(target='knees_and_ankles',source='c020_01_full',parameters=['LEG_L_ROTATE','LEG_R_ROTATE','FOOT_L_ROTATE','FOOT_R_ROTATE'],reuse='controller hierarchy and 2-axis keyform organization',rebuild='new geometry, UVs and bare-foot shape keys'),
        dict(target='breath_face_cloth',source='c032_01_full',parameters=['BREATH','FACE_X','FACE_Y','EYE_CONTROL','MOUTH_CONTROL','HAIR_WAVE'],reuse='timing and control separation',rebuild='facial layers, pinned-hand body deformation, skirt and long hair meshes'),
        dict(target='ears_tail',source='c031_02_full',parameters=['PARAM_EAR','PARAM_TAIL_X','PARAM_TAIL_Y'],reuse='ear/tail isolation and multi-key phase organization',rebuild='new ear pivots and fluffy tail silhouette'),
        dict(target='table_hover_shadow',source=None,parameters=[],reuse=None,rebuild='new shared character/table hover root; ground shadow independent'),
    ])
    for g in mapping['groups']:
        if g['source']:
            entry=inventory[g['source']];spec=read(Path(entry['source'])/'rig-spec-v4.json')
            g['sourceHashes']=entry['sha256']
            g['sourceBindings']=[dict(id=n['id'],type=n.get('type','mesh'),parent=n.get('parent'),bindings=n.get('bindings',[]),keyformCount=len(n.get('keyforms',[]))) for n in spec['deformers']+spec['drawables'] if set(n.get('bindings',[]))&set(g['parameters'])]
    (HERE/'reuse-map.json').write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
    tabs=[];panels=[]
    for i,(id,role,desc,params) in enumerate(ROLES):
        entry=inventory[id]
        tabs.append(f'<button role="tab" aria-selected="{str(i==0).lower()}" aria-controls="panel-{i}" id="tab-{i}" data-index="{i}">{role.split(" · ")[0]}</button>')
        panels.append(f'''<section class="candidate" id="panel-{i}" role="tabpanel" aria-labelledby="tab-{i}" {'hidden' if i else ''}>
<img class="motion" src="candidates/{id}-idle.gif" alt="{id} 的原模型待机预览"><div class="candidate-copy"><span class="eyebrow">{role}</span><h2>{id}</h2><p>{desc}</p><div class="counts">{len(entry['parameters'])} 参数 · {entry['meshCount']} 网格 · {entry['deformerCount']} 变形器</div><code>{params}</code><a class="link" href="candidates/{id}-endpoints.jpg" target="_blank">查看参数端点对照 ↗</a></div></section>''')
    doc='''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>猫耳女仆 · 动作模板分析</title>
<style>
:root{color-scheme:light;--ink:#2c3434;--muted:#6d7472;--teal:#277d7e;--line:#deded7}*{box-sizing:border-box}body{margin:0;background:#f3f2ec;color:var(--ink);font:15px/1.7 "Microsoft YaHei",sans-serif}header{max-width:1440px;margin:auto;padding:30px 38px 18px;display:flex;justify-content:space-between;align-items:center}h1{font-size:27px;margin:3px 0;font-weight:600}h2{font-size:21px;margin:8px 0}.eyebrow{font-size:12px;color:var(--teal);font-weight:600;letter-spacing:1px}.stage{font-size:13px;border:1px solid #b9cfcb;padding:7px 14px;border-radius:30px;color:var(--teal)}main{max-width:1440px;margin:auto;padding:0 38px 42px;display:grid;grid-template-columns:minmax(300px,.9fr) minmax(540px,1.3fr);gap:24px}.art-card,.work-card{background:#fff;border:1px solid var(--line);border-radius:18px;overflow:hidden}.art-card{position:relative;align-self:start}.art-card>img{display:block;width:100%;height:auto;max-height:760px;object-fit:contain}.caption{border-top:1px solid var(--line);padding:14px 20px;display:flex;justify-content:space-between;gap:12px;font-size:13px}.caption span{color:var(--muted)}.work-card{padding:25px}.intro{margin-top:0;color:var(--muted)}.stats{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin:22px 0}.stat{background:#f4f6f2;padding:13px 16px;border-radius:10px}.stat strong{font-size:25px;display:block;font-weight:600}.stat span{color:var(--muted);font-size:12px}.tabs{display:flex;gap:6px;flex-wrap:wrap;border-bottom:1px solid var(--line);padding-bottom:12px}button{font:inherit;cursor:pointer;border:1px solid transparent;background:#f3f2ed;border-radius:7px;padding:6px 10px;font-size:12px;color:var(--muted)}button[aria-selected=true]{background:var(--teal);color:white}.candidate{display:grid;grid-template-columns:190px 1fr;gap:20px;align-items:center;margin:18px 0}.candidate[hidden]{display:none}.motion{width:100%;background:#e8edef;border-radius:9px}.candidate-copy p{font-size:14px}.counts{color:var(--muted);font-size:12px}code{font:11px/1.6 Consolas,monospace;overflow-wrap:anywhere;display:block;margin-top:12px;color:#647271}.link{display:inline-block;margin-top:14px;color:var(--teal);font-size:13px;text-decoration:none}.notice{font-size:12px;color:var(--muted);background:#f6f3ec;padding:11px 14px;border-radius:8px}.next{margin-top:24px;padding-top:20px;border-top:1px solid var(--line)}.next h2{font-size:18px}.steps{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.steps div{font-size:12px;color:var(--muted)}.steps b{display:block;color:var(--ink);font-size:14px;margin-bottom:5px}.links{margin-top:20px;display:flex;gap:18px;flex-wrap:wrap}.links a{color:var(--teal);font-size:13px;text-decoration:none}details{margin-top:18px;font-size:13px;color:var(--muted)}details a{display:inline-block;margin:10px 10px 0 0;color:var(--teal)}@media(max-width:1000px){main{grid-template-columns:1fr;padding:0 18px 30px}header{padding:22px 18px}.art-card>img{max-height:620px}.stage{display:none}}@media(max-width:550px){.candidate{grid-template-columns:125px 1fr;gap:13px}.work-card{padding:17px}.stats{gap:6px}.stat{padding:10px}.steps{grid-template-columns:1fr}h1{font-size:23px}}
</style><header><div><div class="eyebrow">CAT MAID / LIVE2D 制作记录</div><h1>外观已确认，开始拆解动作</h1></div><span class="stage">01 模板分析已完成</span></header><main>
<section class="art-card"><img src="../concept-v003.png" alt="用户已确认的第三版猫耳女仆立绘"><div class="caption"><b>第三版 · 外观基准</b><span>新角色尚未分层绑定</span></div></section>
<section class="work-card"><div class="eyebrow">本阶段结论</div><h2>组合参考，为新立绘重新绑定</h2><p class="intro">保留现在的脸、长裙、双马尾和坐姿。参考已有模型的控制组织与动作，再重建适合这张图的网格和关键形状。</p><div class="stats"><div class="stat"><strong>144</strong><span>指定范围内已渲染模型</span></div><div class="stat"><strong>5</strong><span>深入检查的候选</span></div><div class="stat"><strong>2,001</strong><span>候选实测姿态</span></div></div>
<div class="tabs" role="tablist" aria-label="动作参考模型">__TABS__</div>__PANELS__
<p class="notice">右侧动画是源模型的原待机，用于分析动作。左侧是已确认立绘，目前仍为静态图；这里没有把整图晃动当作新角色绑定。</p>
<div class="next"><div class="eyebrow">02 分层与可动草稿 · 待制作</div><h2>先解决接缝与支撑，再加表情</h2><div class="steps"><div><b>01 · 隐藏区域补齐</b>拆分前后发、裙摆、双腿、脸和桌子，补足运动时露出的内容。</div><div><b>02 · 小幅动作</b>桌子与人物共同悬浮，手掌贴住桌面，双腿与脚踝分别控制。</div><div><b>03 · 动作检查</b>检查发尾、裙摆和双腿遮挡，再加入眨眼、猫耳与尾巴运动。</div></div></div>
<div class="links"><a href="制作方案.md" target="_blank">完整分析与限制 ↗</a><a href="reuse-map.json" target="_blank">控制映射记录 ↗</a></div><details><summary>查看全部 144 个候选的图录</summary>__CATALOG__</details>
</section></main><script>
document.querySelectorAll('[role=tab]').forEach(tab=>tab.addEventListener('click',()=>{document.querySelectorAll('[role=tab]').forEach(t=>t.setAttribute('aria-selected',String(t===tab)));document.querySelectorAll('[role=tabpanel]').forEach((p,i)=>p.hidden=i!==Number(tab.dataset.index));}));
</script></html>'''
    doc=doc.replace('__TABS__',''.join(tabs)).replace('__PANELS__',''.join(panels)).replace('__CATALOG__',''.join(f'<a href="catalog/catalog-{i:02d}.jpg" target="_blank">图录 {i}</a>' for i in range(1,7)))
    (HERE/'review.html').write_text(doc,encoding='utf-8')
    print('Review and source binding map written.')

if __name__=='__main__':main()
