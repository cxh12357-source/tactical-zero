'use strict';
// The existing solo loop owns combat. This module adds the small search/extract loop.
function setModeUI(){
 document.body.classList.toggle('multiplayer',gameMode==='multi');
 const label=gameMode==='solo'?'按住开火 · 自动瞄准':'拖动瞄准 · 松手停火';
 $('aimStick').querySelector('.stickLabel').textContent=label;
 $('aimStick').setAttribute?.('aria-label',label);
 $('pause').textContent=gameMode==='solo'?'Ⅱ':'菜单';
}
const Expedition={
 containers:[],bag:[],searching:null,damageAtSearch:0,settled:false,stored:0,storageOK:true,
 reset(){this.containers=gameMode==='solo'?worldGeometry.pois():[];
  // Two searchable caches per zone create an actual choice: 16 possible items, eight slots.
  for(const zone of [...this.containers]){const p=worldGeometry.floor.filter(p=>this.containers.every(c=>dist(p,c)>96)).sort((a,b)=>dist(a,zone)-dist(b,zone))[0];if(p)this.containers.push({...zone,...p,id:zone.id+'b',name:zone.name+'储藏箱',opened:false,progress:0})}
  this.bag=[];this.searching=null;this.settled=false;this.load();this.hud()},
 load(){this.stored=0;try{const n=JSON.parse(localStorage.getItem('tactical-stash-v1')||'{}').value;if(Number.isFinite(n)&&n>=0)this.stored=Math.min(n,1e9)}catch(_){}},
 value(){return (typeof FieldOps!=='undefined'?FieldOps.bonus:0)+this.bag.reduce((sum,type)=>sum+TZWorld.LOOT[type].value,0)},
 nearby(){return this.containers.filter(c=>!c.opened&&dist(c,player)<64&&los(player,c)).sort((a,b)=>dist(a,player)-dist(b,player))[0]||null},
 search(){if(!running||paused||gameMode!=='solo')return;const c=this.nearby();if(!c)return;if(this.searching===c){this.searching=null;c.progress=0;return}if(this.bag.length>=TZWorld.CONFIG.bagSlots){toast('背包已满 · 按 B 或点背包整理');return}this.searching=c;c.progress=0;this.damageAtSearch=stats.damageTaken;sound('reload',.4)},
 update(dt){if(gameMode!=='solo'||!this.searching)return;const c=this.searching;const moving=Math.hypot(sticks.move.x,sticks.move.y)>.12||keys.KeyW||keys.KeyA||keys.KeyS||keys.KeyD||keys.ArrowUp||keys.ArrowDown||keys.ArrowLeft||keys.ArrowRight;
  if(paused||dist(c,player)>=64||moving||mouse.down||sticks.aim.id!==null||stats.damageTaken!==this.damageAtSearch){c.progress=0;this.searching=null;return}c.progress+=dt;if(c.type==='tech'&&FieldOps.ops&&(c.noiseAt||0)<=time){c.noiseAt=time+.6;FieldOps.ops.emit(player,'lock',520)}if(c.progress>=(c.type==='tech'?3.6:TZWorld.CONFIG.searchSeconds)){this.open(c);this.searching=null}
 },
 open(c){if(c.opened||this.bag.length>=TZWorld.CONFIG.bagSlots)return false;c.opened=true;c.progress=0;const contents={med:['med','scrap'],arms:['scrap','intel'],tools:['scrap','scrap'],tech:['circuit','intel']}[c.type];
  this.bag.push(...contents.slice(0,TZWorld.CONFIG.bagSlots-this.bag.length));
  if(c.type==='med'){const heal=Math.min(35,100-player.hp);player.hp+=heal;stats.healed+=heal}
  if(c.type==='tools'){replenishAmmo();player.modStock=Math.min(3,(player.modStock||0)+1)}
  if(c.type==='arms'&&crates.length+airdrops.length<CONFIG.loot.crates)crates.push({x:c.x,y:c.y,type:TZWorld.rollWeapon(),phase:0});
  sound('pickup');emit(c.x,c.y,12,['#e7c779','#8bd0b4'],80,.4);toast('搜索完成 · '+contents.map(x=>TZWorld.LOOT[x].name).join(' / '),2.5);this.hud();return true;
 },
 finish(won){if(this.settled)return;this.settled=true;this.storageOK=true;if(won){this.stored=Math.min(1e9,this.stored+this.value());try{localStorage.setItem('tactical-stash-v1',JSON.stringify({version:1,value:this.stored}))}catch(_){this.storageOK=false}}},
 showBag(){if(gameMode!=='solo'||!running)return;clearInput();paused=true;this.searching=null;document.body.classList.remove('playing');$('overlay').dataset.screen='bag';$('overlay').classList.remove('hidden');this.bagUI()},
 bagUI(){const box=$('bagItems');box.innerHTML='';this.bag.forEach((type,i)=>{const def=TZWorld.LOOT[type],button=document.createElement('button');button.className='lootSlot';button.textContent=def.name+' · '+def.value+' ◈ · 丢弃';button.addEventListener('click',()=>{this.bag.splice(i,1);this.bagUI()});box.appendChild(button)});$('bagValue').textContent=`${this.bag.length} / ${TZWorld.CONFIG.bagSlots} 格 · 总值 ${this.value()} ◈`},
 closeBag(){if(!running)return;paused=false;clearInput();document.body.classList.add('playing');$('overlay').classList.add('hidden');this.hud();last=performance.now()},
 hud(){if(!player||gameMode!=='solo')return;const c=this.nearby(),button=$('searchButton');button.style.display=c?'block':'none';button.textContent=this.searching?`搜索中 ${Math.round(this.searching.progress/(this.searching.type==='tech'?3.6:TZWorld.CONFIG.searchSeconds)*100)}% · 再点取消`:(c?.type==='tech'?'撬锁（会暴露声音） ':'搜索 ')+(c?.name||'')+' [E]';$('bagButton').textContent=`背包 ${this.bag.length} / 8 · ${this.value()} ◈`;
  let closest=null,d=Infinity;for(const poi of this.containers){const n=dist(player,poi);if(n<d){d=n;closest=poi}}$('zoneLabel').textContent=closest?`${closest.name} · ${d<190?'已抵达':Math.round(d/64)+' 格'}`:activeMap.name;
 },
 draw(){const list=gameMode==='multi'?(typeof TZNet!=='undefined'?TZNet.containers:[]):this.containers;for(const c of list){if(!onScreen(c.x,c.y)||!worldGeometry.revealed(player,c,time,false))continue;const color={arms:'#e3b56c',med:'#9bdda3',tools:'#c6c69b',tech:'#7acacb',special:'#d4acff'}[c.kind||c.type]||'#c6c69b';ctx.save();ctx.translate(c.x,c.y);ctx.globalAlpha=c.opened?.38:1;ctx.fillStyle='#152926';ctx.fillRect(-25,-19,50,38);ctx.strokeStyle=color;ctx.lineWidth=3;ctx.strokeRect(-25,-19,50,38);ctx.fillStyle=color;ctx.fillRect(-20,-14,40,6);ctx.font='bold 14px system-ui';ctx.textAlign='center';ctx.fillText(c.opened?'已搜索':c.name,0,-31);ctx.font='bold 21px monospace';ctx.fillText({arms:'A',med:'+',tools:'⚙',tech:'◆',special:'★'}[c.kind||c.type]||'□',0,11);if(c.progress){ctx.fillRect(-25,26,50*Math.min(1,c.progress/((c.kind||c.type)==='tech'?3.6:TZWorld.CONFIG.searchSeconds)),4)}ctx.restore()}},
 minimap(m,s){FieldOps.minimap(m,s);for(const c of (gameMode==='solo'?this.containers:worldGeometry.pois())){m.fillStyle=c.opened?'#66796a':'#85c9c1';m.fillRect(c.x*s-2,c.y*s-2,4,4)}}
};
$('searchButton').addEventListener('pointerdown',e=>{e.preventDefault();if(gameMode==='multi')TZNet.command('interact');else if(!FieldOps.interact())Expedition.search()});
$('bagButton').addEventListener('click',()=>Expedition.showBag());$('bagClose').addEventListener('click',()=>Expedition.closeBag());
$('homeStash').addEventListener('click',()=>{Expedition.load();$('stashValue').textContent=Expedition.stored+' ◈';showScreen('stash')});$('stashBack').addEventListener('click',()=>showScreen('home'));
