// One small same-origin offline shell. New releases activate on the next launch;
// never replace the game while a round is in progress.
const CACHE='tactical-zero-4.0.0';
const SHELL=['/','/index.html','/manifest.webmanifest','/icon-192.png','/icon-512.png','/world.js','/expedition.js','/network.js','/field.css','/art.js','/scenery.js','/tactics.js','/fieldops.js','/classes.js','/class-ui.js','/operator-art.js','/death-rules.js','/death-view.js','/room-ui.js','/arsenal.js','/controls.js','/src/building.css','/src/building-data.js','/src/building-systems.js','/src/building-game.js','/src/pwa-update.js'];
const PORTRAITS=['assault','scout','medic','engineer','guardian','marksman','infiltrator','supplier'].map(id=>'/assets/operators/'+id+'.png');
self.addEventListener('install',event=>event.waitUntil(caches.open(CACHE).then(cache=>cache.addAll([...SHELL,...PORTRAITS]))));
self.addEventListener('activate',event=>event.waitUntil(
  caches.keys().then(keys=>Promise.all(keys.filter(key=>key.startsWith('tactical-zero-')&&key!==CACHE).map(key=>caches.delete(key)))).then(()=>self.clients?.claim())
));
self.addEventListener('message',event=>{if(event.data?.type==='APPLY_BUILDING_UPDATE')self.skipWaiting()});
self.addEventListener('fetch',event=>{
  const url=new URL(event.request.url);
  if(event.request.method!=='GET'||url.origin!==self.location.origin||!SHELL.includes(url.pathname)&&!PORTRAITS.includes(url.pathname))return;
  event.respondWith((async()=>{
    const cache=await caches.open(CACHE);
    // A release is one immutable shell. Avoid combining a new HTML response with
    // old scripts when a deployment or connection is interrupted.
    if(SHELL.includes(url.pathname)){const ready=await cache.match(url.pathname);if(ready)return ready}
    try{
      const response=await fetch(event.request);
      if(response.ok&&!response.redirected){await cache.put(event.request,response.clone());return response}
      return (await cache.match(event.request))||response;
    }catch(error){
      const cached=(await cache.match(event.request))||(event.request.mode==='navigate'?await cache.match('/'):null);
      if(cached)return cached;throw error;
    }
  })());
});

