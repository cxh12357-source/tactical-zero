'use strict';
// Canvas-native extension of the existing sprites. Static terrain is baked once per map.
const Art={
 explosions:[],
 themes:{sector07:{ground:'#354944',edge:'#52635a',wall:'#8b9a85',accent:'#e3bd7a',metal:'#52736c'},docks:{ground:'#304b54',edge:'#52707a',wall:'#92a9ac',accent:'#e4b978',metal:'#457b89'},ruins:{ground:'#554d42',edge:'#756a54',wall:'#ad9e83',accent:'#d9c393',metal:'#768275'},outpost:{ground:'#344e40',edge:'#527355',wall:'#93a483',accent:'#bdd68a',metal:'#537565'}},
 round(g,x,y,w,h,r,fill,stroke){g.beginPath();if(g.roundRect)g.roundRect(x,y,w,h,r);else g.rect(x,y,w,h);if(fill){g.fillStyle=fill;g.fill()}if(stroke){g.strokeStyle=stroke;g.stroke()}},
 terrain(g){Scenery.draw(g)},
 weapon(g,type,x,y,scale=1,color){const w=TZWorld.W[type],c=color||w.color;g.save();g.translate(x,y);g.scale(scale,scale);g.lineWidth=2;g.lineJoin='round';g.lineCap='round';
  const base='#253b3d',dark='#13262c';this.round(g,-15,-5,30,10,3,base,dark);g.fillStyle=c;g.fillRect(-8,-4,17,3);g.fillStyle=dark;g.fillRect(-9,5,6,11);g.fillRect(13,-2,w.barrel-18,4);
  if(w.kind==='pistol'){g.fillStyle=c;g.fillRect(-13,-7,29,5)}else{g.fillStyle=base;g.fillRect(-27,-6,11,11);g.strokeStyle=c;g.beginPath();g.moveTo(-26,-6);g.lineTo(-26,5);g.stroke();g.fillStyle=dark;g.fillRect(0,5,7,w.kind==='smg'?15:10)}
  if(w.kind==='dmr'){this.round(g,-6,-12,17,5,2,dark,c);g.fillStyle=c;g.fillRect(w.barrel-9,-3,5,6)}
  if(w.kind==='lmg'){this.round(g,-1,5,16,16,3,c,dark);g.strokeStyle=dark;g.beginPath();g.moveTo(24,2);g.lineTo(33,12);g.stroke()}
  if(w.kind==='shotgun'){g.fillStyle='#bc9871';g.fillRect(5,3,15,4);g.fillRect(16,-4,18,3)}
  if(w.kind==='rail'){for(let i=0;i<4;i++){g.fillStyle=c;g.fillRect(3+i*7,-7,3,14)}g.fillStyle='#edffff';g.fillRect(0,-1,43,2);g.fillStyle=c;g.fillRect(38,-5,8,10)}
  if(w.kind==='launcher'){this.round(g,-7,-11,28,22,6,c,dark);this.round(g,15,-8,16,16,3,base,dark);g.fillStyle='#ffdda1';g.fillRect(29,-6,3,12)}
  if(w.kind==='rotary'){g.fillStyle=c;for(let i=-1;i<=1;i++)g.fillRect(8,-1+i*5,29,3);this.round(g,-17,-10,22,20,4,base,dark);g.fillStyle=c;g.fillRect(-12,-6,6,12)}
  g.restore();
 },
 actor(a,friendly,dead,targetContext){const g=targetContext||ctx,w=TZTactics.gunStats(a.weapon,a.mod),look=OperatorArt.look(a),ink='#182e38',cloth=look?.cloth||(friendly?'#66baa9':a.elite?'#d8b16c':'#c98779'),vest=look?.vest||(friendly?'#345b60':a.elite?'#646053':'#665258'),helmet=look?.helmet||(friendly?'#c0d7b4':a.elite?'#ebce8c':'#d8b3a0');
  const moving=dead?0:Math.min(1,a.moving??(a.path?.length ? .6 : 0)),walk=Math.sin(time*13)*moving,kick=!dead&&a.flash>0?2:0,reloadMove=!dead&&a.reload>0&&w.reload>0?Math.sin(Math.PI*Math.max(0,Math.min(1,1-a.reload/w.reload))):0;
  g.save();g.translate(a.x,a.y);if(dead)g.globalAlpha*=.48;
  g.fillStyle='#10263265';g.beginPath();g.ellipse(0,9,24,17,0,0,TAU);g.fill();if(look&&!dead){g.strokeStyle=friendly?'#78ead1':'#ed977e';g.lineWidth=2;g.beginPath();g.ellipse(0,9,26,18,0,0,TAU);g.stroke()}g.rotate(dead?(a.deadAngle||a.angle):a.angle);if(look)g.scale(1,look.width);g.lineWidth=1.5;OperatorArt.mantle(g,look,walk);
  // Boots, trouser cuffs and knee plates alternate under the torso while walking.
  for(const side of [-1,1]){const stride=side*walk*4,yy=side*9;
   this.round(g,-21+stride,yy-4,15,9,3,ink);this.round(g,-20+stride,yy-3,10,6,2,'#46555a');g.fillStyle='#9da69a';g.fillRect(-19+stride,yy-2,2,4);
   this.round(g,-12+stride,yy-4,12,9,3,cloth,ink);this.round(g,-10+stride,yy-3,6,7,2,vest);g.fillStyle='#b4c1ae66';g.fillRect(-9+stride,yy-2,3,1);
  }
  g.save();if(!dead)g.translate(-kick,Math.sin(time*3)*.35*(1-moving));
  // Compact field pack with compression straps, rolled bedding and a radio antenna.
  if(look)OperatorArt.pack(g,look);else{this.round(g,-25,-11,14,23,4,vest,ink);this.round(g,-26,-9,6,19,2,'#789486',ink);g.fillStyle='#263e44';g.fillRect(-24,-4,4,3);g.fillRect(-24,5,4,3);
  this.round(g,-21,-17,8,9,2,'#30454f',ink);g.strokeStyle=ink;g.lineWidth=1.5;g.beginPath();g.moveTo(-18,-16);g.lineTo(-21,-25);g.stroke();g.fillStyle=friendly?'#99e2cb':'#e8c490';g.fillRect(-19,-15,3,2);}
  this.round(g,-17,-16,29,32,9,cloth,ink);this.round(g,-14,-12,20,25,5,vest,ink);
  // Shoulder harness, center buckle, three raised magazine pouches and utility knife.
  g.fillStyle='#bac3a080';g.fillRect(-12,-13,4,27);g.fillRect(0,-13,4,27);g.fillStyle='#203c44';g.fillRect(-13,-2,18,4);this.round(g,-7,-3,5,6,1,'#d1c7a2',ink);
  for(const yy of [-10,5]){this.round(g,-10,yy,7,6,1,cloth,ink);g.fillStyle='#d0d9b28c';g.fillRect(-9,yy+1,5,1)}
  this.round(g,0,5,5,8,1,cloth,ink);this.round(g,-14,13,17,4,1,ink);g.fillStyle='#b2beb4';g.fillRect(-9,14,8,1);
  for(const side of [-1,1]){this.round(g,-3,side<0?-18:10,12,8,3,vest,ink);g.fillStyle=friendly?'#b6ecdc':a.elite?'#ffe3a1':'#efb5a1';g.fillRect(0,side<0?-17:15,6,2);if(a.elite)g.fillRect(0,side<0?-14:12,6,1)}
  if(look)OperatorArt.shoulders(g,look);
  // Bent sleeves and separate gloves keep both the grip and the supporting hand readable.
  const supportX=w.kind==='pistol'?13:23,supportY=5+reloadMove*7,handX=supportX-reloadMove*18;
  g.lineCap='round';g.lineJoin='round';g.beginPath();g.moveTo(3,-13);g.lineTo(11,-10);g.lineTo(17,-5);g.moveTo(3,13);g.lineTo(10,12);g.lineTo(handX,supportY);g.strokeStyle=ink;g.lineWidth=9;g.stroke();g.strokeStyle=cloth;g.lineWidth=6;g.stroke();g.lineWidth=1.5;
  this.weapon(g,a.weapon,15,0,.82);this.round(g,13,-8,7,6,2,'#334b51',ink);this.round(g,handX-3,supportY-3,7,6,2,'#334b51',ink);g.fillStyle='#c2c9b2';g.fillRect(14,-7,3,1);
  // Segmented helmet, cheek cover, two goggles, side ear protection and a small mount.
  if(look)OperatorArt.head(g,look);else{g.fillStyle=ink;g.beginPath();g.arc(9,0,11,0,TAU);g.fill();g.fillStyle=helmet;g.beginPath();g.arc(9,-1,9,0,TAU);g.fill();
  g.strokeStyle='#697c7377';g.lineWidth=1.2;g.beginPath();g.arc(8,-1,6,-2.2,1.4);g.stroke();g.fillStyle='#edf0d57a';g.fillRect(3,-6,6,2);this.round(g,6,-10,6,3,1,'#40575b');
  this.round(g,7,-11,6,4,1,vest,ink);this.round(g,7,7,6,4,1,vest,ink);this.round(g,13,-8,6,15,2,ink);g.fillStyle=friendly?'#a6e7e5':a.elite?'#f9d99c':'#b7d9e4';g.fillRect(15,-6,2,4);g.fillRect(15,1,2,4);g.fillStyle='#efffff';g.fillRect(15,-6,1,2);g.fillRect(15,1,1,2);this.round(g,17,-2,3,3,1,'#93a89f');
  if(a.elite){g.fillStyle='#ffe6ac';g.fillRect(2,-3,3,6)}}
  g.restore();
  if(!dead&&a.flash>0){g.fillStyle=w.color;g.beginPath();g.moveTo(w.barrel+7-kick,-6);g.lineTo(w.barrel+24-kick,-2);g.lineTo(w.barrel+11-kick,7);g.closePath();g.fill();g.fillStyle='#fff4d4';g.fillRect(w.barrel+6-kick,-2,13,4)}g.restore();
  ClassUI.badge(g,a,dead);
  if(!dead&&!friendly&&a.name){g.save();g.font='bold 11px system-ui';g.textAlign='center';g.fillStyle=a.npc?'#ffd580':'#e8dbca';g.fillText(a.npc?'NPC · 巡逻兵':a.name,a.x,a.y-36);g.restore()}
 },
 crate(c){const g=ctx,w=W[c.type],special=w.tier==='airdrop',color=special?w.color:'#c6b88c';g.save();g.translate(c.x,c.y);g.fillStyle='#122b3066';g.beginPath();g.ellipse(0,13,30,13,0,0,TAU);g.fill();if(special){g.globalAlpha=.18+.07*Math.sin(time*3);g.fillStyle=color;g.beginPath();g.arc(0,0,38,0,TAU);g.fill();g.globalAlpha=1;g.strokeStyle=color;g.lineWidth=2;g.beginPath();g.arc(0,0,35+Math.sin(time*2)*2,0,TAU);g.stroke()}this.round(g,-27,-19,54,38,6,'#233e3e',color);g.fillStyle=color;g.fillRect(-25,-17,50,5);this.weapon(g,c.type,1,0,.75);g.font='bold 12px system-ui';g.textAlign='center';g.fillStyle=color;g.fillText(w.label,0,-30);if(special){g.font='10px monospace';g.fillText('AIR-DROP ONLY',0,34)}g.restore()},
 drop(d){const g=ctx,remaining=Math.max(0,(d.landAt||0)-time),color='#f1c885';g.save();g.translate(d.x,d.y);g.strokeStyle=color;g.fillStyle='#e2ba7317';g.lineWidth=2;g.beginPath();g.arc(0,0,42,0,TAU);g.fill();g.stroke();g.setLineDash([7,7]);g.beginPath();g.arc(0,0,54+Math.sin(time*2)*3,0,TAU);g.stroke();g.setLineDash([]);
  if(remaining){g.font='bold 15px monospace';g.textAlign='center';g.fillStyle=color;g.fillText('空投降落 '+Math.ceil(remaining)+'s',0,-63)}else{this.round(g,-23,-23,46,46,6,'#294649',color);g.fillStyle=color;g.fillRect(-5,-20,10,40);g.fillRect(-20,-5,40,10);g.font='bold 12px system-ui';g.textAlign='center';g.fillText(W[d.weapon]?.tier==='airdrop'?'特殊武器空投':'常规武器补给',0,-62)}g.restore()},
 projectile(b){const g=ctx,w=W[b.weapon],speed=Math.hypot(b.vx,b.vy),tail=w.kind==='rail'?90:w.kind==='launcher'?16:27,dx=b.vx/speed*Math.min(tail,b.travelled||30),dy=b.vy/speed*Math.min(tail,b.travelled||30);g.strokeStyle=w.tier==='airdrop'?w.color:(b.color||w.color);g.lineCap='round';g.lineWidth=w.tier==='airdrop'?3:1.8;g.beginPath();g.moveTo(b.x-dx,b.y-dy);g.lineTo(b.x,b.y);g.stroke();if(w.tier==='airdrop'){g.strokeStyle='#f6fff2';g.lineWidth=1;g.stroke();if(w.kind==='launcher'){g.fillStyle=w.color;g.beginPath();g.arc(b.x,b.y,5,0,TAU);g.fill()}}g.lineCap='butt'},
 blasts(){const events=gameMode==='multi'?(typeof TZNet!=='undefined'?TZNet.state?.effects||[]:[]):this.explosions;for(const e of events){const age=time-e.at;if(age<0||age>.5||!worldGeometry.revealed(player,e,time,false))continue;const t=age/.5;ctx.save();ctx.globalAlpha=(1-t)*.65;ctx.fillStyle='#ffdca2';ctx.strokeStyle='#ffb578';ctx.lineWidth=4*(1-t)+1;ctx.beginPath();ctx.arc(e.x,e.y,Math.max(2,e.radius*t),0,TAU);if(t<.3)ctx.fill();ctx.stroke();ctx.restore()}this.explosions=this.explosions.filter(e=>time-e.at<.5)},
 rarityHUD(){if(!player)return;const w=W[player.weapon],el=$('weaponName');el.style.color=w.color;$('weaponTier').textContent=w.tier==='airdrop'?'空投限定':w.tier==='rare'?'稀有装备':'战场装备'}
};


