const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs'),vm=require('node:vm');
const {env}=require('./harness.cjs'),{keys,validate}=require('../mobile/save.js');
test('single-file mobile game boots, renders and plays without any network APIs',()=>{
 const html=fs.readFileSync(require('node:path').join(__dirname,'../mobile/build/game.html'),'utf8'),e=env();
 e.box.navigator={};e.box.location={protocol:'file:'};
 assert.ok(!/<script\s+src=|<link[^>]*rel="stylesheet"/.test(html));
 for(const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g))vm.runInContext(match[1],e.box);
 vm.runInContext('reset();for(let i=0;i<60;i++){update(STEP);render()}',e.box);
 assert.equal(vm.runInContext('running',e.box),true);assert.ok(vm.runInContext('time',e.box)>.9);
 assert.equal(vm.runInContext('typeof TZNet',e.box),'undefined');
 assert.ok(vm.runInContext('Object.values(OFFLINE_PORTRAITS).every(x=>x.startsWith("data:image/png;base64,"))',e.box));
 e.els.homeMultiplayer.listeners.click();assert.equal(e.els.offlinePanel.hidden,false);
});
test('save import only restores game keys and rejects invalid backups',()=>{
 const raw=JSON.stringify({format:'tactical-zero-save',version:1,values:{'tactical-class':'medic','tactical-stash-v1':'{"version":1,"value":500}',unrelated:'secret'}});
 const restored=validate(raw);assert.equal(restored['tactical-class'],'medic');assert.equal(restored.unrelated,undefined);assert.ok(keys.includes('tactical-building-settings-v1'));assert.ok(keys.includes('tactical-building-records-v1')); 
 assert.throws(()=>validate('{}'));assert.throws(()=>validate('x'.repeat(1000001)));
 assert.throws(()=>validate(JSON.stringify({format:'tactical-zero-save',version:1,values:{'tactical-class':'invalid'}})));
});
test('mobile save UI exports and restores progress without touching unrelated storage',()=>{
 const e=env({'tactical-class':'medic','tactical-stash-v1':'{"version":1,"value":500}',unrelated:'keep'});let exported,reloads=0;
 e.box.NativeGame={exportSave:text=>exported=text};e.box.location={reload:()=>reloads++};e.box.localStorage.removeItem=key=>delete e.values[key];
 vm.runInContext(fs.readFileSync(require('node:path').join(__dirname,'../mobile/save.js'),'utf8'),e.box);
 e.els.saveExport.listeners.click();assert.equal(validate(exported)['tactical-class'],'medic');
 e.values['tactical-class']='assault';e.values['tactical-difficulty']='hell';
 e.box.previewLocalSave(exported);assert.equal(e.values['tactical-class'],'assault');assert.equal(e.els.saveRestore.hidden,false);
 e.els.saveRestore.listeners.click();assert.equal(e.values['tactical-class'],'medic');assert.equal(e.values['tactical-stash-v1'],'{"version":1,"value":500}');
 assert.equal(e.values['tactical-difficulty'],undefined);assert.equal(e.values.unrelated,'keep');assert.equal(reloads,1);
});
