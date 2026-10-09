import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import WebSocket from 'ws';
import harnessModule from './harness.cjs';
import {createGameServer} from '../server/index.mjs';
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
async function waitFor(fn){for(let i=0;i<120;i++){if(fn())return;await delay(50)}throw Error('Client state timeout')}
function gameClient(origin){const h=harnessModule.harness(),b=h.box,intervals=[],timeouts=[];const decorate=e=>{e.children=[];e.append=(...items)=>e.children.push(...items);e.appendChild=x=>e.children.push(x);e.select=()=>{};return e};for(const e of Object.values(h.els))decorate(e);const create=b.document.createElement;b.document.createElement=()=>decorate(create());const get=b.document.getElementById;b.document.getElementById=id=>{const e=get(id);return e.append?e:decorate(e)};
 Object.assign(b,{WebSocket,URL,AbortSignal,location:{href:origin+'/',host:new URL(origin).host,protocol:'http:'},sessionStorage:{getItem:()=>null,setItem(){},removeItem(){}},fetch:(url,opts)=>fetch(new URL(url,origin),opts),setInterval:(fn,ms)=>{const id=setInterval(fn,ms);intervals.push(id);return id},setTimeout:(fn,ms)=>{const id=setTimeout(fn,ms);timeouts.push(id);return id},clearTimeout});
 vm.runInContext(fs.readFileSync(new URL('../dist/network.js',import.meta.url),'utf8'),b);
 const timer=setInterval(()=>{h.run('if(TZNet.active){TZNet.update(1/60);render()}')},1000/60);intervals.push(timer);
 return {...h,close(){h.run('TZNet.leave()');intervals.forEach(clearInterval);timeouts.forEach(clearTimeout)}};
}
test('two complete game adapters go through lobby, manual touch controls, pause, rematch and solo return',{timeout:16000},async()=>{const server=createGameServer({port:0,host:'127.0.0.1'}),address=await server.ready;const a=gameClient('http://127.0.0.1:'+address.port),b=gameClient('http://127.0.0.1:'+address.port);
 try{await a.run('TZNet.open()');a.run("$('netMap').value='docks';$('netName').value='Alpha';TZNet.connect('create')");await waitFor(()=>a.run('!!TZNet.state'));const code=a.run('TZNet.code');await b.run('TZNet.open()');b.box.document.getElementById("netCode").value=code;b.run("$('netName').value='Bravo';TZNet.connect('join')");await waitFor(()=>a.run('TZNet.state.roster.length===2'));a.run('TZNet.send({type:"ready",ready:true})');b.run('TZNet.send({type:"ready",ready:true})');await waitFor(()=>a.run('TZNet.state.roster.every(p=>p.ready)'));a.run('TZNet.send({type:"start"})');await waitFor(()=>a.run('TZNet.active&&TZNet.state.status==="playing"')&&b.run('TZNet.active&&TZNet.state.status==="playing"'));
  assert.equal(a.run('gameMode'),'multi');assert.equal(b.run('activeMap.id'),'docks');assert.equal(a.run('career.totalRuns'),0);a.run('sticks.aim.id=5;sticks.aim.x=0;sticks.aim.y=0');assert.equal(a.run('TZNet.controls().fire'),false);a.run('sticks.aim.x=1');assert.equal(a.run('TZNet.controls().fire'),true);assert.equal(a.run('TZNet.controls().angle'),0);a.run('TZNet.pause()');assert.equal(a.run('TZNet.controls().fire'),false);assert.equal(a.run('paused'),false);a.run('TZNet.returnToBattle()');assert.equal(a.run('TZNet.menu'),false);
  const room=server.rooms.get(code),victim=room.players.get(a.run('TZNet.id')),killer=room.players.get(b.run('TZNet.id'));victim.shieldUntil=0;room.hurt(victim,{owner:killer.id,weapon:'AWP'},1000);killer.kills=room.settings.scoreLimit;await waitFor(()=>a.run('TZNet.state.status==="finished"'));assert.equal(a.run('running'),false);assert.equal(a.run('DeathView.active'),true);assert.equal(a.run('DeathView.mode'),'final');assert.equal(a.run('DeathView.record.killerName'),'Bravo');a.run('update(3.3);render()');assert.equal(a.run('DeathView.active'),false);a.run('TZNet.send({type:"ready",ready:true})');b.run('TZNet.send({type:"ready",ready:true})');await waitFor(()=>a.run('TZNet.state.roster.every(p=>p.ready)'));a.run('TZNet.send({type:"start"})');await waitFor(()=>a.run('TZNet.active&&TZNet.round===2'));a.run('TZNet.leave();showScreen("home");reset()');assert.equal(a.run('gameMode'),'solo');assert.equal(a.run('career.totalRuns'),1);assert.equal(a.run('running'),true);
 }finally{a.close();b.close();await server.close()}
});
function rulesSeconds(){return 301}
test('four full clients start 2v2, render operations and rescue a downed ally over WebSocket',{timeout:16000},async()=>{
 const server=createGameServer({port:0,host:'127.0.0.1'}),address=await server.ready,clients=Array.from({length:4},()=>gameClient('http://127.0.0.1:'+address.port));
 try{for(const c of clients)await c.run('TZNet.open()');const a=clients[0];a.run("ClassUI.choose('scout');$('netMode').value='teams';$('netName').value='Alpha';TZNet.connect('create')");await waitFor(()=>a.run('!!TZNet.state'));const code=a.run('TZNet.code');for(const [i,c]of clients.entries()){if(!i)continue;c.box.document.getElementById("netCode").value=code;c.run(`$('netName').value='Player${i}';TZNet.connect('join')`);await waitFor(()=>c.run('!!TZNet.state'))}await waitFor(()=>a.run('TZNet.state.roster.length===4'));clients[1].run("ClassUI.netCards.find(c=>c.id==='medic').button.listeners.click()");await waitFor(()=>clients[1].run("TZNet.state.self.classId==='medic'"));for(const c of clients)c.run('TZNet.send({type:"ready",ready:true})');await waitFor(()=>a.run('TZNet.state.roster.every(p=>p.ready)'));a.run('TZNet.send({type:"start"})');await waitFor(()=>clients.every(c=>c.run('TZNet.state.status==="playing"')));const room=server.rooms.get(code),p=room.players.get(a.run('TZNet.id')),helper=room.players.get(clients[1].run('TZNet.id')),enemy=room.players.get(clients[2].run('TZNet.id'));Object.assign(p,{x:224,y:1056,shieldUntil:0});Object.assign(helper,{x:224,y:1106,shieldUntil:0});room.hurt(p,{owner:enemy.id,weapon:'AWP'},100);await waitFor(()=>a.run('player.downed===true'));await waitFor(()=>/击倒/.test(a.els.respawnNotice.textContent));clients[1].run('TZNet.send({type:"interact"})');await waitFor(()=>!!helper.task);await waitFor(()=>a.run('player.hp===45'));assert.equal(p.deaths,0);assert.match(clients[1].els.enemyCount.textContent,/青队/);assert.ok(clients[1].els.objectiveText.textContent);assert.equal(a.run('autoAimTarget'),null);assert.equal(a.run('player.classId'),'scout');clients[1].run('ClassUI.cast()');await waitFor(()=>a.run('player.hp===75'));assert.equal(helper.classId,'medic');assert.ok(helper.skillReadyAt>room.time);assert.match(clients[1].els.classStatus.textContent,/医疗兵/);p.shieldUntil=0;room.hurt(p,{owner:enemy.id,weapon:'AWP'},1000);room.hurt(p,{owner:enemy.id,weapon:'USP'},1);await waitFor(()=>a.run('DeathView.active'));assert.equal(a.run('DeathView.record.killerName'),enemy.name);assert.equal(a.run('DeathView.record.reason'),'finish');assert.equal(a.run('TZNet.controls().fire'),false);room.spawn(p);await waitFor(()=>a.run('player.hp>0&&!DeathView.active'));assert.equal(a.els.deathHUD.hidden,true);
 }finally{clients.forEach(c=>c.close());await server.close()}
});
test('old server terrain is rejected before joining and during saved-room reconnect',async()=>{
 const a=gameClient('http://localhost:4173');a.box.fetch=async()=>({ok:true,json:async()=>({multiplayer:true,protocol:1})});
 try{await a.run('TZNet.open()');assert.equal(a.run('TZNet.available'),false);assert.equal(a.els.netCreate.disabled,true);assert.match(a.els.netStatus.textContent,/地图版本/);a.run("TZNet.receive({type:'state',mapRevision:'old'})");assert.equal(a.run('TZNet.active'),false);assert.match(a.els.netStatus.textContent,/地图版本/)}finally{a.close()}
});
test('invitations never copy loopback URLs to a friend; LAN and cloud links keep their reachable origin',async()=>{
 for(const origin of ['http://localhost:4173','http://127.0.0.1:4173','http://[::1]:4173','http://192.168.10.143:4173','https://play.example.com']){
  const a=gameClient(origin),copied=[];a.box.navigator.clipboard={writeText:async text=>copied.push(text)};
  try{a.run("TZNet.code='123456'");await a.run('TZNet.invite()');
   if(origin.includes('192.168.')||origin.startsWith('https:'))assert.equal(copied[0],origin+'/?room=123456');
   else{assert.equal(copied.length,0);assert.match(a.els.netStatus.textContent,/局域网/)}
  }finally{a.close()}
 }
});


