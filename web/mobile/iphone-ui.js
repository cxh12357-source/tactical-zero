(function(){
const $=id=>document.getElementById(id),status=$('iphoneStatus'),update=$('iphoneUpdateStatus');let registration=null,busy=false;
function request(type){return new Promise((resolve,reject)=>{const worker=registration?.active;if(!worker)return reject(Error('离线安装尚未完成，请保持联网'));const channel=new MessageChannel(),timer=setTimeout(()=>{channel.port1.close();reject(Error('操作超时，请检查网络后重试'))},90000);channel.port1.onmessage=e=>{clearTimeout(timer);channel.port1.close();e.data.ok?resolve(e.data):reject(Error(e.data.error))};worker.postMessage({type},[channel.port2])})}
function display(text){status.textContent=text;$('iphoneHomeStatus').textContent=text+' · 点“安装与更新”查看';}
async function showStatus(){const s=await request('STATUS');display(s.ready?'已可离线游玩 · 本机版本 '+s.active.version:'离线资源未就绪，请保持联网');$('iphoneApply').hidden=!s.pending;return s}
async function install(){
 display('正在保存离线游戏，请保持联网…');
 try{if(!window.isSecureContext||!('serviceWorker'in navigator))throw Error('请在 Safari 中打开 HTTPS 安装链接');registration=await navigator.serviceWorker.register('/iphone/sw.js',{scope:'/iphone/',updateViaCache:'none'});
  if(!registration.active)await new Promise((resolve,reject)=>{const w=registration.installing||registration.waiting;if(!w)return reject(Error('安装未开始，请重试'));if(w.state==='activated')return resolve();const timer=setTimeout(()=>reject(Error('下载超时，请重试')),90000);w.addEventListener('statechange',()=>{if(w.state==='activated'){clearTimeout(timer);resolve()}else if(w.state==='redundant'){clearTimeout(timer);reject(Error('离线下载失败，请检查网络和存储空间后重试'))}})});
  await showStatus();
 }catch(e){display(e.message+'。可点击“重试离线安装”。')}
}
$('iphoneRetry').addEventListener('click',install);
$('iphoneCheck').addEventListener('click',async()=>{if(busy)return;busy=true;update.textContent='正在检查…';try{const s=await request('CHECK');update.textContent=s.available?'发现版本 '+s.latest.version+'，由你决定是否下载。':'当前已是最新版。';$('iphoneDownload').hidden=!s.available}catch(e){update.textContent=e.message}finally{busy=false}});
$('iphoneDownload').addEventListener('click',async()=>{if(busy)return;busy=true;update.textContent='正在下载并校验，当前版本会保留…';try{const s=await request('DOWNLOAD');update.textContent=s.current?'当前已是最新版。':'新版已下载。可先导出存档，再点击“更新并重新打开”。';$('iphoneApply').hidden=!s.pending;$('iphoneDownload').hidden=true}catch(e){update.textContent=e.message}finally{busy=false}});
$('iphoneApply').addEventListener('click',async()=>{if(busy)return;busy=true;try{await request('APPLY');location.replace('/iphone/index.html')}catch(e){update.textContent=e.message;busy=false}});
install();
})();
