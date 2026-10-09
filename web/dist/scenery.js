'use strict';
// Original procedural environment art. Every static detail is baked into mapCanvas.
const Scenery={
 gallery(){const root=$('mapGallery');if(!root.children.length){for(const map of Object.values(MAPS)){const button=document.createElement('button');button.className='mapTile';button.type='button';button.dataset.map=map.id;const c=document.createElement('canvas');c.width=392;c.height=280;const g=c.getContext('2d');g.save();g.scale(392/WW,280/WH);this.draw(g,map,new TZWorld.World(map.id));g.restore();button.appendChild(c);const label=document.createElement('strong');label.textContent=map.name;button.appendChild(label);const note=document.createElement('span');note.textContent={sector07:'工业窄巷 · 转角突袭',docks:'潮汐码头 · 长廊交火',ruins:'落日旧城 · 绕楼包抄',outpost:'青岚山谷 · 林间穿插'}[map.id];button.appendChild(note);button.addEventListener('click',()=>{selectedMap=map.id;$('mapSelect').value=map.id;try{localStorage.setItem('tactical-map-v1',map.id)}catch(_){}mapInfo()});root.appendChild(button)}}for(const card of root.children){const chosen=card.dataset.map===selectedMap;card.classList.toggle('selected',chosen);card.setAttribute('aria-pressed',String(chosen))}},
 palettes:{
  sector07:{floor:'#404855',light:'#a7bdd0',dark:'#202d3f',wall:'#8796aa',accent:'#ffc067',grass:'#46634e',leaf:'#91b173'},
  docks:{floor:'#6f8d98',light:'#bfd9da',dark:'#274e68',wall:'#94adb9',accent:'#ffe4a0',grass:'#437a73',leaf:'#9ac7a0'},
  ruins:{floor:'#ad8e78',light:'#f6d3aa',dark:'#634958',wall:'#d9a181',accent:'#f8d090',grass:'#787d48',leaf:'#c4b46e'},
  outpost:{floor:'#54795b',light:'#b9d998',dark:'#294b4b',wall:'#8d9b9c',accent:'#c5e0a4',grass:'#3f7450',leaf:'#91bd76'}
 },
 noise(x,y,n=0){const v=Math.sin(x*127.1+y*311.7+n*74.7)*43758.5453;return v-Math.floor(v)},
 rect(g,x,y,w,h,r,fill,stroke){Art.round(g,x,y,w,h,r,fill,stroke)},
 poly(g,points,fill,stroke){g.beginPath();points.forEach(([x,y],i)=>i?g.lineTo(x,y):g.moveTo(x,y));g.closePath();if(fill){g.fillStyle=fill;g.fill()}if(stroke){g.strokeStyle=stroke;g.stroke()}},
 line(g,points,color,width=2){g.beginPath();points.forEach(([x,y],i)=>i?g.lineTo(x,y):g.moveTo(x,y));g.strokeStyle=color;g.lineWidth=width;g.stroke()},
 ellipse(g,x,y,rx,ry,fill,angle=0){g.fillStyle=fill;g.beginPath();g.ellipse(x,y,rx,ry,angle,0,Math.PI*2);g.fill()},
 text(g,s,x,y,size,color,align='center'){g.font=`800 ${size}px system-ui`;g.textAlign=align;g.fillStyle=color;g.fillText(s,x,y)},
 glow(g,x,y,r,color){const glow=g.createRadialGradient(x,y,2,x,y,r);glow.addColorStop(0,color);glow.addColorStop(1,'#00000000');g.fillStyle=glow;g.fillRect(x-r,y-r,r*2,r*2)},
 draw(g,map=activeMap,world=worldGeometry){this.map=map;this.world=world;const id=map.id,p=this.palettes[id];g.save();g.fillStyle=p.floor;g.fillRect(0,0,WW,WH);
  // Decorative paths never change movement or visibility. Clip them to walkable floor.
  g.save();g.beginPath();for(let y=1;y<ROWS-1;y++)for(let x=1;x<COLS-1;x++)if(!this.world.blocked(x,y))g.rect(x*64,y*64,64,64);g.clip();
  this[id](g,p);this.grass(g,p);this.landmarks(g,p);g.restore();
  this.obstacles(g,p);this.border(g,p);g.restore();
 },
 sector07(g,p){
  // Steel plates and service trenches, broad amber loading bays, narrow utility strips.
  for(let y=0;y<20;y++)for(let x=0;x<28;x++){const xx=x*64,yy=y*64,n=this.noise(x,y);this.rect(g,xx+2,yy+2,60,60,2,n>.5?'#475260':'#414b58','#34414e');g.fillStyle='#a5bbbc33';g.fillRect(xx+7,yy+6,49,1);for(const [bx,by]of [[7,7],[56,56]])this.ellipse(g,xx+bx,yy+by,1.5,1.5,'#bfd1cf44')}
  for(const [x,y,w,h]of [[1,9,25,1],[7,1,1,17],[20,2,1,16]]){g.fillStyle='#253440';g.fillRect(x*64,y*64,w*64,h*64);g.strokeStyle='#7f909855';g.lineWidth=2;for(let i=0;i<Math.max(w,h)*64;i+=12){const xx=x*64+(w>h?i:12),yy=y*64+(w>h?12:i);g.beginPath();g.moveTo(xx,yy);g.lineTo(xx+(w>h?0:40),yy+(w>h?40:0));g.stroke()}}
  for(const [x,y,w,h,label]of [[2,2,3,5,'A / 07'],[8,8,3,7,'B / 12'],[14,2,4,6,'C / 03'],[21,8,5,5,'D / 21']]){this.rect(g,x*64+6,y*64+6,w*64-12,h*64-12,3,'#e5b8750a','#d5b47988');this.text(g,label,(x+w/2)*64,(y+h/2)*64,25,'#eec38755');g.fillStyle='#e0b36480';for(let i=0;i<w*64-18;i+=27)this.poly(g,[[x*64+10+i,y*64+7],[x*64+20+i,y*64+7],[x*64+13+i,y*64+19],[x*64+3+i,y*64+19]],'#d6ac62aa')}
  for(const [x,y]of [[352,96],[736,480],[1184,224],[1376,1120]]){if(!(this.world.darkUntil>time))this.glow(g,x,y,180,'#ffba4928');this.rect(g,x-23,y-5,46,10,3,this.world.darkUntil>time?'#52606b':'#ffdf99');this.rect(g,x-28,y-9,56,18,3,null,'#24313f')}
  for(let i=0;i<24;i++){const x=80+this.noise(i,1)*1630,y=80+this.noise(i,2)*1110;this.line(g,[[x,y],[x+21,y+1],[x+31,y-6]],'#bdc8b81c',2)}
 },
 docks(g,p){
  for(let y=1;y<19;y++)for(let x=1;x<27;x++){g.fillStyle=this.noise(x,y)>.6?'#78969e':'#6f8d98';g.fillRect(x*64+1,y*64+1,62,62);if((x+y)%3===0)this.line(g,[[x*64+5,y*64+50],[x*64+30,y*64+43],[x*64+48,y*64+52]],'#b4d0ca28',1)}
  // Timber piers reach between the three seawalled basins.
  for(const [x,y,w,h]of [[9,1,2,4],[17,1,2,4],[1,3,26,1]]){this.rect(g,x*64,y*64,w*64,h*64,2,'#aa9b78');for(let j=0;j<h*64;j+=16){g.fillStyle=j%32?'#8d876e':'#b2a282';g.fillRect(x*64+4,y*64+j+1,w*64-8,13);g.fillStyle='#535f60';g.fillRect(x*64+10,y*64+j+6,3,3);g.fillRect((x+w)*64-14,y*64+j+6,3,3)}}
  for(const x of [9,15,21]){g.setLineDash([24,24]);this.line(g,[[x*64,320],[x*64,1184]],'#f7deb27a',3);g.setLineDash([])}
  for(const [x,y]of [[224,352],[864,800],[1568,1120]]){this.ellipse(g,x,y,60,23,'#32596b22',-.2);this.ellipse(g,x+4,y-3,47,15,'#bddede25',-.2)}
  this.text(g,'PIER 08',850,300,32,'#f6e6b0b0');this.text(g,'CARGO / SOUTH',935,1120,26,'#e5dba78a');
  // Crane rail lanes and mounting plates are markings, not invisible obstacles.
  for(const y of [805,838])this.line(g,[[150,y],[1650,y]],'#2e526280',4);
  for(let x=160;x<1640;x+=100){g.fillStyle='#bbd1cc88';g.fillRect(x,794,4,58)}
 },
 ruins(g,p){
  for(let y=0;y<20;y++)for(let x=0;x<28;x++){g.fillStyle=this.noise(x,y)>.5?'#b59880':'#aa8c78';g.fillRect(x*64+1,y*64+1,62,62)}
  // A wide cruciform road connects four different courtyards.
  g.fillStyle='#9e9b91';g.fillRect(11*64,64,5*64,1152);g.fillRect(64,8*64,1664,4*64);g.fillStyle='#545767';g.fillRect(12*64,64,3*64,1152);g.fillRect(64,9*64,1664,2*64);
  g.setLineDash([28,25]);this.line(g,[[864,75],[864,1205]],'#e5c48e88',3);this.line(g,[[78,640],[1710,640]],'#e5c48e88',3);g.setLineDash([]);
  for(const y of [480,728])for(let x=786;x<948;x+=24){g.fillStyle='#e2d4bdab';g.fillRect(x,y,13,34)}
  for(const x of [716,991])for(let y=592;y<691;y+=22){g.fillStyle='#e2d4bdab';g.fillRect(x,y,31,12)}
  for(const [x,y,w,h,color]of [[5,4,4,4,'#a98074'],[18,4,5,4,'#b5a088'],[5,12,5,4,'#9a8e81'],[18,13,5,3,'#b09081']]){g.fillStyle=color;g.fillRect(x*64,y*64,w*64,h*64);for(let yy=y*64;yy<(y+h)*64;yy+=24)for(let xx=x*64;xx<(x+w)*64;xx+=24){g.strokeStyle='#493c4622';g.lineWidth=1;g.strokeRect(xx,yy,23,23)}}
  for(let i=0;i<85;i++){const x=80+this.noise(i,4)*1640,y=85+this.noise(i,5)*1110;if(i%3===0)this.line(g,[[x,y],[x+11,y+9],[x+6,y+17],[x+21,y+26]],'#58495255',1.5);else this.poly(g,[[x,y],[x+7,y-3],[x+12,y+5],[x+2,y+7]],i%2?'#d6b39766':'#796b7166')}
  this.text(g,'旧 城 大 道',1080,620,22,'#d1c4a870');this.text(g,'09',870,870,50,'#c9c3ad45');
  for(const [x,y]of [[610,340],[1240,990],[1540,400]]){this.glow(g,x,y,160,'#ffba7430');this.line(g,[[x-70,y+12],[x+70,y+12]],'#76535c55',2)}
 },
 outpost(g,p){
  // Overlapping clearings and winding dirt tracks, rather than a visible square floor grid.
  for(let i=0;i<200;i++){const x=this.noise(i,10)*WW,y=this.noise(i,11)*WH,r=25+this.noise(i,12)*85;this.ellipse(g,x,y,r,r*.65,i%3===0?'#81a56729':i%3===1?'#2d63552c':'#b4c37d13',this.noise(i,13))}
  const routes=[[[170,1090],[400,940],[675,1090],[970,930],[1200,800],[1550,1050]],[[160,1050],[285,660],[640,510],[930,650],[1110,330],[1530,220]],[[500,200],[620,415],[900,700],[1150,760],[1550,560]]];
  for(const route of routes){g.beginPath();g.moveTo(...route[0]);for(let i=1;i<route.length-1;i++){const a=route[i],b=route[i+1];g.quadraticCurveTo(a[0],a[1],(a[0]+b[0])/2,(a[1]+b[1])/2)}g.lineTo(...route.at(-1));g.lineCap='round';g.strokeStyle='#354f423d';g.lineWidth=96;g.stroke();g.strokeStyle='#a2a080';g.lineWidth=76;g.stroke();g.strokeStyle='#b7ad8799';g.lineWidth=53;g.stroke();g.lineCap='butt'}
  for(let i=0;i<160;i++){const x=80+this.noise(i,21)*1640,y=80+this.noise(i,22)*1120;this.line(g,[[x-3,y],[x,y-7],[x+3,y]],'#a5c28560',1.5);if(i%5===0)this.ellipse(g,x+7,y,2,2,i%2?'#f2d58fa0':'#aecbdfa0')}
  for(const [x,y]of [[1000,540],[330,950],[1340,180]]){g.strokeStyle='#c5d8a92a';g.lineWidth=2;for(let i=1;i<4;i++){g.beginPath();g.ellipse(x,y,40+i*30,25+i*18,-.2,0,Math.PI*2);g.stroke()}}
 },
 grass(g,p){for(let y=1;y<19;y++)for(let x=1;x<27;x++)if(this.world.grass[y*28+x]){const px=x*64,py=y*64;this.rect(g,px,py,64,64,7,p.grass);for(let i=0;i<20;i++){const xx=px+5+this.noise(x+i,y)*54,yy=py+8+this.noise(x,y+i,2)*48;const h=6+this.noise(x+i,y,4)*9;this.line(g,[[xx-4,yy-h*.7],[xx,yy],[xx+4,yy-h]],i%3===0?p.leaf:p.dark+'80',2);if(this.map.id==='ruins'&&i%5===0)this.ellipse(g,xx+4,yy-h,2,3,'#e6bd81')}
  if(this.map.id==='outpost')for(let i=0;i<2;i++)this.ellipse(g,px+18+i*27,py+25,10,7,'#96b66b33');
 }},
 landmarks(g,p){for(const poi of this.world.pois()){g.save();g.translate(poi.x,poi.y);this.rect(g,-57,-47,114,94,9,null,p.accent+'44');g.strokeStyle=p.accent+'50';g.lineWidth=2;g.beginPath();g.arc(0,0,32,0,Math.PI*2);g.stroke();this.text(g,poi.type==='med'?'+':poi.type==='arms'?'///':poi.type==='tech'?'⌁':'⚙',0,8,26,p.accent+'66');this.text(g,poi.name,0,-67,17,p.light);this.text(g,poi.type.toUpperCase(),0,-49,10,p.accent+'99');g.restore()}},
 obstacles(g,p){
  for(const [i,r]of this.map.walls.entries()){const [tx,ty,tw,th]=r,x=tx*64,y=ty*64,w=tw*64,h=th*64;
   if(this.map.water?.some(a=>a.every((v,j)=>v===r[j]))){this.water(g,x,y,w,h);continue}
   this.rect(g,x+9,y+13,w,h,5,p.dark+'70');
   if(this.map.id==='outpost'){this.rocks(g,x,y,w,h,p);continue}
   this.rect(g,x,y,w,h,3,p.dark);this.rect(g,x+2,y+2,w-4,h-10,3,p.wall);g.fillStyle=p.light+'80';g.fillRect(x+5,y+4,w-10,4);g.fillStyle=p.dark+'60';g.fillRect(x+3,y+h-17,w-6,8);
   if(this.map.id==='ruins'){for(let yy=10;yy<h-15;yy+=17)for(let xx=(Math.floor(yy/17)%2)*16;xx<w-4;xx+=33){g.strokeStyle='#885e5855';g.lineWidth=1;g.strokeRect(x+xx+3,y+yy,Math.min(30,w-xx-6),15)}for(let j=0;j<Math.max(w,h);j+=55){const xx=x+(w>h?j:23),yy=y+(w>h?23:j);this.line(g,[[xx,yy],[xx+8,yy+10],[xx+2,yy+20]],'#6e54617a',2)}}
   else{for(let j=18;j<Math.max(w,h)-8;j+=48){const xx=x+(w>h?j:12),yy=y+(w>h?12:j);g.fillStyle=p.dark+'60';g.fillRect(xx,yy,w>h?6:w-24,w>h?h-28:6)}if(this.map.id==='sector07'){this.rect(g,x+7,y+6,Math.min(26,w-14),7,2,this.world.darkUntil>time?'#52606b':'#edc087');if(!(this.world.darkUntil>time))this.glow(g,x+20,y+7,68,'#ffc97b1a')}}
  }
  for(const [i,[tx,ty,tw,th]]of this.map.crates.entries()){const x=tx*64,y=ty*64,w=tw*64,h=th*64;this.rect(g,x+8,y+12,w,h,5,p.dark+'80');if(this.map.id==='docks')this.container(g,x,y,w,h,i);else if(this.map.id==='ruins')this.car(g,x,y,w,h,i);else if(this.map.id==='outpost')this.hut(g,x,y,w,h,i);else this.cargo(g,x,y,w,h,i)}
 },
 container(g,x,y,w,h,i){const colors=['#cc744c','#4d929e','#d4b15b','#746d9d','#cb7f65','#598da9'],c=colors[i%colors.length];this.rect(g,x,y,w,h,3,'#284556');this.rect(g,x+3,y+3,w-6,h-10,3,c);g.fillStyle='#ffffff33';g.fillRect(x+8,y+7,w-16,4);g.strokeStyle='#263d4944';g.lineWidth=3;for(let k=15;k<(h>w?h:w)-10;k+=15){g.beginPath();if(h>w){g.moveTo(x+9,y+k);g.lineTo(x+w-9,y+k)}else{g.moveTo(x+k,y+10);g.lineTo(x+k,y+h-17)}g.stroke()}this.rect(g,x+w/2-30,y+h/2-18,60,36,3,'#203b4c99');this.text(g,'TZ / 0'+(i+1),x+w/2,y+h/2+5,13,'#f0e8c8');for(const dx of [7,w-12]){g.fillStyle='#ead2a7';g.fillRect(x+dx,y+h-17,5,8)}},
 cargo(g,x,y,w,h,i){this.rect(g,x,y,w,h,3,'#433827');this.rect(g,x+3,y+3,w-6,h-10,3,i%2?'#ad7f51':'#c6985c');for(let yy=12;yy<h-10;yy+=18){g.fillStyle='#604c3b66';g.fillRect(x+5,y+yy,w-10,3)}for(const xx of [9,w-16]){g.fillStyle='#556571';g.fillRect(x+xx,y+4,7,h-13)}this.line(g,[[x+16,y+12],[x+w-17,y+h-19]],'#e6bd7e99',5);this.rect(g,x+w/2-12,y+h/2-13,24,26,1,'#e6dab4');this.text(g,'↑',x+w/2,y+h/2+8,21,'#5d5342')},
 car(g,x,y,w,h,i){g.save();g.translate(x+w/2,y+h/2);if(h>w)g.rotate(Math.PI/2);const l=Math.max(w,h),s=Math.min(w,h);this.rect(g,-l/2,-s/2,l,s,9,'#263541');for(const xx of [-l*.31,l*.3])for(const yy of [-s/2,s/2-8])this.rect(g,xx,yy,22,8,2,'#263039');this.rect(g,-l/2+3,-s/2+5,l-6,s-10,9,['#b7796b','#7d97a1','#c6a45e','#7d8198'][i%4]);this.rect(g,-l*.24,-s*.31,l*.39,s*.62,4,'#b5c2bd');this.rect(g,-l*.26,-s*.29,12,s*.58,2,'#34495a');this.rect(g,l*.15,-s*.29,15,s*.58,2,'#34495a');g.fillStyle='#f4dca4';g.fillRect(l/2-8,-s/2+10,5,10);g.fillRect(l/2-8,s/2-20,5,10);this.line(g,[[-15,-12],[5,4],[15,1]],'#745f62',2);g.restore()},
 hut(g,x,y,w,h,i){this.rect(g,x,y,w,h,4,'#294f50');this.rect(g,x+4,y+4,w-8,h-12,4,i%2?'#829b80':'#7299a1');this.poly(g,[[x+5,y+5],[x+w/2,y+14],[x+w/2,y+h-16],[x+5,y+h-10]],'#b5c4a660');this.line(g,[[x+w/2,y+7],[x+w/2,y+h-13]],'#d3d5b3',4);for(let yy=22;yy<h-16;yy+=18)this.line(g,[[x+8,y+yy],[x+w-8,y+yy]],'#294b4d30',2);this.rect(g,x+w/2-17,y+h-15,34,12,2,'#28474b');if(i===3){this.ellipse(g,x+w*.7,y+h*.36,31,31,'#34555566');this.ellipse(g,x+w*.7,y+h*.31,28,25,'#c9dbcc');this.line(g,[[x+w*.7-20,y+h*.31+9],[x+w*.7+17,y+h*.31-14]],'#679a96',3);this.line(g,[[x+w*.7,y+h*.31],[x+w*.7+8,y+h*.31-30]],'#f0e2b9',3)}},
 rocks(g,x,y,w,h,p){this.rect(g,x,y,w,h,4,'#445c61');for(let yy=0;yy<h;yy+=64)for(let xx=0;xx<w;xx+=64){const a=x+xx,b=y+yy;this.poly(g,[[a+2,b+4],[a+31,b+1],[a+62,b+8],[a+63,b+48],[a+45,b+62],[a+4,b+60]],'#869695');this.poly(g,[[a+2,b+4],[a+31,b+1],[a+46,b+21],[a+18,b+34]],'#b4beb0');this.poly(g,[[a+18,b+34],[a+46,b+21],[a+63,b+48],[a+45,b+62]],'#6f8487');this.line(g,[[a+7,b+48],[a+27,b+41],[a+37,b+46]],'#c3ccb34a',2);this.ellipse(g,a+12,b+13,10,5,'#768f63aa')}},
 water(g,x,y,w,h){this.rect(g,x,y,w,h,1,'#245477');g.save();g.beginPath();g.rect(x,y,w,h);g.clip();for(let j=0;j<h;j+=19)for(let k=0;k<w;k+=58){const xx=x+k+this.noise(k,j)*15,yy=y+j;this.line(g,[[xx,yy+5],[xx+13,yy+7],[xx+31,yy+4]],j%2?'#79bfc148':'#4384a260',2)}this.rect(g,x+16,y+h-13,w-32,4,2,'#d6e8d271');g.restore();g.fillStyle='#d5c09a';g.fillRect(x,y+h-7,w,7);for(let k=10;k<w;k+=52){this.ellipse(g,x+k,y+h-2,10,10,'#233d50');this.ellipse(g,x+k,y+h-2,5,5,'#567586')}},
 tree(g,x,y,s=1){this.ellipse(g,x+9*s,y+13*s,27*s,15*s,'#244d4466');this.rect(g,x-3*s,y-4*s,6*s,22*s,1,'#736c4b');for(let i=0;i<3;i++){const yy=y-i*13*s,r=(28-i*5)*s;this.poly(g,[[x,yy-31*s],[x+r,yy+10*s],[x,yy+5*s],[x-r,yy+10*s]],['#32614f','#438065','#619978'][i]);this.line(g,[[x,yy-26*s],[x-r*.58,yy+1*s]],'#b4cc854f',2*s)}},
 border(g,p){for(let gy=0;gy<20;gy++)for(let gx=0;gx<28;gx++){const x=gx*64,y=gy*64;if(x!==0&&y!==0&&x!==WW-64&&y!==WH-64)continue;this.rect(g,x,y,64,64,1,p.dark);if(this.map.id==='outpost'){this.rocks(g,x,y,64,64,p);if((x+y)%128===0)this.tree(g,x+32,y+35,.9)}else if(this.map.id==='docks'){this.rect(g,x+4,y+5,56,53,4,'#698798');this.line(g,[[x+7,y+11],[x+52,y+11]],'#c4d2c880',3)}else if(this.map.id==='ruins'){this.rect(g,x+3,y+4,58,54,3,'#b38373');for(let i=18;i<55;i+=16)this.line(g,[[x+5,y+i],[x+59,y+i]],'#704f5a66',2)}else{this.rect(g,x+3,y+3,58,54,2,'#586878');g.fillStyle='#b7c4bf66';g.fillRect(x+8,y+6,48,3);g.fillStyle='#dab16c';g.fillRect(x+27,y+20,10,18)}}}
};

