const fs=require('fs');
const path=require('path');
const crypto=require('crypto');
const sharp=require('C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const base='F:/天命之子/圣诞勒达_c397/c397_02_moc3';
const output='F:/天命之子/圣诞勒达_c397/c397_02_redhair';
const generated='C:/Users/Administrator/.codex/generated_images/01a096a0-fc5b-7c60-824d-0567575b131a/exec-c07ddb23-9a46-4ded-a43a-04078a2eb715.png';
const rig=JSON.parse(fs.readFileSync(base+'/rig-spec-v4.json'));
// The hair parts also contain cords, a pale ornament, a gem, and metal beads.
const exclude=new Set(['D_PSD_203','D_PSD_202','D_PSD_201','D_PSD_184','D_PSD_167','D_PSD_168']);
const hair=rig.drawables.filter(d=>d.partId.includes('HAIR')&&!exclude.has(d.id));
const W=4096,H=4096;
function hue(r,g,b){const max=Math.max(r,g,b),min=Math.min(r,g,b),d=max-min;if(d===0)return 0;let h=max===r?(g-b)/d:max===g?2+(b-r)/d:4+(r-g)/d;return (h*60+360)%360;}
function unitHue(h){let x=1-Math.abs((h/60)%2-1);return h<60?[1,x,0]:h<120?[x,1,0]:h<180?[0,1,x]:h<240?[0,x,1]:h<300?[x,0,1]:[1,0,x];}
function polygons(ds){let s='';for(const d of ds)for(let i=0;i<d.indices.length;i+=3)s+='<polygon points="'+d.indices.slice(i,i+3).map(k=>`${d.uvs[k*2]*W},${d.uvs[k*2+1]*H}`).join(' ')+'"/>';return s;}
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
(async()=>{
 if(!fs.existsSync(output)){
  fs.mkdirSync(output,{recursive:true});
  for(const name of ['motions','expressions'])fs.cpSync(base+'/'+name,output+'/'+name,{recursive:true});
  for(const name of ['model.moc3','model.model3.json','model.cdi3.json','id-map.json'])fs.copyFileSync(base+'/'+name,output+'/'+name);
  fs.mkdirSync(output+'/textures');
 }else if(!process.argv.includes('--refresh-texture'))throw Error('Output already exists; use --refresh-texture only for this task output.');
 fs.copyFileSync(generated,__dirname+'/hair-color-reference.png');
 const ref=await sharp(generated).removeAlpha().raw().toBuffer();let hues=[];
 for(let i=0;i<ref.length;i+=3){const r=ref[i],g=ref[i+1],b=ref[i+2];if(r>80&&r<220&&r>g*1.5&&r>b*1.4){let h=hue(r,g,b);hues.push(h>180?h-360:h);}}
 hues.sort((a,b)=>a-b);const targetHue=hues[Math.floor(hues.length/2)];
 const maskSvg=Buffer.from(`<svg width="${W}" height="${H}"><g fill="white" stroke="white" stroke-width="5" stroke-linejoin="round">${polygons(hair)}</g></svg>`);
 const mask=await sharp(maskSvg).ensureAlpha().extractChannel(3).raw().toBuffer();
 const original=await sharp(base+'/textures/texture_00.png').ensureAlpha().raw().toBuffer();
 const out=Buffer.from(original);let changed=0,maxLumaError=0,sumError=0;
 for(let p=0;p<W*H;p++){
  if(!mask[p]||!original[p*4+3])continue;
  let i=p*4,r=original[i],g=original[i+1],b=original[i+2];
  let sourceY=.2126*r+.7152*g+.0722*b,c=Math.max(r,g,b)-Math.min(r,g,b);
  // A strictly increasing tone curve keeps every original strand/highlight shape.
  // Moderate midtone darkening avoids pastel pink while retaining bright gloss.
  let Y=sourceY-24*Math.sin(Math.PI*sourceY/255)**2;
  let h=3+Math.max(0,sourceY-185)/70*6;
  let u=unitHue(h),uy=.2126*u[0]+.7152*u[1]+.0722*u[2],v=u.map(q=>q-uy);
  c*=1.50;
  for(const q of v){if(q>0)c=Math.min(c,(255-Y)/q);else if(q<0)c=Math.min(c,-Y/q);}
  let a=mask[p]/255;
  let nr=Math.round(r*(1-a)+(Y+c*v[0])*a),ng=Math.round(g*(1-a)+(Y+c*v[1])*a),nb=Math.round(b*(1-a)+(Y+c*v[2])*a);
  out[i]=nr;out[i+1]=ng;out[i+2]=nb;
  if(nr!==r||ng!==g||nb!==b){changed++;let e=Math.abs(.2126*nr+.7152*ng+.0722*nb-(sourceY*(1-a)+Y*a));maxLumaError=Math.max(maxLumaError,e);sumError+=e;}
 }
 await sharp(out,{raw:{width:W,height:H,channels:4}}).png().toFile(output+'/textures/texture_00.png');
 let alphaDiff=0,outsideDiff=0;for(let p=0;p<W*H;p++){let i=p*4;if(out[i+3]!==original[i+3])alphaDiff++;if(!mask[p]&&(out[i]!==original[i]||out[i+1]!==original[i+1]||out[i+2]!==original[i+2]))outsideDiff++;}
 const motionHashes=fs.readdirSync(base+'/motions').map(name=>({name,sha256:hash(base+'/motions/'+name),unchanged:hash(base+'/motions/'+name)===hash(output+'/motions/'+name)}));
 const report={source:base,output,method:'Imagegen red palette, warmed to match scarlet clothes. Original per-pixel strand detail retained through a strictly increasing luminance tone curve; alpha retained byte-for-byte.',generatedReference:generated,referenceMedianHueDegrees:targetHue,targetHueDegrees:3,midtoneCurve:'Y - 24*sin(pi*Y/255)^2; strictly increasing',hairMeshCount:hair.length,hairMeshIds:hair.map(d=>d.id),excludedAccessories:[...exclude],dimensions:[W,H],changedRgbPixels:changed,changedAlphaPixels:alphaDiff,changedPixelsOutsideHairMask:outsideDiff,toneCurveMaxRoundingError8bit:maxLumaError,toneCurveMeanRoundingError8bit:sumError/changed,mocUnchanged:hash(base+'/model.moc3')===hash(output+'/model.moc3'),mocSha256:hash(output+'/model.moc3'),motions:motionHashes};
 fs.writeFileSync(output+'/recolor-verification.json',JSON.stringify(report,null,2));
 fs.writeFileSync(__dirname+'/selected-hair.json',JSON.stringify(hair.map(d=>d.id)));
 console.log(JSON.stringify({...report,hairMeshIds:undefined,motions:motionHashes.map(m=>({name:m.name,unchanged:m.unchanged}))},null,2));
})();
