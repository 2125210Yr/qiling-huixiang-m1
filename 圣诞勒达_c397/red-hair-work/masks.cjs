const fs=require('fs');
const sharp=require('C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const base='F:/天命之子/圣诞勒达_c397/c397_02_moc3';
const rig=JSON.parse(fs.readFileSync(base+'/rig-spec-v4.json'));
const atlas=base+'/textures/texture_00.png', W=4096,H=4096;
const hair=rig.drawables.filter(d=>d.partId.includes('HAIR'));
function meshPoly(d,ox=0,oy=0){let p='';for(let i=0;i<d.indices.length;i+=3){p+='<polygon points="'+d.indices.slice(i,i+3).map(k=>`${d.uvs[k*2]*W-ox},${d.uvs[k*2+1]*H-oy}`).join(' ')+'" fill="white"/>';}return p;}
(async()=>{
 let images=[], rows=[];
 for(let i=0;i<hair.length;i++){
  let d=hair[i], xs=d.uvs.filter((v,k)=>k%2===0).map(v=>v*W),ys=d.uvs.filter((v,k)=>k%2===1).map(v=>v*H);
  let left=Math.max(0,Math.floor(Math.min(...xs))-3),top=Math.max(0,Math.floor(Math.min(...ys))-3);
  let width=Math.min(W,Math.ceil(Math.max(...xs))+3)-left,height=Math.min(H,Math.ceil(Math.max(...ys))+3)-top;
  let svg=Buffer.from(`<svg width="${width}" height="${height}">${meshPoly(d,left,top)}</svg>`);
  let img=await sharp(atlas).extract({left,top,width,height}).composite([{input:svg,blend:'dest-in'}]).png().toBuffer();
  img=await sharp(img).resize(130,120,{fit:'inside'}).png().toBuffer();
  images.push({input:img,left:(i%9)*150+10,top:Math.floor(i/9)*150+25});
  images.push({input:Buffer.from(`<svg width="150" height="22"><text x="5" y="16" fill="white" font-size="13">${d.id}</text></svg>`),left:(i%9)*150,top:Math.floor(i/9)*150});
  rows.push({id:d.id,left,top,width,height});
 }
 await sharp({create:{width:1350,height:Math.ceil(hair.length/9)*150,channels:4,background:'#45454b'}}).composite(images).png().toFile(__dirname+'/hair-masked-contact.png');
 fs.writeFileSync(__dirname+'/hair-bounds.json',JSON.stringify(rows,null,2));
 console.log('Masked contact sheet ready.');
})();
