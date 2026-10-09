const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
function build(){
const root=path.resolve(__dirname,'..'),dist=path.join(root,'dist'),html=fs.readFileSync(path.join(dist,'mobile-game.html'),'utf8');
const version=JSON.parse(html.match(/window\.OFFLINE_VERSION=("[^"]+")/)[1]);
// iPhone uses a stable HTTPS home-screen URL with a manually selected cached bundle.
const ios=path.join(dist,'iphone');fs.mkdirSync(ios,{recursive:true});
let iphone=html.replace('<head>','<head><link rel="manifest" href="/iphone/manifest.webmanifest">');
iphone=iphone.replace(/href="data:image\/png;base64,[^"]+"/,'href="/iphone/icon-192.png"');
iphone=iphone.replace('<p>离线单机无需连接电脑。需要更新时再下载新版；更新前导出存档，可在新版中恢复。好友联机请使用联网版。</p>',()=>fs.readFileSync(path.join(root,'mobile/iphone-panel.html'),'utf8'));
iphone=iphone.replace('存档与版本 <small>备份 · 恢复 · 手动更新</small>','安装与更新 <small>离线状态 · 存档备份</small>');
iphone=iphone.replace('<div class="homeLinks">','<p id="iphoneHomeStatus" role="status">正在准备离线游戏…</p><div class="homeLinks">');
iphone=iphone.replace('</main>','<style>#iphoneDownload[hidden],#iphoneApply[hidden]{display:none}#iphoneStatus,#iphoneHomeStatus{color:#d6df9c!important}#iphoneInstall,#iphoneUpdates{border-bottom:1px solid #426069;padding-bottom:12px;margin-bottom:12px}</style><script>'+fs.readFileSync(path.join(root,'mobile/iphone-ui.js'),'utf8')+'</script></main>');
for(const match of iphone.matchAll(/<script>([\s\S]*?)<\/script>/g))new vm.Script(match[1]);
const sha256=require('node:crypto').createHash('sha256').update(iphone).digest('hex'),bundle='game-'+sha256+'.json';
fs.writeFileSync(path.join(ios,bundle),JSON.stringify({html:iphone}));fs.writeFileSync(path.join(ios,'index.html'),iphone);
fs.writeFileSync(path.join(ios,'release.json'),JSON.stringify({protocol:1,version:version+'-iPhone',url:'/iphone/'+bundle,sha256}));
fs.writeFileSync(path.join(ios,'manifest.webmanifest'),JSON.stringify({id:'/iphone/',name:'战术禁区 · 离线版',short_name:'战术禁区',start_url:'/iphone/index.html',scope:'/iphone/',display:'standalone',orientation:'landscape',background_color:'#101516',theme_color:'#101516',icons:[192,512].map(size=>({src:'/iphone/icon-'+size+'.png',sizes:size+'x'+size,type:'image/png',purpose:'any'}))}));
for(const name of ['icon-192.png','icon-512.png'])fs.copyFileSync(path.join(dist,name),path.join(ios,name));
fs.copyFileSync(path.join(root,'mobile/iphone-sw.js'),path.join(ios,'sw.js'));
console.log('iPhone package: '+bundle);

}
module.exports=build;if(require.main===module)build();
