import http from 'node:http';
import {readFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {resolve,extname,sep} from 'node:path';
import {randomInt} from 'node:crypto';
import {WebSocketServer,WebSocket} from 'ws';
import {Room,rules} from './simulation.mjs';

const root=fileURLToPath(new URL('../dist/',import.meta.url));
const types={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.json':'application/json','.css':'text/css; charset=utf-8','.webmanifest':'application/manifest+json','.png':'image/png'};
export function createGameServer({port=Number(process.env.PORT)||4173,host=process.env.HOST||'0.0.0.0',maxRooms=64}={}){
 const rooms=new Map(),sockets=new Set(),limits=new Map();
 const send=(ws,data)=>{if(ws.readyState===WebSocket.OPEN&&ws.bufferedAmount<256*1024)ws.send(JSON.stringify(data))};
 const server=http.createServer(async(req,res)=>{
  res.setHeader('X-Content-Type-Options','nosniff');res.setHeader('Referrer-Policy','same-origin');
  if(!['GET','HEAD'].includes(req.method)){res.writeHead(405);res.end();return}
  try{const url=new URL(req.url,'http://localhost');
   if(url.pathname==='/health'){res.writeHead(200,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({ok:true,protocol:1,rooms:rooms.size}));return}
   if(url.pathname==='/api/config'){res.writeHead(200,{'Content-Type':'application/json','Cache-Control':'no-store'});res.end(JSON.stringify({multiplayer:true,protocol:1,mapRevision:rules.CONFIG.mapRevision}));return}
   const path=resolve(root,'.'+(url.pathname==='/'?'/index.html':decodeURIComponent(url.pathname)));if(!path.startsWith(resolve(root)+sep))throw Error();
   if(!types[extname(path)])throw Error();const body=await readFile(path);res.writeHead(200,{'Content-Type':types[extname(path)],'Cache-Control':'no-cache'});res.end(req.method==='HEAD'?undefined:body);
  }catch{res.writeHead(404);res.end('Not found')}
 });
 const wss=new WebSocketServer({noServer:true,maxPayload:2048,perMessageDeflate:false});
 server.on('upgrade',(req,socket,head)=>{
  const reject=()=>{socket.write('HTTP/1.1 403 Forbidden\r\nConnection: close\r\n\r\n');socket.destroy()};
  if(req.url!=='/ws'||sockets.size>=maxRooms*4+16)return reject();
  // Same-origin by default. Reverse proxies must preserve Host. Explicit origins may be added for a separately hosted frontend.
  if(req.headers.origin){let origin;try{origin=new URL(req.headers.origin)}catch{return reject()}const allowed=(process.env.ALLOWED_ORIGINS||'').split(',');if(origin.host!==req.headers.host&&!allowed.includes(origin.origin))return reject()}
  const ip=req.socket.remoteAddress||'unknown',now=Date.now();let limit=limits.get(ip);if(!limit||now-limit.at>60000){limit={at:now,n:0};limits.set(ip,limit)}if(++limit.n>60)return reject();
  wss.handleUpgrade(req,socket,head,ws=>wss.emit('connection',ws,req));
 });
 wss.on('connection',ws=>{
  sockets.add(ws);let room=null,player=null,windowAt=Date.now(),messages=0;ws.alive=true;ws.on('pong',()=>ws.alive=true);
  ws.on('message',raw=>{try{if(Date.now()-windowAt>1000){windowAt=Date.now();messages=0}if(++messages>90){ws.close(1008,'Too many messages');return}const msg=JSON.parse(raw.toString());if(!msg||typeof msg!=='object'||Array.isArray(msg))return;
   if(msg.type==='ping'){send(ws,{type:'pong',at:msg.at});return}
   if(msg.type==='leave'){if(room&&player)room.remove(player.id);room=player=null;send(ws,{type:'left'});return}
   if(!player){
    if(msg.type==='create'){if(rooms.size>=maxRooms)throw Error('服务器房间已满，请稍后重试');let code;do{code=String(randomInt(100000,1000000))}while(rooms.has(code));room=new Room(code,Object.hasOwn(rules.MAPS,msg.mapId)?msg.mapId:'sector07',msg.mode,msg.settings);rooms.set(code,room);player=room.add(msg.name,msg.classId)}
    else if(msg.type==='join'){room=rooms.get(String(msg.code));if(!room)throw Error('房间不存在或已关闭');player=room.add(msg.name,msg.classId)}
    else if(msg.type==='resume'){room=rooms.get(String(msg.code));if(!room)throw Error('房间已关闭');player=room.resume(msg.token);for(const old of sockets)if(old!==ws&&old.playerId===player.id){old.detached=true;old.close(1000,'Reconnected elsewhere')}}
    else return;
    ws.playerId=player.id;ws.roomCode=room.code;send(ws,{type:'welcome',id:player.id,token:player.token,code:room.code});send(ws,room.snapshot(player));return;
   }
   if(room.players.get(player.id)!==player){send(ws,{type:'error',message:'重连时间已过，请重新加入'});ws.close(1000);return}
   room.action(player,msg);
  }catch(error){send(ws,{type:'error',message:error instanceof SyntaxError?'消息格式错误':error.message})}});
  ws.on('close',()=>{sockets.delete(ws);if(!ws.detached&&room&&player)room.disconnect(player.id)});ws.on('error',()=>{});
  // Read the closure, not mutable packet fields, when publishing personalized state.
  ws.publish=()=>{if(room&&player&&room.players.has(player.id))send(ws,room.snapshot(player))};
 });
 let prior=performance.now(),accumulator=0,broadcastClock=0;
 const loop=setInterval(()=>{const now=performance.now();accumulator=Math.min(.25,accumulator+(now-prior)/1000);prior=now;while(accumulator>=1/rules.CONFIG.tickRate){for(const room of rooms.values())room.tick(1/rules.CONFIG.tickRate);accumulator-=1/rules.CONFIG.tickRate;broadcastClock++}
  if(broadcastClock>=rules.CONFIG.tickRate/rules.CONFIG.snapshotRate){broadcastClock=0;for(const ws of sockets)ws.publish()}
  for(const [code,room]of rooms)if(!room.players.size)rooms.delete(code);
 },1000/rules.CONFIG.tickRate);
 const heartbeat=setInterval(()=>{for(const ws of sockets){if(!ws.alive){ws.terminate();continue}ws.alive=false;ws.ping()}const now=Date.now();for(const [ip,limit]of limits)if(now-limit.at>120000)limits.delete(ip)},10000);
 const ready=new Promise(resolve=>server.listen(port,host,()=>resolve(server.address())));
 return {server,rooms,ready,async close(){clearInterval(loop);clearInterval(heartbeat);for(const ws of sockets)ws.terminate();await new Promise(resolve=>wss.close(resolve));await new Promise(resolve=>server.close(resolve))}};
}
if(process.argv[1]&&resolve(process.argv[1])===fileURLToPath(import.meta.url)){
 const app=createGameServer();app.ready.then(address=>console.log(`Tactical Zero: http://localhost:${address.port} (HTTP + WebSocket, 2–4 players)`));
 for(const signal of ['SIGTERM','SIGINT'])process.on(signal,()=>app.close().then(()=>process.exit(0)));
}
