const {test}=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const {harness}=require('./harness.cjs');
test('each profession has an independent silhouette, headgear, pack and complete dossier',()=>{
 const h=harness();assert.equal(h.run('Object.keys(OperatorArt.looks).length'),8);
 assert.equal(h.run('new Set(Object.values(OperatorArt.looks).map(x=>x.head)).size'),8);
 assert.equal(h.run('new Set(Object.values(OperatorArt.looks).map(x=>x.pack)).size'),8);
 for(const group of ['cards','soloCards','netCards']){const cards=h.run('ClassUI.'+group);assert.equal(cards.length,8);for(const {id,button}of cards){assert.equal(button.children.length,1);const portrait=button.children[0];assert.equal(portrait.src,`assets/operators/${id}.png`);assert.ok(portrait.alt.includes('立绘'));assert.equal(portrait.loading,'lazy')}}const model=h.els.selectedClassModel;assert.equal(model.width,280);assert.equal(model.height,104);h.run("ClassUI.soloCards.find(c=>c.id==='medic').button.listeners.click();reset()");assert.equal(h.run('player.classId'),'medic');assert.equal(h.run("ClassUI.cards.filter(c=>c.button.classList.contains('selected')).length"),1)
});
test('different rigs produce different drawing geometry; animation and death keep gameplay state intact',()=>{
 const h=harness();h.box.drawTraces=[];
 h.run(`reset();const originalState=JSON.stringify(player);for(const id of Object.keys(OperatorArt.looks)){const calls=[];const g=new Proxy({},{get:(o,k)=>o[k]||((...v)=>calls.push([k,...v])),set:(o,k,v)=>(o[k]=v,true)});for(const pose of [{moving:0},{moving:1,flash:.06},{moving:.4,reload:.8}])Art.actor({...player,...pose,classId:id},true,false,g);Art.actor({...player,classId:id},false,true,g);drawTraces.push(JSON.stringify(calls))}if(JSON.stringify(player)!==originalState)throw Error('Rendering mutated player')`);
 assert.equal(new Set(h.box.drawTraces).size,8);assert.ok(h.box.drawTraces.every(x=>!x.includes('null')));
});
test('portrait files are valid bounded PNG assets at the intended display resolution',()=>{
 for(const id of ['assault','scout','medic','engineer','guardian','marksman','infiltrator','supplier']){const b=fs.readFileSync(`dist/assets/operators/${id}.png`);assert.equal(b.toString('ascii',1,4),'PNG');assert.equal(b.readUInt32BE(16),512);assert.equal(b.readUInt32BE(20),768);assert.ok(b.length>5000&&b.length<350000)}
});
