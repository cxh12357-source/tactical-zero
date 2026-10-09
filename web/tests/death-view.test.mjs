import test from 'node:test';
import assert from 'node:assert/strict';
import {Room} from '../server/simulation.mjs';
import harnessModule from './harness.cjs';
const {harness}=harnessModule;
function battle(mode='ffa'){const r=new Room('123456','sector07',mode),a=r.add('受害者'),b=r.add('击杀者');if(mode==='teams'){r.add('敌人甲');r.add('敌人乙')}r.begin();r.status='playing';r.time=5;for(const [i,p]of [...r.players.values()].entries())Object.assign(p,{x:224+i*50,y:1056,shieldUntil:0});return {r,a,b}}
test('a fatal server hit names the killer and weapon and captures an immutable death viewpoint',()=>{
 const {r,a,b}=battle();r.hurt(a,{owner:b.id,weapon:'AWP'},100);const s=r.snapshot(a);assert.equal(s.self.death?.killerName,'击杀者');assert.equal(s.self.death.weapon,'AWP');assert.equal(s.self.death.attacker.x,b.x);assert.equal(s.self.death.victim.x,a.x);const frozen=JSON.stringify(s.self.death);b.x+=200;r.remove(b.id);assert.equal(JSON.stringify(r.snapshot(a).self.death),frozen);r.spawn(a);assert.equal(r.snapshot(a).self.death,null);
});
test('a real solo projectile retains its attacker through fatal damage and enters death view before results',()=>{
 const h=harness();h.run("reset();player.hp=1;player.shieldUntil=0;const enemy=enemies[0];enemy.name='巡逻兵';enemy.weapon='USP';enemy.ammo=12;enemy.cooldown=0;fire(enemy,false);const fatal=bullets.find(b=>!b.friendly);if(!fatal)throw Error('No enemy bullet');damage(player,fatal,false,player.x,player.y);finish(false)");assert.equal(h.run('player.death?.killerName'),'巡逻兵');assert.equal(h.run('DeathView.active'),true);assert.equal(h.run('running'),true);h.run('DeathView.skip()');assert.equal(h.run('running'),false);assert.match(h.els.resultText.textContent,/巡逻兵.*USP/);
});

test('downed players receive attribution without attacker coordinates; rescue clears it, finishing credits the finisher',()=>{
 const {r,a,b}=battle('teams'),enemy=[...r.players.values()][2],finisher=[...r.players.values()][3];r.hurt(a,{owner:enemy.id,weapon:'AWP'},100);let s=r.snapshot(a);assert.equal(a.downed,true);assert.equal(s.self.death,null);assert.equal(s.self.downedBy.killerName,'敌人甲');assert.equal('attacker' in s.self.downedBy,false);assert.equal('x' in s.self.downedBy,false);r.hurt(a,{owner:finisher.id,weapon:'USP'},5);s=r.snapshot(a);assert.equal(s.self.death.killerName,'敌人乙');assert.equal(s.self.death.reason,'finish');assert.equal(s.self.death.weapon,'USP');assert.equal(s.self.downedBy,null);assert.equal(a.deaths,1);
});
test('bleedout keeps the original downing attacker after they leave; self explosion is named correctly',()=>{
 const {r,a}=battle('teams'),enemy=[...r.players.values()][2];r.hurt(a,{owner:enemy.id,weapon:'AWP'},100);r.remove(enemy.id);r.time=a.bleedUntil;r.tick(.01);assert.equal(r.snapshot(a).self.death.killerName,'敌人甲');assert.equal(r.snapshot(a).self.death.reason,'bleedout');const f=battle();f.r.hurt(f.a,{owner:f.a.id,weapon:'NOVA',reason:'explosion',impact:{x:f.a.x,y:f.a.y},blastRadius:100},150);const d=f.r.snapshot(f.a).self.death;assert.equal(d.killerName,'你自己');assert.equal(d.reason,'self');assert.equal(d.blastRadius,100);
});
test('live snapshots never disclose bullet source or another player death scene',()=>{
 const {r,a,b}=battle();a.angle=0;b.angle=Math.PI;r.shoot(b);const packet=r.snapshot(a);assert.ok(packet.bullets.length);for(const bullet of packet.bullets){assert.equal('source' in bullet,false);assert.equal('owner' in bullet,false)}assert.equal(packet.self.death,null);assert.ok(packet.peers.every(p=>!('death' in p)));const shot=r.bullets[0],origin=shot.source.x;b.x+=200;r.hurt(a,shot,1000);assert.equal(r.snapshot(a).self.death.attacker.x,origin);
});
test('solo death camera freezes gameplay, renders the source, auto settles exactly once, and resets',()=>{
 const h=harness();h.run("reset();player.hp=1;damage(player,{weapon:'USP',damage:50,source:TZDeath.source(enemies[0],'USP')},false,player.x,player.y);finish(false)");const start=h.run('time'),runs=h.run('career.totalRuns');h.run('update(1);render()');assert.equal(h.run('time'),start);assert.equal(h.run('DeathView.active'),true);assert.match(h.els.deathCameraLabel.textContent,/开火位置定格/);h.run('update(2.3)');assert.equal(h.run('DeathView.active'),false);assert.equal(h.run('running'),false);assert.equal(h.run('career.totalRuns'),runs);h.run('DeathView.skip();reset()');assert.equal(h.run('DeathView.active'),false);assert.equal(h.run('player.death'),undefined);assert.equal(h.els.deathHUD.hidden,true);
});
test('unknown hits remain unknown and solo self explosions retain the source through explosion handling',()=>{
 const h=harness();h.run("reset();player.hp=1;Arsenal.explode({weapon:'NOVA',damage:100,source:TZDeath.source(player,'NOVA'),friendly:true},player.x,player.y);finish(false);render()");assert.equal(h.run('player.death.reason'),'self');assert.equal(h.run('player.death.weapon'),'NOVA');h.run("DeathView.skip();reset();player.hp=1;damage(player,{damage:100},false,player.x,player.y);finish(false);render()");assert.equal(h.run('player.death.killerName'),'未知来源');assert.equal(h.run('player.death.attacker'),null);
});
