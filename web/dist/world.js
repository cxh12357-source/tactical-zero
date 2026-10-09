/* Shared map and collision rules: browser + authoritative server. */
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.TZWorld=api})(typeof globalThis==='object'?globalThis:this,function(){
'use strict';
const MAPS={
 sector07:{id:'sector07',name:'禁区仓库',code:'SECTOR 07',description:'琥珀灯光下的工业迷宫：货架窄巷、装卸带与侧翼维修通道。',floor:[64,72,85],wall:'#8997a8',metal:'#ce843f',grass:'#465e55',
 walls:[[6,2,1,5],[6,10,1,7],[12,1,1,4],[12,8,1,7],[19,4,1,5],[19,12,1,6],[2,8,3,1],[8,6,4,1],[9,16,6,1],[15,10,4,1],[21,6,4,1],[22,13,5,1]],
 crates:[[3,4,1,2],[9,11,1,2],[16,3,2,1],[16,14,1,2],[23,9,2,1],[23,16,1,1]],
 brush:[[2,13,3,2],[8,3,3,2],[14,6,3,2],[21,10,4,2],[9,17,4,2],[2,2,2,1]]},
 docks:{id:'docks',name:'港区货场',code:'SECTOR 08',description:'蓝潮码头：北岸双栈桥、纵向集装箱长廊与南侧装卸场。',floor:[103,133,143],wall:'#829aa9',metal:'#d78756',grass:'#4d8070',
 water:[[1,1,8,2],[11,1,6,2],[19,1,8,2]],
 walls:[[1,1,8,2],[11,1,6,2],[19,1,8,2],[3,11,2,1],[16,13,1,4]],
 crates:[[5,5,2,5],[11,6,2,5],[17,5,2,5],[22,7,3,2],[7,14,6,2],[19,14,5,2]],
 brush:[[2,13,2,2],[7,5,3,1],[19,5,3,1],[9,11,3,2],[21,17,4,2],[13,3,2,2]]},
 ruins:{id:'ruins',name:'荒废街区',code:'SECTOR 09',description:'落日旧城：十字主街、四处破损院落与车辆路障，绕楼包抄。',floor:[155,126,108],wall:'#ca9e80',metal:'#717ea1',grass:'#818458',
 walls:[[4,3,6,1],[4,3,1,4],[9,5,1,3],[18,3,6,1],[23,3,1,4],[18,6,3,1],[4,12,1,5],[4,16,6,1],[8,12,2,1],[17,13,1,4],[17,16,7,1],[23,12,1,3]],
 crates:[[6,9,3,1],[16,9,3,1],[12,4,1,2],[13,14,1,3]],
 brush:[[2,13,2,2],[6,4,3,3],[10,12,2,3],[18,4,3,2],[20,11,3,2],[18,17,4,2],[2,7,2,2]]},
 outpost:{id:'outpost',name:'山地哨站',code:'SECTOR 10',description:'青岚山谷：错落岩脊、曲折土径与林间营地，多条草丛侧翼。',floor:[76,114,89],wall:'#88969d',metal:'#5b8a96',grass:'#427d55',
 walls:[[6,3,3,2],[8,5,2,2],[13,2,2,4],[19,4,4,2],[21,6,2,3],[5,10,3,2],[7,12,3,2],[13,11,2,3],[16,14,4,2],[23,12,2,4]],
 crates:[[3,4,2,2],[17,2,2,2],[10,8,2,2],[18,9,2,2]],
 brush:[[2,13,3,2],[3,2,3,1],[9,4,3,1],[16,5,3,2],[10,11,3,2],[20,13,2,3],[8,16,5,2]]}
};
const W={
 USP:{label:'USP',desc:'半自动手枪',mag:12,reserve:60,damage:30,interval:.26,reload:1.4,speed:1700,spread:.013,recoil:.019,maxRecoil:.13,pellets:1,range:1050,barrel:27,color:'#c5d4c5',head:3.2},
 AK47:{label:'AK-47',desc:'突击步枪',mag:30,reserve:120,damage:36,interval:.105,reload:2.15,speed:2100,spread:.018,recoil:.024,maxRecoil:.25,pellets:1,range:1400,barrel:39,color:'#d9b27d',head:3},
 MP6:{label:'MP6',desc:'高射速冲锋枪',mag:35,reserve:140,damage:18,interval:.062,reload:1.65,speed:1650,spread:.026,recoil:.009,maxRecoil:.13,pellets:1,range:850,barrel:31,color:'#9fbfc6',head:3},
 AWP:{label:'AWP',desc:'自动瞄准 · 仅 5 发',mag:5,reserve:0,damage:115,interval:1.38,reload:2.7,speed:4200,spread:.065,recoil:.045,maxRecoil:.14,pellets:1,range:1900,barrel:47,color:'#bfd38d',head:3},
 Shotgun:{label:'SHOTGUN',desc:'泵动霰弹枪',mag:6,reserve:30,damage:15,interval:.9,reload:2.4,speed:1450,spread:.16,recoil:.035,maxRecoil:.16,pellets:8,range:540,barrel:38,color:'#d6ad94',head:2},
 M4A1:{label:'M4A1',desc:'均衡步枪 · 低后坐力',mag:30,reserve:120,damage:27,interval:.095,reload:1.8,speed:2200,spread:.009,recoil:.009,maxRecoil:.09,pellets:1,range:1250,barrel:39,color:'#8cdbc5',head:2.7,kind:'rifle'},
 VECTOR:{label:'VECTOR',desc:'近距冲锋枪 · 极高射速',mag:25,reserve:125,damage:16,interval:.046,reload:1.45,speed:1600,spread:.022,recoil:.008,maxRecoil:.12,pellets:1,range:620,barrel:29,color:'#8dd5ed',head:2.5,kind:'smg'},
 DMR:{label:'DMR-14',desc:'精确步枪 · 高单发伤害',mag:10,reserve:40,damage:64,interval:.46,reload:2.1,speed:2800,spread:.005,recoil:.025,maxRecoil:.11,pellets:1,range:1650,barrel:46,color:'#c5c2ed',head:2.4,kind:'dmr'},
 RPK:{label:'RPK-60',desc:'轻机枪 · 60 发压制火力',mag:60,reserve:180,damage:29,interval:.12,reload:3.1,speed:2000,spread:.025,recoil:.017,maxRecoil:.21,pellets:1,range:1350,barrel:44,color:'#e4bf85',head:2.3,kind:'lmg'},
 RAIL:{label:'极光 RAIL',desc:'空投专属 · 直线贯穿 3 个目标',mag:6,reserve:0,damage:84,interval:1.05,reload:0,speed:3600,spread:.003,recoil:.012,maxRecoil:.05,pellets:1,range:1700,barrel:49,color:'#8eeced',head:1.8,kind:'rail',tier:'airdrop',limited:true,pierce:3},
 NOVA:{label:'烈阳 NOVA',desc:'空投专属 · 范围爆炸，小心误伤自己',mag:4,reserve:0,damage:78,interval:1.15,reload:0,speed:720,spread:.012,recoil:.03,maxRecoil:.08,pellets:1,range:780,barrel:36,color:'#ffbd7d',head:1,kind:'launcher',tier:'airdrop',limited:true,blast:100},
 CYCLONE:{label:'飓风 CYCLONE',desc:'空投专属 · 三联扇形弹幕',mag:24,reserve:0,damage:18,interval:.13,reload:0,speed:2100,spread:.006,recoil:.008,maxRecoil:.09,pellets:3,range:850,barrel:41,color:'#e8a5db',head:1.8,kind:'rotary',tier:'airdrop',limited:true,fan:.065}
};
for(const [type,w]of Object.entries(W)){w.tier||=type==='AWP'?'rare':'field';w.kind||=({USP:'pistol',AK47:'rifle',MP6:'smg',AWP:'dmr',Shotgun:'shotgun'})[type];if(type==='AWP')w.limited=true}
const FIELD_WEAPONS=['AK47','MP6','Shotgun','M4A1','VECTOR','DMR','RPK'],DROP_WEAPONS=['RAIL','NOVA','CYCLONE'];
function rollWeapon(random=Math.random,airdrop=false){const n=Math.max(0,Math.min(.999999,random()));return airdrop?DROP_WEAPONS[Math.floor(n*DROP_WEAPONS.length)]:n<.04?'AWP':FIELD_WEAPONS[Math.floor((n-.04)/.96*FIELD_WEAPONS.length)]}
// A four-delivery cycle guarantees ordinary guns remain the majority.
function rollSupplyWeapon(serial=0,random=Math.random){const groups=[['AK47','M4A1'],['MP6','VECTOR'],['Shotgun','DMR','RPK'],DROP_WEAPONS],pool=groups[serial%4];return pool[Math.floor(Math.max(0,Math.min(.999999,random()))*pool.length)]}
const CONFIG={tile:64,cols:28,rows:20,speed:188,radius:16,visionRange:660,nearVision:115,halfFov:Math.PI*.39,grassRevealDistance:52,shotReveal:.9,maxPlayers:4,tickRate:30,snapshotRate:15,scoreLimit:7,matchSeconds:300,respawnSeconds:4,spawnShield:2,reconnectSeconds:20,bagSlots:8,searchSeconds:1.2};
CONFIG.airdrop={first:35,interval:65,landing:6,max:2};
CONFIG.mapRevision='clear-view-3.9';
const POIS={
 sector07:[['仓库','arms',9,3],['医务室','med',3,6],['维修间','tools',16,6],['通信站','tech',23,16]],
 docks:[['海关办公室','tech',3,4],['军需堆场','arms',19,6],['急救站','med',10,17],['机修棚','tools',24,11]],
 ruins:[['废弃诊所','med',7,5],['公寓储藏室','tech',18,6],['军警岗亭','arms',22,12],['修理铺','tools',11,16]],
 outpost:[['哨站军械库','arms',17,3],['野战医院','med',4,12],['雷达站','tech',23,16],['后勤营地','tools',10,6]]
};
const LOOT={scrap:{name:'机械零件',value:35,color:'#b8bf9a'},circuit:{name:'电子元件',value:90,color:'#79d4cb'},intel:{name:'加密情报',value:180,color:'#e8bc78'},med:{name:'医疗物资',value:60,color:'#a5d899'}};
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
const distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
const angleDiff=(a,b)=>Math.atan2(Math.sin(a-b),Math.cos(a-b));
class World{
 constructor(mapId,crane=false){this.map=MAPS[mapId]||MAPS.sector07;this.crane=crane&&this.map.id==='docks';if(this.crane)this.map={...this.map,crates:this.map.crates.map((r,i)=>i===5?[19,11,5,2]:r)};this.grid=new Uint8Array(CONFIG.cols*CONFIG.rows);this.grass=new Uint8Array(this.grid.length);
  const block=(x,y,w,h)=>{for(let j=y;j<y+h;j++)for(let i=x;i<x+w;i++)this.grid[j*CONFIG.cols+i]=1};
  block(0,0,28,1);block(0,19,28,1);block(0,0,1,20);block(27,0,1,20);
  for(const r of [...this.map.walls,...this.map.crates])block(...r);
  for(const [x,y,w,h]of this.map.brush)for(let j=y;j<y+h;j++)for(let i=x;i<x+w;i++)if(!this.blocked(i,j))this.grass[j*28+i]=1;
  this.floor=[];for(let y=1;y<19;y++)for(let x=1;x<27;x++)if(!this.blocked(x,y))this.floor.push({x:(x+.5)*64,y:(y+.5)*64});
 }
 blocked(x,y){return x<0||y<0||x>=28||y>=20||this.grid[y*28+x]!==0}
 hidden(p){return this.grass[Math.floor(p.y/64)*28+Math.floor(p.x/64)]===1}
 stand(x,y,r=16){for(let j=Math.floor((y-r)/64);j<=Math.floor((y+r)/64);j++)for(let i=Math.floor((x-r)/64);i<=Math.floor((x+r)/64);i++)if(this.blocked(i,j)){const nx=clamp(x,i*64,(i+1)*64),ny=clamp(y,j*64,(j+1)*64);if((x-nx)**2+(y-ny)**2<r*r)return false}return true}
 move(p,x,y,dt){const n=Math.max(1,Math.ceil(Math.hypot(x,y)*dt/8));for(let i=0;i<n;i++){if(this.stand(p.x+x*dt/n,p.y))p.x+=x*dt/n;if(this.stand(p.x,p.y+y*dt/n))p.y+=y*dt/n}}
 cast(a,b){const dx=b.x-a.x,dy=b.y-a.y;let x=Math.floor(a.x/64),y=Math.floor(a.y/64),t=0;const sx=dx>0?1:-1,sy=dy>0?1:-1,stepX=dx===0?Infinity:Math.abs(64/dx),stepY=dy===0?Infinity:Math.abs(64/dy);let nx=dx===0?Infinity:((x+(dx>0?1:0))*64-a.x)/dx,ny=dy===0?Infinity:((y+(dy>0?1:0))*64-a.y)/dy;
  for(let i=0;i<64;i++){if(this.blocked(x,y))return t;if(nx>1&&ny>1)return 1;if(Math.abs(nx-ny)<1e-10){t=nx;if(this.blocked(x+sx,y)||this.blocked(x,y+sy))return t;x+=sx;y+=sy;nx+=stepX;ny+=stepY}else if(nx<ny){t=nx;x+=sx;nx+=stepX}else{t=ny;y+=sy;ny+=stepY}}return 1;
 }
 los(a,b){return this.cast(a,b)>=1}
 visionRange(now=0){return now<(this.darkUntil||0)?320:CONFIG.visionRange}
 // Human presentation has no fog, direction cone, or vision radius. Grass concealment remains separate.
 revealed(observer,target,now=0,conceal=true){return !conceal||!this.hidden(target)||(target.revealUntil||0)>now||distance(observer,target)<=CONFIG.grassRevealDistance}
 visible(observer,target,now=0,conceal=true){const d=distance(observer,target);if(d>this.visionRange(now)||!this.los(observer,target))return false;if(conceal&&this.hidden(target)&&(target.revealUntil||0)<=now&&d>CONFIG.grassRevealDistance)return false;return d<=CONFIG.nearVision||Math.abs(angleDiff(Math.atan2(target.y-observer.y,target.x-observer.x),observer.angle))<=CONFIG.halfFov}
 nearest(x,y){let best=this.floor[0],n=Infinity;for(const p of this.floor){const d=Math.hypot(x-p.x,y-p.y);if(d<n){n=d;best=p}}return {...best}}
 pois(){return POIS[this.map.id].map(([name,type,x,y],i)=>({id:'poi'+i,name,type,...this.nearest((x+.5)*64,(y+.5)*64),opened:false,progress:0}))}
 spawn(others=[],previous=null,random=Math.random){
  // Randomize among safe floor cells, rather than always choosing the same farthest corner.
  let candidates=this.floor.filter(p=>this.stand(p.x,p.y)&&others.every(other=>distance(p,other)>CONFIG.radius*2));
  if(!candidates.length)throw Error('地图没有可用复活点');
  if(previous){const fresh=candidates.filter(p=>distance(p,previous)>=256);if(fresh.length)candidates=fresh;else{const different=candidates.filter(p=>distance(p,previous)>1);if(different.length)candidates=different}}
  const safe=candidates.filter(p=>others.every(other=>distance(p,other)>=384));
  if(safe.length){const covered=safe.filter(p=>others.every(other=>!this.los(p,other)));candidates=covered.length?covered:safe}
  else if(others.length){const scored=candidates.map(p=>({p,score:Math.min(...others.map(other=>distance(p,other)))})),best=Math.max(...scored.map(x=>x.score));candidates=scored.filter(x=>x.score>=best*.9).map(x=>x.p)}
  return {...candidates[Math.min(candidates.length-1,Math.floor(random()*candidates.length))]};
 }
}
function segmentCircle(a,b,p,r=16){const dx=b.x-a.x,dy=b.y-a.y,fx=a.x-p.x,fy=a.y-p.y,q=dx*dx+dy*dy,c=fx*fx+fy*fy-r*r;if(c<=0)return 0;if(q<1e-9)return null;const k=2*(fx*dx+fy*dy),disc=k*k-4*q*c;if(disc<0)return null;const t=(-k-Math.sqrt(disc))/(2*q);return t>=0&&t<=1?t:null}
return {MAPS,W,CONFIG,POIS,LOOT,World,FIELD_WEAPONS,DROP_WEAPONS,rollWeapon,rollSupplyWeapon,clamp,distance,angleDiff,segmentCircle};
});
