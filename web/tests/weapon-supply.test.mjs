import test from 'node:test';
import assert from 'node:assert/strict';
import {Room,rules} from '../server/simulation.mjs';
const battle=()=>{const r=new Room('123456','sector07','ffa',{supplySeconds:10});const a=r.add('A');r.add('B');r.begin();r.status='playing';return {r,a}};

test('the supply rotation includes every ordinary rifle, SMG and support weapon',()=>{
 const found=new Set();for(let phase=0;phase<3;phase++)for(const random of [0,.49,.999999])found.add(rules.rollSupplyWeapon(phase,()=>random));
 assert.deepEqual([...found].sort(),[...rules.FIELD_WEAPONS].sort());
});

test('timed supplies rotate rifle, SMG, support and special; normal drops have full reserve',()=>{
 const {r}=battle();
 for(let i=0;i<8;i++){
  r.airdrops=[];assert.equal(r.spawnDrop(),true);const drop=r.airdrops[0],w=rules.W[drop.weapon];
  if(i%4===0)assert.equal(w.kind,'rifle');
  if(i%4===1)assert.equal(w.kind,'smg');
  assert.equal(w.tier==='airdrop',i%4===3);
  r.time=drop.landAt;r.updateDrops();const item=r.loot.find(x=>x.id===drop.id);
  assert.equal(item.ammo,w.mag);assert.equal(item.reserve,w.reserve);
 }
});

test('ordinary weapon points return after pickup with fresh ammunition and cannot be farmed early',()=>{
 const {r,a}=battle();const item=r.loot.find(x=>x.type==='weapon');
 assert.equal(r.loot.filter(x=>x.type==='weapon').some(x=>rules.W[x.weapon].kind==='rifle'),true);
 assert.equal(r.loot.filter(x=>x.type==='weapon').some(x=>rules.W[x.weapon].kind==='smg'),true);
 const id=item.id;r.collect(a,item);assert.equal(item.respawnAt,r.time+10);r.tick(.1);
 assert.ok(r.loot.some(x=>x.id===id&&x.taken));assert.ok(!r.snapshot(a).loot.some(x=>x.id===id));
 r.time=item.respawnAt;r.tick(.01);const fresh=r.loot.find(x=>x.id===id);
 assert.equal(fresh.taken,false);assert.equal(fresh.ammo,rules.W[fresh.weapon].mag);assert.equal(fresh.reserve,rules.W[fresh.weapon].reserve);
 assert.ok(!rules.W[fresh.weapon].limited);
 r.begin();assert.equal(r.supplySerial,0);
});
