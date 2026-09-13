"""Package the verified editable/runtime model; no source library writes."""
import hashlib,json,zipfile,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];MODEL=ROOT/'export-v001/model'
sys.stdout.reconfigure(encoding='utf-8')
def read(p):return json.loads(p.read_text(encoding='utf-8'))
for name in ['native-summary.json','browser-summary.json','rgb-preservation.json']:assert read(ROOT/'verification'/name)['ok'],name
native=read(MODEL/'verification.json');assert native['passed']
assert hashlib.sha256((MODEL/'model.moc3').read_bytes()).hexdigest()==native['moc3Sha256']
assert hashlib.sha256((MODEL/'model.cmo3').read_bytes()).hexdigest()==native['cmo3Sha256']
refs=read(MODEL/'model.model3.json')['FileReferences'];required=[refs['Moc'],*refs['Textures'],refs['DisplayInfo'],*[m['File'] for group in refs['Motions'].values() for m in group]]
assert all((MODEL/file).is_file() for file in required)
prompts=read(ROOT/'assets/prompts.json')
for p in prompts:p['editTarget']=str(ROOT.parents[1]/'concept-v003.png')
(ROOT/'assets/prompts.json').write_text(json.dumps(prompts,ensure_ascii=False,indent=2),encoding='utf-8')
delivery=ROOT/'CatMaid_draft_v001.zip';items=[]
with zipfile.ZipFile(delivery,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in sorted(MODEL.rglob('*')):
        if p.is_file():z.write(p,p.relative_to(MODEL).as_posix());items.append(p.relative_to(MODEL).as_posix())
    z.write(ROOT/'README.md','制作说明.md');items.append('制作说明.md')
    for name in ['native-summary.json','browser-summary.json','rgb-preservation.json']:z.write(ROOT/'verification'/name,'verification/'+name);items.append('verification/'+name)
with zipfile.ZipFile(delivery) as z:assert z.testzip() is None
result=dict(ok=True,archive=delivery.name,archiveBytes=delivery.stat().st_size,archiveSha256=hashlib.sha256(delivery.read_bytes()).hexdigest(),files=items,allRuntimeReferencesExist=True,nativeMocAndCmoHashesMatchExport=True,psdSeparate='CatMaid_layers_v001.psd')
(ROOT/'verification/delivery.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False))
