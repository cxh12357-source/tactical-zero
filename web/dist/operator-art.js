'use strict';
// Art-only layered rigs. Coordinates face +X. Physics and weapon geometry stay in World.
const OperatorArt={
 looks:{
  assault:{cloth:'#c97342',vest:'#4c4443',helmet:'#cb7745',hair:'#793f35',skin:'#e8b38e',width:1,head:'hair',pack:'rig',mantle:'scarf'},
  scout:{cloth:'#408c9c',vest:'#294959',helmet:'#90b9bc',hair:'#2e343e',skin:'#ba896d',width:.92,head:'optic',pack:'antenna',mantle:null},
  medic:{cloth:'#e1e4ce',vest:'#609d85',helmet:'#f0edda',hair:'#313c45',skin:'#b98970',width:.97,head:'medical',pack:'medical',mantle:'coat'},
  engineer:{cloth:'#c59b43',vest:'#625442',helmet:'#efc560',hair:'#624532',skin:'#d3a17b',width:1.08,head:'hardhat',pack:'tools',mantle:null},
  guardian:{cloth:'#687baf',vest:'#344860',helmet:'#8497c1',hair:'#313a4b',skin:'#c8ae8b',width:1.2,head:'sealed',pack:'shield',mantle:null},
  marksman:{cloth:'#8c648c',vest:'#414356',helmet:'#9b749e',hair:'#d4d5ce',skin:'#efd0b0',width:.92,head:'beret',pack:'scope',mantle:'cape'},
  infiltrator:{cloth:'#657c50',vest:'#354941',helmet:'#789463',hair:'#2d3f39',skin:'#ba9279',width:.94,head:'hood',pack:'slim',mantle:'leaves'},
  supplier:{cloth:'#b29b71',vest:'#706451',helmet:'#ddc79e',hair:'#453b32',skin:'#c3936c',width:1.08,head:'cap',pack:'cargo',mantle:null}
 },
 look(a){return Object.hasOwn(this.looks,a.classId)?this.looks[a.classId]:null},
 poly(g,points,fill,stroke='#172d38'){g.beginPath();points.forEach(([x,y],i)=>i?g.lineTo(x,y):g.moveTo(x,y));g.closePath();g.fillStyle=fill;g.fill();if(stroke){g.strokeStyle=stroke;g.lineWidth=1.4;g.stroke()}},
 box(g,x,y,w,h,color,stroke='#172d38'){Art.round(g,x,y,w,h,2,color,stroke)},
 mantle(g,l,walk){if(!l?.mantle)return;g.save();g.translate(0,walk*.6);const c=l.cloth;
  if(l.mantle==='cape')this.poly(g,[[-1,-13],[-27,-24],[-34,-9],[-21,1],[-11,5]],'#78597f');
  if(l.mantle==='scarf'){this.poly(g,[[5,-5],[-20,-12],[-34,-5],[-26,-2],[-5,6]],'#e6bb74');g.strokeStyle='#845e42';g.beginPath();g.moveTo(-20,-8);g.lineTo(-28,-5);g.stroke()}
  if(l.mantle==='leaves'){this.poly(g,[[8,-15],[-10,-21],[-18,-15],[-31,-18],[-25,-6],[-34,0],[-25,8],[-29,18],[-12,15],[-7,21],[8,14]],c);for(const y of [-11,0,11])this.poly(g,[[-12,y],[-23,y-3],[-18,y+4]],'#8eaa70',null)}
  if(l.mantle==='coat'){this.poly(g,[[-7,-15],[-24,-18],[-26,-3],[-14,-1],[-25,4],[-22,18],[-7,15]],'#e7e6d3');g.fillStyle='#75a98c';g.fillRect(-24,-15,3,9);g.fillRect(-23,6,3,9)}g.restore()
 },
 pack(g,l){const c=l.vest,b=(x,y,w,h,col)=>this.box(g,x,y,w,h,col);
  if(l.pack==='rig'){b(-25,-12,13,25,c);for(const y of [-9,1])b(-26,y,7,8,'#c38658');g.fillStyle='#d3c19b';g.fillRect(-21,-10,2,20)}
  if(l.pack==='antenna'){b(-28,-12,15,25,'#365e72');b(-31,-8,6,17,'#67b2bf');for(const y of [-13,13]){g.strokeStyle='#142f40';g.lineWidth=2.5;g.beginPath();g.moveTo(-23,y);g.lineTo(-34,y*1.8);g.stroke();g.fillStyle='#9aebed';g.fillRect(-35,y*1.8-2,3,4)}b(-25,-9,8,5,'#82d0cf')}
  if(l.pack==='medical'){b(-31,-15,18,30,'#e1e4d4');b(-32,-14,6,28,'#67a991');for(const y of [-12,6])b(-27,y,12,7,'#81bda0');g.strokeStyle='#fcffe9';g.lineWidth=2;g.beginPath();g.moveTo(-28,0);g.lineTo(-24,0);g.lineTo(-22,-4);g.lineTo(-20,4);g.lineTo(-17,0);g.stroke()}
  if(l.pack==='tools'){b(-29,-14,17,28,'#615444');b(-33,-9,7,18,'#bb8c3e');for(const y of [-9,-2,5])b(-24,y,8,5,'#d0b876');g.strokeStyle='#9faeb1';g.lineWidth=4;g.beginPath();g.moveTo(-22,13);g.lineTo(-29,-23);g.stroke();this.poly(g,[[-33,-27],[-34,-21],[-30,-17],[-25,-20],[-24,-27],[-28,-22]],'#a9b9b7');b(-23,12,16,8,'#8b7660')}
  if(l.pack==='shield'){this.poly(g,[[-16,-21],[-33,-23],[-37,-16],[-37,17],[-30,24],[-16,21]],'#334a66');b(-32,-16,10,33,'#6c82aa');g.fillStyle='#a9c5da';g.fillRect(-34,-10,3,20);g.fillStyle='#d6d4a6';g.fillRect(-30,-2,6,4)}
  if(l.pack==='scope'){b(-26,-9,12,19,c);b(-26,-24,8,45,'#363c51');g.fillStyle='#ab93ba';g.fillRect(-25,-16,6,3);g.fillRect(-25,11,6,3);b(-20,10,9,9,'#a18899')}
  if(l.pack==='slim'){b(-24,-10,10,20,'#314b42');g.strokeStyle='#afba87';g.lineWidth=2;g.beginPath();g.moveTo(-21,-7);g.lineTo(-17,7);g.stroke()}
  if(l.pack==='cargo'){b(-37,-19,24,38,'#8d795c');b(-39,-16,8,32,'#c6b88f');for(const y of [-16,5])b(-29,y,12,12,'#536461');b(-29,-26,15,8,'#c9b28b');g.fillStyle='#463f36';g.fillRect(-34,-18,3,36);g.fillRect(-32,-2,19,4);b(-28,-3,5,6,'#e3c891');b(-31,19,17,7,'#af9673')}
 },
 shoulders(g,l){if(l.pack==='shield'){for(const y of [-22,12]){this.box(g,-10,y,22,11,'#788eb7');this.box(g,-6,y+2,14,5,'#a6b7d0')}}else if(l.pack==='rig'){this.poly(g,[[-5,-21],[6,-23],[13,-15],[5,-10],[-7,-13]],'#e89455');g.fillStyle='#e5d2ad';g.fillRect(0,-18,6,2)}else if(l.pack==='tools'){this.box(g,-5,-20,14,7,'#ddbd62');this.box(g,-5,13,14,7,'#ddbd62')}else if(l.pack==='medical'){this.box(g,0,-19,7,6,'#f0edd9');g.fillStyle='#5c9d83';g.fillRect(1,-17,5,2)}},
 head(g,l){const b=(x,y,w,h,c)=>this.box(g,x,y,w,h,c);g.fillStyle='#192e37';g.beginPath();g.ellipse(8,0,12,10,0,0,Math.PI*2);g.fill();g.fillStyle=l.skin;g.beginPath();g.ellipse(11,0,8,7,0,0,Math.PI*2);g.fill();
  if(l.head==='hair'){this.poly(g,[[11,-8],[2,-10],[-3,-5],[-1,5],[6,9],[10,5],[6,2],[10,-2]],l.hair);b(4,-11,5,4,'#313f49');g.strokeStyle='#374957';g.lineWidth=2;g.beginPath();g.moveTo(7,-10);g.lineTo(14,-9);g.lineTo(19,-3);g.stroke()}
  if(l.head==='optic'){this.poly(g,[[14,-8],[2,-10],[-3,-5],[-2,6],[7,9],[12,6],[8,0]],l.helmet);b(13,-8,6,7,'#335866');g.fillStyle='#8df0ec';g.beginPath();g.arc(17,-4,3,0,Math.PI*2);g.fill();b(5,7,7,4,l.vest)}
  if(l.head==='medical'){g.fillStyle=l.helmet;g.beginPath();g.ellipse(5,0,10,11,0,0,Math.PI*2);g.fill();g.fillStyle='#6daa91';g.fillRect(-2,-2,17,4);b(13,-8,6,15,'#31514e');g.fillStyle='#a7e5cf';g.fillRect(16,-6,2,12)}
  if(l.head==='hardhat'){g.fillStyle=l.helmet;g.beginPath();g.ellipse(5,0,11,12,0,0,Math.PI*2);g.fill();b(10,-13,6,26,'#dcad42');g.fillStyle='#ffe5a0';g.fillRect(-4,-2,17,4);for(const y of [-8,3])b(6,y,6,5,'#52676d')}
  if(l.head==='sealed'){this.poly(g,[[-5,-8],[1,-12],[14,-10],[20,-5],[20,6],[13,11],[0,10],[-5,6]],l.helmet);b(12,-9,7,18,'#283e58');g.fillStyle='#acf0f6';g.fillRect(16,-7,2,14);b(-3,-5,6,10,'#566789')}
  if(l.head==='beret'){this.poly(g,[[10,-8],[2,-11],[-5,-7],[-3,5],[6,9],[10,5],[5,1]],l.hair);g.fillStyle=l.helmet;g.beginPath();g.ellipse(2,-4,10,8,-.25,0,Math.PI*2);g.fill();g.fillStyle='#f0d4a2';g.fillRect(4,-8,3,3)}
  if(l.head==='hood'){this.poly(g,[[18,-6],[8,-13],[-6,-11],[-12,0],[-6,11],[8,13],[18,6],[9,6],[6,0],[9,-6]],l.helmet);g.strokeStyle='#a6ba83';g.lineWidth=1;g.beginPath();g.moveTo(-9,0);g.lineTo(4,-8);g.stroke();b(13,-6,7,12,'#304941');g.fillStyle='#d0dfb5';g.fillRect(17,-4,2,3);g.fillRect(17,2,2,3)}
  if(l.head==='cap'){g.fillStyle=l.helmet;g.beginPath();g.ellipse(4,0,10,10,0,0,Math.PI*2);g.fill();this.poly(g,[[11,-9],[20,-7],[23,0],[20,7],[11,9]],'#b09b72');g.fillStyle='#6e644d';g.fillRect(-3,-1,15,2)}
 },
 preview(canvas,id){const g=canvas.getContext('2d');canvas.width=280;canvas.height=104;g.clearRect(0,0,280,104);const a={classId:id,x:75,y:53,angle:-.4,weapon:'USP',hp:100,moving:0,flash:0,reload:0};Art.actor(a,true,false,g);Art.actor({...a,x:192,angle:2.6},true,false,g)}
};
