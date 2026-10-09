const fs=require('node:fs'),vm=require('node:vm'),path=require('node:path');
const html=fs.readFileSync('dist/index.html','utf8');
for(const [,body] of html.matchAll(/<script>([\s\S]*?)<\/script>/g))new vm.Script(body);
for(const [,src]of html.matchAll(/<script src="([^"]+)"/g))new vm.Script(fs.readFileSync(path.join('dist',src),'utf8'),{filename:src});
const ids=[...html.matchAll(/id="([^"]+)"/g)].map(m=>m[1]);if(new Set(ids).size!==ids.length)throw Error('Duplicate DOM IDs');
for(const file of ['dist/index.html','dist/expedition.js','dist/network.js','dist/art.js','dist/arsenal.js','dist/scenery.js','dist/fieldops.js','dist/class-ui.js','dist/operator-art.js','dist/death-view.js','dist/src/building-game.js','dist/src/pwa-update.js'])for(const [,id]of fs.readFileSync(file,'utf8').matchAll(/\$\('([^']+)'\)/g))if(!ids.includes(id))throw Error('Missing element '+id);
console.log('Scripts parse; referenced controls exist; no duplicate DOM IDs.');

