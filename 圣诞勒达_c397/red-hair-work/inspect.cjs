const fs=require('fs');
const sharp=require('C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const path=require('path');
const base='F:/天命之子/圣诞勒达_c397/c397_02_moc3';
const work=__dirname;
const rig=JSON.parse(fs.readFileSync(base+'/rig-spec-v4.json'));
const tileDir='F:/天命之子/dc-extract/moc-recover-c397_02/atlas-work';
(async()=>{
 const ids=rig.drawables.filter(d=>d.partId.includes('HAIR')).map(d=>d.id);
 const all=[]; const tiles=[];
 for(let i=0;i<ids.length;i++){
  const p=tileDir+'/'+ids[i]+'.png';
  const m=await sharp(p).metadata();
  tiles.push({id:ids[i],w:m.width,h:m.height});
  const img=await sharp(p).resize(130,120,{fit:'inside'}).png().toBuffer();
  const col=i%9,row=Math.floor(i/9);
  all.push({input:img,left:col*150+10,top:row*150+25});
  const label=Buffer.from(`<svg width="150" height="22"><text x="5" y="16" fill="white" font-size="13">${ids[i]}</text></svg>`);
  all.push({input:label,left:col*150,top:row*150});
 }
 await sharp({create:{width:1350,height:Math.ceil(ids.length/9)*150,channels:4,background:'#45454b'}}).composite(all).png().toFile(work+'/hair-contact.png');
 const refs=['D_PSD_165','D_PSD_186','D_PSD_193','D_PSD_200'];
 const refComps=[];
 for(let i=0;i<refs.length;i++){
  refComps.push({input:await sharp(tileDir+'/'+refs[i]+'.png').resize(420,440,{fit:'contain',background:'#45454b'}).png().toBuffer(),left:(i%2)*480+30,top:Math.floor(i/2)*480+20});
 }
 await sharp({create:{width:960,height:960,channels:4,background:'#45454b'}}).composite(refComps).png().toFile(work+'/hair-color-input.png');
 fs.writeFileSync(work+'/hair-tiles.json',JSON.stringify(tiles,null,2));
 console.log(JSON.stringify({hairCount:ids.length,atlas:await sharp(base+'/textures/texture_00.png').metadata(),files:['hair-contact.png','hair-color-input.png']},null,2));
})();
