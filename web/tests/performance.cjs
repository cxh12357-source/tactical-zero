// CPU simulation comparison only; Canvas is mocked, so this is NOT a GPU/FPS test.
const fs=require('node:fs'),{performance}=require('node:perf_hooks'),{harness,code}=require('./harness.cjs');
const baseline=process.argv[2];
function measure(source){
 const e=harness({},source);
 e.run(`selectedDifficulty='hell';reset();if(typeof eventState!=='undefined')eventState.next=1e9;spawnTimer=itemTimer=1e9;player.shieldUntil=player.infiniteUntil=1e9;const positions=[];for(let y=1;y<ROWS-1;y++)for(let x=1;x<COLS-1;x++){const p={x:(x+.5)*TILE,y:(y+.5)*TILE};if(!blocked(x,y)&&dist(p,player)>600)positions.push(p)}enemies.forEach((e,i)=>{e.x=positions[i*7].x;e.y=positions[i*7].y;e.weapon='MP6';e.ammo=35;e.think=0;e.repath=.3;e.cooldown=.5;e.reaction=.5;e.patrol={x:player.x,y:player.y}});`);
 const start=performance.now();e.run(`for(let i=0;i<3600;i++){update(STEP);render()}`);return performance.now()-start;
}
const before=baseline?fs.readFileSync(baseline,'utf8').match(/<script>([\s\S]*?)<\/script>/)[1]:null;
const median=v=>v.sort((a,b)=>a-b)[Math.floor(v.length/2)];
measure(code);if(before)measure(before);
const current=[],old=[];for(let i=0;i<3;i++){if(before)old.push(measure(before));current.push(measure(code))}
console.log(JSON.stringify({unit:'milliseconds / 3600 simulation+mock-render steps',current:current.map(Math.round),baseline:old.map(Math.round),medianCurrent:Math.round(median(current)),medianBaseline:old.length?Math.round(median(old)):null,notice:'No GPU or real-device FPS measurement.'},null,2));
