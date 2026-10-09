import test from 'node:test';
import assert from 'node:assert/strict';
import classes from '../dist/classes.js';
import {Room,rules} from '../server/simulation.mjs';
import harnessModule from './harness.cjs';
const {harness}=harnessModule;
function battle(mode='ffa'){const r=new Room('654321','sector07',mode),a=r.add('A'),b=r.add('B'),c=mode==='teams'?r.add('C'):null,d=mode==='teams'?r.add('D'):null;r.begin();r.status='playing';r.time=5;for(const [i,p]of [...r.players.values()].entries())Object.assign(p,{x:224+i*35,y:1056,hp:100,shieldUntil:0,input:{x:0,y:0,angle:0,fire:false},lastInput:5});return {r,a,b,c,d}}
function choose(r,p,id){classes.init(p,id,r.time)}
test('eight valid professions: lobby choice, readiness reset, battle lock and respawn cooldown',()=>{
 assert.equal(Object.keys(classes.ROLES).length,8);const r=new Room('123456','sector07'),a=r.add('A','medic'),b=r.add('B');assert.equal(a.classId,'medic');a.ready=true;r.action(a,{type:'class',classId:'__proto__'});assert.equal(a.classId,'medic');r.action(a,{type:'class',classId:'scout'});assert.equal(a.ready,false);r.begin();r.status='playing';r.action(a,{type:'class',classId:'guardian'});assert.equal(a.classId,'scout');r.action(a,{type:'skill'});const ready=a.skillReadyAt;assert.ok(ready>r.time);r.action(a,{type:'skill'});assert.equal(a.skillReadyAt,ready);r.spawn(a);assert.equal(a.skillReadyAt,ready);assert.equal(a.skillUntil,0);r.begin();assert.equal(a.skillReadyAt,r.time);assert.equal(a.classId,'scout');
});
test('assault increases real movement without walking through walls; expired skill restores speed',()=>{
 const {r,a}=battle();choose(r,a,'assault');r.action(a,{type:'skill'});assert.equal(classes.speed(a,r.time),1.3);r.action(a,{type:'input',seq:1,x:0,y:1,angle:0,fire:false});const y=a.y;r.tick(.1);assert.ok(Math.abs(a.y-y-rules.CONFIG.speed*1.3*.1)<.001);assert.equal(classes.speed(a,r.time+6),1);for(let i=0;i<20;i++){r.action(a,{type:'input',seq:i+2,x:-1,y:0,angle:0,fire:false});r.tick(.1)}assert.ok(r.world.stand(a.x,a.y));
});
test('medic heals self and nearby living teammates, never enemies/downed; ineffective cast is free',()=>{
 const {r,a,b,c}=battle('teams');choose(r,a,'medic');a.hp=40;b.hp=50;c.hp=40;r.action(a,{type:'skill'});assert.equal(a.hp,70);assert.equal(b.hp,80);assert.equal(c.hp,40);r.action(a,{type:'skill'});assert.equal(a.hp,70);a.skillReadyAt=r.time;a.hp=100;b.hp=0;b.downed=true;r.action(a,{type:'skill'});assert.equal(b.hp,0);assert.equal(b.downed,true);assert.equal(a.skillReadyAt,r.time);a.hp=0;r.action(a,{type:'skill'});assert.equal(a.hp,0);
});
test('healing and armor respect LOS; FFA cannot support another player',()=>{
 const {r,a,b}=battle('teams');choose(r,a,'medic');a.hp=b.hp=40;const old=r.world.los;r.world.los=(x,y)=>x===y;r.action(a,{type:'skill'});assert.equal(a.hp,70);assert.equal(b.hp,40);r.world.los=old;const f=battle();choose(f.r,f.a,'medic');f.b.hp=20;f.r.action(f.a,{type:'skill'});assert.equal(f.b.hp,20);
});
test('engineer armor absorbs 35, does not stack, expires, and clears on respawn',()=>{
 const {r,a,b,c}=battle('teams');choose(r,a,'engineer');r.action(a,{type:'skill'});assert.equal(a.armor,35);assert.equal(b.armor,35);assert.equal(c.armor,0);r.hurt(b,{owner:c.id,weapon:'USP'},50);assert.equal(b.hp,85);assert.equal(b.armor,0);a.skillReadyAt=r.time;r.action(a,{type:'skill'});assert.equal(a.armor,35);r.time+=11;r.hurt(a,{owner:c.id,weapon:'USP'},10);assert.equal(a.hp,90);r.spawn(a);assert.equal(a.armor,0);
});
test('guardian damage reduction has movement cost and works with armor',()=>{
 const {r,a,b}=battle();choose(r,a,'guardian');r.action(a,{type:'skill'});assert.equal(classes.speed(a,r.time),.8);r.hurt(a,{owner:b.id,weapon:'USP'},100);assert.ok(Math.abs(a.hp-45)<1e-9);a.armor=35;a.armorUntil=r.time+10;r.hurt(a,{owner:b.id,weapon:'USP'},40);assert.ok(Math.abs(a.hp-45)<1e-9);assert.equal(a.armor,13);assert.equal(classes.speed(a,r.time+6),1);
});
test('marksman tightens server bullet spread without targeting and uses normal ammunition',()=>{
 const {r,a,b}=battle();choose(r,a,'marksman');r.action(a,{type:'skill'});assert.equal(classes.spread(a,r.time),.35);assert.equal(classes.speed(a,r.time),.65);const random=Math.random;try{Math.random=()=>1;a.angle=1.1;r.shoot(a);const bullet=r.bullets[0];assert.ok(Math.abs(Math.atan2(bullet.vy,bullet.vx)-(1.1+rules.W.USP.spread*.35))<.0001);assert.equal(a.pistol.ammo,rules.W.USP.mag-1);assert.equal(a.angle,1.1)}finally{Math.random=random}
});
test('infiltrator emits no footsteps; firing ends stealth and does not hide from vision',()=>{
 const {r,a,b}=battle();choose(r,a,'infiltrator');r.action(a,{type:'skill'});assert.equal(classes.silent(a,r.time),true);a.moving=1;r.ops.time=r.time;r.ops.tick(.1);assert.equal(r.ops.noises.some(n=>n.source===a.id&&n.kind==='step'),false);assert.equal(r.world.visible(b,a,r.time),true);r.shoot(a);assert.equal(classes.silent(a,r.time),false);assert.ok(r.ops.noises.some(n=>n.kind==='shot'));
});
test('scout signals reveal quantized bearings only; do not add unseen enemies to peers',()=>{
 const {r,a,b,c}=battle('teams');choose(r,a,'scout');Object.assign(a,{x:224,y:1056,angle:0});Object.assign(c,{x:224,y:800});Object.assign(b,{x:250,y:1056});r.action(a,{type:'skill'});r.ops.time=r.time;const normal=r.world.revealed;r.world.revealed=()=>false;const s=r.snapshot(a),scan=s.sounds.filter(x=>String(x.id).startsWith('scan'));assert.ok(scan.length>0);assert.equal(s.peers.some(x=>x.id===c.id),false);for(const signal of scan){assert.deepEqual(Object.keys(signal).sort(),['direction','id','kind']);assert.ok(Math.abs(signal.direction/(Math.PI/4)-Math.round(signal.direction/(Math.PI/4)))<1e-8)}r.world.revealed=normal;assert.equal(classes.signals(a,[a,b],(x,y)=>r.allied(x,y),r.time).length,0);assert.equal(classes.signals(a,[a,c],()=>false,r.time+7).length,0);
});
test('supplier adds bounded reserve only, no loaded ammunition or limited-weapon refill',()=>{
 const {r,a,b,c}=battle('teams');choose(r,a,'supplier');a.pistol.reserve=0;b.pistol.reserve=rules.W.USP.reserve*2-2;c.pistol.reserve=0;const ammo=a.pistol.ammo;r.action(a,{type:'skill'});assert.equal(a.pistol.reserve,rules.W.USP.mag);assert.equal(a.pistol.ammo,ammo);assert.equal(b.pistol.reserve,rules.W.USP.reserve*2);assert.equal(c.pistol.reserve,0);for(const type of ['AWP','RAIL','NOVA','CYCLONE']){a.primary={type,ammo:0,reserve:0};a.slot='primary';a.skillReadyAt=r.time;r.action(a,{type:'skill'});assert.equal(a.primary.reserve,0);assert.equal(a.primary.ammo,0);assert.equal(a.skillReadyAt,r.time)}
});
test('solo selection persists, all eight abilities execute through actual UI adapter and render',()=>{
 for(const id of Object.keys(classes.ROLES)){const h=harness();h.run(`ClassUI.choose('${id}');reset();player.hp=40;player.reserve=0;ClassUI.cast();hud();render()`);assert.equal(h.run('player.classId'),id);assert.equal(h.values['tactical-class'],id);assert.ok(h.run('player.skillReadyAt>time'));assert.match(h.els.classStatus.textContent,new RegExp(classes.ROLES[id].name));assert.equal(h.run('ClassUI.cards.length'),8);h.run('reset()');assert.equal(h.run('player.classId'),id)}const restored=harness({'tactical-class':'medic'});restored.run('reset()');assert.equal(restored.run('player.classId'),'medic');
});
test('solo damage and shooting use profession effects; skill is inert while paused/dead',()=>{
 const h=harness();h.run("ClassUI.choose('guardian');reset();ClassUI.cast();damage(player,{damage:40,weapon:'USP'},false,player.x,player.y)");assert.equal(h.run('player.hp'),78);h.run("ClassUI.choose('marksman');reset();player.recoil=0;player.moving=0;ClassUI.cast()");assert.equal(h.run('spread(player,true)'),rules.W.USP.spread*.35);h.run("ClassUI.choose('medic');reset();player.hp=40;paused=true;ClassUI.cast()");assert.equal(h.run('player.hp'),40);h.run('paused=false;player.hp=0;ClassUI.cast()');assert.equal(h.run('player.hp'),0);
});
