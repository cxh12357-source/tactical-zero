const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
const root=path.resolve(__dirname,'..'),dist=path.join(root,'dist'),version=JSON.parse(fs.readFileSync(path.join(root,'package.json'),'utf8')).version;
const dataImage=file=>'data:image/png;base64,'+fs.readFileSync(path.join(dist,file)).toString('base64');
const portraits=Object.fromEntries(['assault','scout','medic','engineer','guardian','marksman','infiltrator','supplier'].map(id=>[id,dataImage('assets/operators/'+id+'.png')]));
let html=fs.readFileSync(path.join(dist,'index.html'),'utf8');
html=html.replace(/<link rel="manifest"[^>]*>/,'').replace('href="/icon-192.png"','href="'+dataImage('icon-192.png')+'"');
html=html.replace(/<link rel="stylesheet" href="([^"]+)">/g,(_,file)=>'<style>'+fs.readFileSync(path.join(dist,file),'utf8')+'</style>');
html=html.replace(/if\('serviceWorker'[^\n]+navigator.serviceWorker.register\('\/sw.js'\).catch\(\(\)=>\{\}\)\}/,'');
html=html.replace(/<script src="([^"]+)"><\/script>/g,(_,file)=>{
 if(file==='network.js'||file==='src/pwa-update.js')return '';
 let code=fs.readFileSync(path.join(dist,file),'utf8');if(file==='class-ui.js')code=code.replace("portrait.src='assets/operators/'+id+'.png'",'portrait.src=OFFLINE_PORTRAITS[id]');
 return '<script>\n'+code.replace(/<\/script/gi,'<\\/script')+'\n</script>';
});
html=html.replace('<head>','<head><script>const OFFLINE_PORTRAITS='+JSON.stringify(portraits)+';window.OFFLINE_VERSION='+JSON.stringify(version)+';</script>');
html=html.replace(/<title>[^<]*<\/title>/,'<title>战术禁区 · 手机版 '+version+'</title>');
html=html.replace(/TACTICAL ZERO 3\.7 \/ SKIRMISH/,'TACTICAL ZERO '+version+' / OFFLINE');
html=html.replace('单机自动瞄准 · 联机手动对战','离线单机 · 本地存档 · 手动更新');
html=html.replace('<button id="homeMultiplayer">好友对战 <small>2–4 人 · 手动瞄准</small></button>','<button id="homeMultiplayer">存档与版本 <small>备份 · 恢复 · 手动更新</small></button>');
const panel=`<section id="offlinePanel" hidden role="dialog" aria-label="本地存档与版本"><div><h2>本地存档与版本</h2><p>手机版 ${version} · 游戏资源已全部内置</p><p>离线单机无需连接电脑。需要更新时再下载新版；更新前导出存档，可在新版中恢复。好友联机请使用联网版。</p><button id="saveExport">导出存档</button><label class="offlineImport">选择存档文件<input id="saveImport" type="file" accept=".json,application/json"></label><button id="saveRestore" hidden>确认恢复并重新载入</button><p id="offlineStatus" role="status"></p><button id="offlineClose">返回游戏</button></div></section><style>#offlinePanel{position:absolute;inset:0;z-index:40;background:#08131cf2;display:grid;place-items:center;padding:20px;overflow:auto;touch-action:pan-y}#offlinePanel[hidden],#saveRestore[hidden]{display:none}#offlinePanel>div{width:min(500px,100%);max-height:100%;overflow:auto}#offlinePanel p{font-size:15px;line-height:1.7;color:#c7d4d9}#offlinePanel button,.offlineImport{display:block;width:100%;font:inherit;font-size:16px;padding:12px;margin:10px 0;border:1px solid #759ba7;border-radius:6px;background:#21424b;color:#edf1de;cursor:pointer}#offlinePanel input{display:block;max-width:100%;margin-top:10px}#offlineStatus{color:#f0d6a3!important}</style>`;
html=html.replace('</main>',panel+'<script>'+fs.readFileSync(path.join(root,'mobile/save.js'),'utf8')+'</script></main>');
for(const match of html.matchAll(/<script>([\s\S]*?)<\/script>/g))new vm.Script(match[1]);
if(/<script\s+src=|<link[^>]*rel="stylesheet"/.test(html))throw Error('External dependencies remain');
fs.mkdirSync(path.join(root,'mobile/build'),{recursive:true});
fs.writeFileSync(path.join(root,'mobile/build/game.html'),html);
fs.writeFileSync(path.join(dist,'mobile-game.html'),html);
fs.writeFileSync(path.join(dist,'mobile.html'),fs.readFileSync(path.join(root,'mobile/download.html'),'utf8').replaceAll('{{VERSION}}',version));
if(process.argv[2]){fs.mkdirSync(process.argv[2],{recursive:true});fs.writeFileSync(path.join(process.argv[2],'TacticalZero-Mobile.html'),html)}
console.log(`Offline ${version}: ${Buffer.byteLength(html)} bytes; all scripts and eight portraits embedded.`);
require('./build-iphone.cjs')();
