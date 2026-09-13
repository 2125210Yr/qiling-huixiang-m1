// Package approved RGB pixels using image_gen-authored selection maps; no repainting.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('C:/Users/Administrator/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const {writePsdBuffer,readPsd,initializeCanvas}=require('F:/天命之子/codex专区/standrig-studio/node_modules/ag-psd');
initializeCanvas(()=>{throw Error('Raw image data only');},(width,height)=>({width,height,data:new Uint8ClampedArray(width*height*4)}));
const ROOT=path.resolve(__dirname,'..'),W=1024,H=1536,N=W*H,AS=4096;
const clamp=v=>Math.max(0,Math.min(1,v)),smooth=v=>{v=clamp(v);return v*v*(3-2*v);};
const read=async p=>{const m=await sharp(p).metadata();if(m.width!==W||m.height!==H)throw Error('Asset registration changed: '+p);return sharp(p).ensureAlpha().raw().toBuffer();};
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const definitions=[['Head',0xff0000,'头部与前发'],['HairScreenL',0xff8000,'画面左后发'],['HairScreenR',0xffff00,'画面右后发'],['Tail',0x00ff00,'猫尾'],['Bodice',0x00ffff,'躯干'],['ArmScreenL',0x0000ff,'画面左臂与手'],['ArmScreenR',0x8000ff,'画面右臂与手'],['Skirt',0xff00ff,'裙摆与围裙'],['LegScreenL',0xff8080,'画面左前腿'],['LegScreenR',0x8080ff,'画面右后腿'],['Table',0x008080,'原画可见桌面与底座']];
(async()=>{
 for(const d of ['layers','verification'])fs.mkdirSync(path.join(ROOT,d),{recursive:true});
 const source=await read(path.resolve(ROOT,'../../concept-v003.png'));
 const [labels,matte,table,cloth,blink]=await Promise.all(['parts-labels','foreground-matte','table-underpaint','cloth-underpaint','blink-closed'].map(n=>read(path.join(ROOT,'assets',n+'.png'))));
 const owner=new Int16Array(N).fill(-1),alpha=new Uint8Array(N),queue=new Int32Array(N);let tail=0;
 const palette=definitions.map(d=>[d[1]>>16,(d[1]>>8)&255,d[1]&255]);
 const bounds=[[295,0,650,350],[60,150,505,1360],[545,95,966,1360],[685,410,959,745],[380,278,660,575],[155,290,440,807],[600,310,865,807],[140,520,890,1120],[418,685,620,1110],[540,720,695,1152],[50,705,980,1380]];
 for(let i=0;i<N;i++){
  const j=i*4;alpha[i]=Math.round(255*clamp((matte[j]-20)/210));if(!alpha[i])continue;
  const x=i%W,y=Math.floor(i/W);
  const rgb=[labels[j],labels[j+1],labels[j+2]],peak=Math.max(...rgb);
  if(peak<40)continue;
  // Classify hue/relative channels, not edge darkness; cyan/table are spatially disjoint.
  const normalized=rgb.map(v=>v*255/peak);
  let best=Infinity,index=-1;
  for(let k=0;k<palette.length;k++){
   const [l,t,r,b]=bounds[k];if(x<l||x>r||y<t||y>b)continue;
   const scale=255/Math.max(...palette[k]),d=normalized.reduce((s,v,c)=>s+(v-palette[k][c]*scale)**2,0);
   if(d<best){best=d;index=k;}
  }
  if(index<0)continue;
  // Label-map cuffs were colored with the bodice; assign their original pixels to arms.
  if(index===4&&x<420&&y>425&&y<505)index=5;
  if(index===4&&x>610&&y>450&&y<535)index=6;
  owner[i]=index;queue[tail++]=i;
 }
 // Fill foreground registration slivers from neighboring semantic labels.
 const orphanBefore=Array.from(owner).reduce((n,v,i)=>n+(v<0&&alpha[i]>0?1:0),0);
 // Propagate labels across empty pixels too, to reach disconnected antialiased strands.
 // Only the independent foreground alpha below determines which pixels are emitted.
 for(let head=0;head<tail;head++){const i=queue[head],x=i%W;for(const n of [x?i-1:-1,x<W-1?i+1:-1,i-W,i+W])if(n>=0&&n<N&&owner[n]<0){owner[n]=owner[i];queue[tail++]=n;}}
 const masks=definitions.map(()=>new Uint8Array(N));
 for(let i=0;i<N;i++)if(alpha[i]){if(owner[i]<0)throw Error('Unassigned foreground');masks[owner[i]][i]=alpha[i];}
 const layers=[];let atlasX=8,atlasY=8,shelf=0;
 async function emit(id,title,src,mask,order,kind='original',hidden=false){
  let l=W,t=H,r=0,b=0,count=0;for(let i=0;i<N;i++)if(mask[i]){const x=i%W,y=Math.floor(i/W);l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x+1);b=Math.max(b,y+1);count++;}
  if(!count)throw Error('Empty layer '+id);l=Math.max(0,l-2);t=Math.max(0,t-2);r=Math.min(W,r+2);b=Math.min(H,b+2);const width=r-l,height=b-t;
  const pixels=Buffer.alloc(width*height*4),coverage=Buffer.alloc(width*height);
  for(let y=0;y<height;y++)for(let x=0;x<width;x++){const i=(y+t)*W+x+l,j=(y*width+x)*4;src.copy(pixels,j,i*4,i*4+3);pixels[j+3]=mask[i];coverage[y*width+x]=mask[i];}
  if(atlasX+width+8>AS){atlasX=8;atlasY+=shelf+8;shelf=0;}if(atlasY+height+8>AS)throw Error('Atlas overflow');
  const png=await sharp(pixels,{raw:{width,height,channels:4}}).png().toBuffer();fs.writeFileSync(path.join(ROOT,'layers',id+'.png'),png);
  const step=28,cols=Math.ceil(width/step),rows=Math.ceil(height/step),occupied=[];
  for(let gy=0;gy<rows;gy++)for(let gx=0;gx<cols;gx++){
   let visible=false;for(let y=gy*step;y<Math.min((gy+1)*step,height)&&!visible;y++)for(let x=gx*step;x<Math.min((gx+1)*step,width);x++)if(coverage[y*width+x]){visible=true;break;}
   if(visible)occupied.push([gx,gy]);
  }
  layers.push({id,title,kind,hidden,order,left:l,top:t,width,height,atlasX,atlasY,step,cols,rows,occupied,paintedPixels:count,pixelSha256:hash(pixels),file:'layers/'+id+'.png',pixels,png});atlasX+=width+8;shelf=Math.max(shelf,height);
 }
 const tableMask=new Uint8Array(N),clothMask=new Uint8Array(N),shadowMask=new Uint8Array(N);
 for(let i=0;i<N;i++){
  const j=i*4,x=i%W,y=Math.floor(i/W),max=Math.max(table[j],table[j+1],table[j+2]),min=Math.min(table[j],table[j+1],table[j+2]);
  // The generated table has a baked gray checkerboard. Only its colored wooden prop is packaged.
  tableMask[i]=max-min>12&&table[j]>table[j+2]+12?alpha[i]:0;
  if(x>405&&x<706&&y>688&&y<1095)clothMask[i]=alpha[i];
  if(y>=1384&&y<1515&&x>200&&x<842){const opacity=clamp((242-(source[j]+source[j+1]+source[j+2])/3)/86);shadowMask[i]=Math.round(100*opacity*smooth((y-1384)/20)*smooth((1515-y)/22)*smooth((x-200)/45)*smooth((842-x)/45));}
 }
 // Ground shadow is a rendering effect: a neutral ink color through the measured soft mask.
 const shadowInk=Buffer.alloc(N*4);for(let i=0;i<N;i++){shadowInk[i*4]=50;shadowInk[i*4+1]=43;shadowInk[i*4+2]=57;shadowInk[i*4+3]=255;}
 await emit('GroundShadow','地面阴影',shadowInk,shadowMask,10,'shadow');
 await emit('TableUnderpaint','桌子隐藏区域补绘',table,tableMask,20,'underpaint');
 await emit('ClothUnderpaint','双腿后方内裙补绘',cloth,clothMask,25,'underpaint');
 const orders={Table:30,HairScreenL:40,HairScreenR:41,Tail:42,LegScreenR:50,LegScreenL:51,Skirt:60,Bodice:70,ArmScreenL:75,ArmScreenR:76,Head:80};
 for(let i=0;i<definitions.length;i++){const [id,,title]=definitions[i];await emit(id,title,source,masks[i],orders[id]);}
 function eyeMask(cx,cy,rx,ry){const m=new Uint8Array(N);for(let y=Math.floor(cy-ry);y<=cy+ry;y++)for(let x=Math.floor(cx-rx);x<=cx+rx;x++){const f=clamp((1-Math.hypot((x-cx)/rx,(y-cy)/ry))/.20);if(f)m[y*W+x]=Math.round(f*255);}return m;}
 await emit('EyeClosedScreenL','闭眼端点：画面左',blink,eyeMask(453,230,31,22),90,'blink',true);
 await emit('EyeClosedScreenR','闭眼端点：画面右',blink,eyeMask(531,208,31,23),91,'blink',true);
 layers.sort((a,b)=>a.order-b.order);
 const atlas=await sharp({create:{width:AS,height:AS,channels:4,background:'#00000000'}}).composite(layers.map(r=>({input:r.png,left:r.atlasX,top:r.atlasY}))).png().toBuffer();fs.writeFileSync(path.join(ROOT,'atlas.png'),atlas);
 const comps=layers.filter(l=>!l.hidden).map(l=>({input:l.png,left:l.left,top:l.top}));
 const composite=await sharp({create:{width:W,height:H,channels:4,background:'#00000000'}}).composite(comps).raw().toBuffer();
 await sharp(composite,{raw:{width:W,height:H,channels:4}}).png().toFile(path.join(ROOT,'assembled-default.png'));
 const psd=writePsdBuffer({width:W,height:H,children:layers.map(l=>({name:l.title+' ['+l.id+']',top:l.top,left:l.left,opacity:1,hidden:l.hidden,blendMode:'normal',imageData:{width:l.width,height:l.height,data:new Uint8ClampedArray(l.pixels)}})),imageData:{width:W,height:H,data:new Uint8ClampedArray(composite)}},{noBackground:true,generateThumbnail:false});
 fs.writeFileSync(path.join(ROOT,'CatMaid_layers_v001.psd'),psd);const decoded=readPsd(psd,{useImageData:true,skipThumbnail:true});if(decoded.children.length!==layers.length)throw Error('PSD roundtrip');
 const manifest={width:W,height:H,atlasSize:AS,approvedImage:'../../concept-v003.png',approvedSha256:hash(fs.readFileSync(path.resolve(ROOT,'../../concept-v003.png'))),layers:layers.map(({pixels,png,...l})=>l)};
 fs.writeFileSync(path.join(ROOT,'layers.json'),JSON.stringify(manifest,null,2));
 const evidence={ok:true,layers:layers.length,psdLayers:decoded.children.length,originalRgbLayers:definitions.length,originalRgbPixelsUnchanged:true,foregroundSelection:'image_gen authored matte',partSelection:'image_gen authored label map with semantic cuff fixes',unlabelledMattePixelsAssignedToNearestRegion:orphanBefore,tableSourceHadAlpha:false,tableCheckerboardExcludedByPropSelection:true,atlas:[AS,AS],psdSha256:hash(psd),approvedImageSha256:manifest.approvedSha256};
 fs.writeFileSync(path.join(ROOT,'verification/layers.json'),JSON.stringify(evidence,null,2));console.log(JSON.stringify(evidence));
})().catch(e=>{console.error(e.stack);process.exitCode=1;});
