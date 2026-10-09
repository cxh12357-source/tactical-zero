// Stable offline protocol: publishing or updating this worker never changes the selected game.
const CACHE='iphone-tz-v1',BASE='/iphone/',STATE=BASE+'selected-version';
const STATIC=[BASE+'manifest.webmanifest',BASE+'icon-192.png',BASE+'icon-512.png'];
const json=value=>new Response(JSON.stringify(value),{headers:{'Content-Type':'application/json'}});
async function state(){const c=await caches.open(CACHE),r=await c.match(STATE);return r?await r.json():{active:null,pending:null}}
async function write(value){const c=await caches.open(CACHE);await c.put(STATE,json(value))}
async function release(){const r=await fetch(BASE+'release.json',{cache:'no-store'});if(!r.ok||r.redirected)throw Error('无法读取新版信息，请稍后再试');const v=await r.json();if(v.protocol!==1||typeof v.version!=='string'||!/^\/iphone\/game-[a-f0-9]{64}\.json$/.test(v.url)||!/^[a-f0-9]{64}$/.test(v.sha256)||v.url!==BASE+'game-'+v.sha256+'.json')throw Error('版本信息无效');return v}
async function download(v){const r=await fetch(v.url,{cache:'no-store'});if(!r.ok||r.redirected)throw Error('下载失败，当前版本已保留');const payload=await r.json();if(typeof payload.html!=='string')throw Error('游戏包内容无效');const bytes=new TextEncoder().encode(payload.html);if(bytes.byteLength>16000000)throw Error('游戏文件过大');const digest=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',bytes)),b=>b.toString(16).padStart(2,'0')).join('');if(digest!==v.sha256)throw Error('文件校验失败，当前版本已保留');const c=await caches.open(CACHE);await c.put(v.url,new Response(bytes,{headers:{'Content-Type':'text/html; charset=utf-8'}}))}
async function ensureInstalled(){const s=await state(),c=await caches.open(CACHE);if(s.active&&await c.match(s.active.url))return s;const v=await release();await download(v);await c.addAll(STATIC);const next={active:v,pending:null};await write(next);return next}
self.addEventListener('install',e=>e.waitUntil(ensureInstalled()));
self.addEventListener('activate',e=>e.waitUntil(self.clients.claim()));
let mutations=Promise.resolve();
async function command(type){
 const s=await state(),c=await caches.open(CACHE);
 if(type==='STATUS')return {ready:!!(s.active&&await c.match(s.active.url)),active:s.active,pending:s.pending};
 if(type==='CHECK'){const latest=await release();return{latest,available:latest.sha256!==s.active?.sha256}}
 if(type==='DOWNLOAD'){const latest=await release();if(latest.sha256===s.active?.sha256)return{current:true,active:s.active};await download(latest);await write({...s,pending:latest});return{pending:latest}}
 if(type==='APPLY'){if(!s.pending||!await c.match(s.pending.url))throw Error('请先下载完整的新版本');await write({active:s.pending,pending:null});return{active:s.pending}}
 throw Error('未知操作');
}
self.addEventListener('message',e=>{
 if(!e.source?.url||new URL(e.source.url).origin!==self.location.origin||!new URL(e.source.url).pathname.startsWith(BASE)||!e.ports[0])return;
 const operation=mutations.then(()=>command(e.data?.type));mutations=operation.catch(()=>{});
 e.waitUntil(operation.then(value=>e.ports[0].postMessage({ok:true,...value}),error=>e.ports[0].postMessage({ok:false,error:error.message})));
});
self.addEventListener('fetch',e=>{
 const u=new URL(e.request.url);if(e.request.method!=='GET'||u.origin!==self.location.origin||!u.pathname.startsWith(BASE))return;
 if(u.pathname===BASE||u.pathname===BASE+'index.html')e.respondWith((async()=>{const s=await state(),c=await caches.open(CACHE);if(s.active){const hit=await c.match(s.active.url);if(hit)return hit}return fetch(e.request)})());
 else if(STATIC.includes(u.pathname))e.respondWith(caches.open(CACHE).then(async c=>(await c.match(u.pathname))||fetch(e.request)));
});
