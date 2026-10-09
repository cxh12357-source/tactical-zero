'use strict';
// The standalone iPhone package uses its existing verified/manual updater instead.
if('serviceWorker' in navigator&&(location.protocol==='https:'||['localhost','127.0.0.1'].includes(location.hostname))){
 let applying=false;
 navigator.serviceWorker.addEventListener('controllerchange',()=>{if(applying)location.reload()});
 navigator.serviceWorker.register('/sw.js',{updateViaCache:'none'}).then(registration=>{
  window.TZApplyBuildingUpdate=()=>{if(!registration.waiting||running)return false;applying=true;registration.waiting.postMessage({type:'APPLY_BUILDING_UPDATE'});return true};
  const ready=()=>{if(!registration.waiting)return;if(!running&&['home','sweepweapons'].includes($('overlay').dataset.screen))window.TZApplyBuildingUpdate();else if(running)toast('新版已下载，将在返回首页后应用',3)};
  ready();registration.addEventListener('updatefound',()=>{const worker=registration.installing;worker?.addEventListener('statechange',()=>{if(worker.state==='installed'&&navigator.serviceWorker.controller)ready()})});
 }).catch(()=>{});
}
