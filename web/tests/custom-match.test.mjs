import test from 'node:test';
import assert from 'node:assert/strict';
import { Room, rules } from '../server/simulation.mjs';
import { matchSettings } from '../server/match-settings.mjs';
import classes from '../dist/classes.js';
import { createGameServer } from '../server/index.mjs';
import WebSocket from 'ws';
import harnessModule from './harness.cjs';

function battle(settings={}) {
  const r=new Room('123456','sector07','ffa',settings),a=r.add('Alpha'),b=r.add('Bravo');
  r.begin();r.status='playing';r.time=5;
  Object.assign(a,{x:224,y:1056,angle:0,shieldUntil:0});
  Object.assign(b,{x:280,y:1056,angle:Math.PI,shieldUntil:0});
  return {r,a,b};
}

test('room rules are bounded, owner-only, frozen during battle and reset ready state',()=>{
  const s=matchSettings({maxHp:Infinity,scoreLimit:-99,npcCount:999,matchSeconds:NaN,startingWeapon:'AWP'});
  assert.equal(s.maxHp,100);assert.equal(s.scoreLimit,3);assert.equal(s.npcCount,6);assert.equal(s.matchSeconds,300);assert.equal(s.startingWeapon,'USP');
  const r=new Room('123','docks'),a=r.add('A'),b=r.add('B');a.ready=b.ready=true;
  assert.throws(()=>r.action(b,{type:'settings',settings:{maxHp:200}}),/房主/);
  r.action(a,{type:'settings',settings:{maxHp:200,scoreLimit:30}});
  assert.equal(a.ready,false);assert.equal(b.ready,false);assert.equal(r.settings.maxHp,200);
  r.begin();assert.throws(()=>r.action(a,{type:'settings',settings:{maxHp:50}}),/对局中/);
  assert.equal(a.hp,200);assert.equal(r.snapshot(b).settings.scoreLimit,30);
});

test('custom health, healing, starting weapon and respawn delay are authoritative',()=>{
  const {r,a,b}=battle({maxHp:200,startingWeapon:'M4A1',respawnSeconds:1});
  assert.equal(a.hp,200);assert.equal(a.slot,'primary');assert.equal(a.primary.type,'M4A1');
  a.hp=160;classes.init(a,'medic',r.time);assert.equal(classes.cast(a,{actors:()=>[a],allied:()=>false,world:r.world,noise(){}},r.time).ok,true);assert.equal(a.hp,190);
  r.collect(a,{type:'container',kind:'med',taken:false});assert.equal(a.hp,200);
  r.hurt(a,{owner:b.id,weapon:'AWP'},250);assert.equal(a.respawnAt,6);
  r.tick(1.01);assert.equal(a.hp,200);assert.equal(a.primary.type,'M4A1');
});

test('kill-only goals exclude objectives and NPCs; score goals and custom timeout still settle',()=>{
  const {r,a,b}=battle({victory:'kills',scoreLimit:3,matchSeconds:60});
  a.objectivePoints=50;r.tick(.01);assert.equal(r.status,'playing');
  for(let i=0;i<3;i++){b.hp=100;b.shieldUntil=0;r.hurt(b,{owner:a.id,weapon:'USP'},1000)}
  r.tick(.01);assert.equal(r.status,'finished');assert.deepEqual(r.winners,['Alpha']);
  const timed=battle({victory:'kills',scoreLimit:100,matchSeconds:60});timed.a.kills=1;timed.r.elapsed=59.99;timed.r.tick(.02);assert.equal(timed.r.status,'finished');
  const scored=battle({victory:'score',scoreLimit:3});scored.a.objectivePoints=3;scored.r.tick(.01);assert.equal(scored.r.status,'finished');
});

test('kills, caches and emergency pistol ammo replenish normal weapons, never limited weapons',()=>{
  const {r,a,b}=battle({startingWeapon:'M4A1',supplySeconds:10});a.pistol.reserve=a.primary.reserve=0;
  r.hurt(b,{owner:a.id,weapon:'M4A1'},1000);
  assert.equal(a.primary.reserve,rules.W.M4A1.mag*2);assert.equal(a.pistol.reserve,24);assert.match(a.combatEvent.text,/Bravo/);
  const cache=r.loot.find(l=>l.type==='container');r.collect(a,cache);assert.equal(cache.respawnAt,r.time+10);
  a.primary={type:'AWP',ammo:1,reserve:0};r.encounters.refill(a,3);assert.equal(a.primary.reserve,0);
  a.pistol.reserve=0;a.emergencyAt=r.time;r.encounters.tick(.1);assert.equal(a.pistol.reserve,12);
  r.time=cache.respawnAt;r.tick(.01);assert.equal(cache.taken,false);
});

test('NPC population stays bounded, bullets hit them, drops are single-use and NPC kills do not score',()=>{
  const {r,a,b}=battle({npcCount:2,scoreLimit:100});r.elapsed=8;r.encounters.tick(.1);assert.equal(r.encounters.npcs.size,2);
  const n=[...r.encounters.npcs.values()][0];Object.assign(n,{x:280,y:1056,shieldUntil:0,hp:1});Object.assign(b,{x:1000,y:900});
  a.angle=0;r.shoot(a);r.tick(.08);
  assert.equal(r.encounters.npcs.has(n.id),false);assert.equal(a.npcKills,1);assert.equal(a.kills,0);
  const drop=r.loot.find(l=>l.special);assert.ok(drop);drop.special='ammo';drop.name='弹药箱';a.pistol.reserve=b.pistol.reserve=0;
  r.collect(a,drop);r.collect(b,drop);assert.equal(a.pistol.reserve,36);assert.equal(b.pistol.reserve,0);
  r.tick(.01);assert.equal(drop.taken,true);
  r.elapsed=38;r.encounters.tick(.1);assert.equal(r.encounters.npcs.size,2);
  r.time=drop.expiresAt+1;r.encounters.tick(.1);assert.ok(!r.loot.includes(drop));
  assert.equal(r.snapshot(a).roster.length,2);
});

test('NPC drops heal to custom max HP, grant expiring armor, and reduce cooldown without stacking infinitely',()=>{
  const {r,a}=battle({maxHp:150});a.hp=120;r.encounters.collect(a,{special:'med',name:'急救'});assert.equal(a.hp,150);
  r.encounters.collect(a,{special:'armor',name:'护甲'});assert.equal(a.armor,50);assert.equal(a.armorUntil,r.time+15);
  r.encounters.collect(a,{special:'armor',name:'护甲'});assert.equal(a.armor,50);
  a.skillReadyAt=r.time+20;r.encounters.collect(a,{special:'cooldown',name:'芯片'});assert.equal(a.skillReadyAt,r.time+8);
  r.encounters.collect(a,{special:'cooldown',name:'芯片'});assert.equal(a.skillReadyAt,r.time);
});

test('NPCs can kill players with attribution; hidden NPCs and attacker feedback are not broadcast to opponents',()=>{
  const {r,a,b}=battle({npcCount:1});r.encounters.spawn();const n=[...r.encounters.npcs.values()][0];
  r.hurt(a,{owner:n.id,weapon:'USP',damage:120},120);assert.equal(a.death.killerName,'中立巡逻兵');assert.equal(b.kills,0);
  const snap=r.snapshot(b);assert.ok(snap.peers.every(p=>!p.npc||r.world.revealed(b,p,r.time)));assert.equal(snap.self.combatEvent,null);
});

test('real server events drive hit confirmation, kill banner, supply notice and death countdown once',()=>{
  const {r,a,b}=battle({maxHp:150});const h=harnessModule.harness();h.run('reset();const playedSounds=[];sound=(name)=>playedSounds.push(name)');
  h.box.before=r.snapshot(a);r.hurt(b,{owner:a.id,weapon:'USP'},20);h.box.after=r.snapshot(a);
  h.run('RoomUI.sync(after,before)');assert.equal(h.run('hitTime'),.2);assert.equal(h.run('playedSounds.length'),1);
  h.run('RoomUI.sync(after,after)');assert.equal(h.run('playedSounds.length'),1);
  h.box.before=h.box.after;r.hurt(b,{owner:a.id,weapon:'USP'},200);h.box.after=r.snapshot(a);
  h.run('RoomUI.sync(after,before)');assert.match(h.els.combatNotice.textContent,/击杀确认.*Bravo/);assert.equal(h.els.combatNotice.hidden,false);assert.match(h.els.resupplyNotice.textContent,/补给/);
  h.box.dead=r.snapshot(b);h.run('DeathView.sync(dead);render()');assert.equal(h.els.deathHUD.hidden,false);assert.match(h.els.deathTitle.textContent,/Alpha/);assert.match(h.els.deathTimer.textContent,/自动复活倒计时/);
  r.spawn(b);h.box.alive=r.snapshot(b);h.run('DeathView.sync(alive);RoomUI.sync(alive,dead)');assert.equal(h.els.deathHUD.hidden,true);assert.match(h.els.combatNotice.textContent,/已复活/);
});

test('two websocket clients receive identical custom rules and the selected starting loadout',{timeout:8000},async()=>{
  const app=createGameServer({port:0,host:'127.0.0.1'}),address=await app.ready;
  const sockets=[];const connect=()=>new Promise(resolve=>{const ws=new WebSocket(`ws://127.0.0.1:${address.port}/ws`);ws.messages=[];ws.on('message',raw=>ws.messages.push(JSON.parse(raw)));ws.on('open',()=>resolve(ws));sockets.push(ws)});
  const wait=async(fn)=>{for(let i=0;i<150;i++){const result=fn();if(result)return result;await new Promise(r=>setTimeout(r,20))}throw Error('WebSocket state timeout')};
  try{
    const a=await connect(),b=await connect();a.send(JSON.stringify({type:'create',name:'A',settings:{maxHp:150,scoreLimit:20,npcCount:2,startingWeapon:'M4A1'}}));
    const welcome=await wait(()=>a.messages.find(x=>x.type==='welcome'));b.send(JSON.stringify({type:'join',name:'B',code:welcome.code}));
    const joined=await wait(()=>b.messages.find(x=>x.type==='state'));assert.equal(joined.settings.maxHp,150);assert.equal(joined.settings.npcCount,2);
    a.send(JSON.stringify({type:'ready',ready:true}));b.send(JSON.stringify({type:'ready',ready:true}));
    await wait(()=>a.messages.find(x=>x.type==='state'&&x.roster.length===2&&x.roster.every(p=>p.ready)));
    a.send(JSON.stringify({type:'start'}));
    const playing=await wait(()=>b.messages.find(x=>x.status==='playing'));
    assert.equal(playing.self.hp,150);assert.equal(playing.self.weapon,'M4A1');assert.equal(playing.settings.scoreLimit,20);
  }finally{for(const ws of sockets)ws.terminate();await app.close()}
});
