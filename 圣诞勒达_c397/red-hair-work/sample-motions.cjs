const fs=require('fs');const base='F:/天命之子/圣诞勒达_c397/c397_02_moc3';
function evalCurve(s,t){let pt=s[0],pv=s[1];for(let i=2;i<s.length;){let type=s[i++];if(type===0||type===2||type===3){let nt=s[i++],nv=s[i++];if(t<=nt){if(type===2)return pv;if(type===3)return nv;let u=nt===pt?0:(t-pt)/(nt-pt);return pv+(nv-pv)*Math.max(0,Math.min(1,u));}pt=nt;pv=nv;}else throw Error('Unexpected non-linear segment '+type);}return pv;}
let rows=['neutral'];let summary=[];
for(const type of ['idle','attack','hit','banner']){let motion=JSON.parse(fs.readFileSync(base+'/motions/c397_02_'+type+'.motion3.json'));let n=type==='idle'?61:9;
 for(let i=0;i<n;i++){let t=motion.Meta.Duration*i/(n-1);rows.push([type+'-'+String(i).padStart(3,'0'),...motion.Curves.filter(c=>c.Target==='Parameter').map(c=>c.Id+'='+evalCurve(c.Segments,t))].join('\t'));}
 summary.push({motion:type,samples:n,duration:motion.Meta.Duration});}
const rig=JSON.parse(fs.readFileSync(base+'/rig-spec-v4.json'));
for(const p of rig.parameters.filter(p=>/HAIR|FACE_[XY]/.test(p.id)))for(const side of ['min','max'])rows.push(`${p.id}-${side}\t${p.id}=${p[side]}`);
fs.writeFileSync(__dirname+'/poses.tsv',rows.join('\n'));
fs.writeFileSync(__dirname+'/sampling.json',JSON.stringify({summary,total:rows.length},null,2));console.log(JSON.stringify({summary,total:rows.length}));
