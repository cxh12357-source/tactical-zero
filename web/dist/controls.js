'use strict';
// HUD actions retain their original nodes/listeners; decoration never replaces their text updates.
const ControlUI={
 names:{moveStick:'移动摇杆',aimStick:'开火摇杆',fullscreenButton:'全屏',pause:'暂停',settingsButton:'设置',bagButton:'背包',classSkill:'职业技能',switchWeapon:'切换武器',reload:'换弹',modToggle:'选择配件',installMod:'安装配件',searchButton:'搜索 / 交互',pickupWeapon:'拾取武器',continueBattle:'继续冒险'},
 icons:{move:'M12 3v18M3 12h18M8 7l4-4 4 4M8 17l4 4 4-4M7 8l-4 4 4 4M17 8l4 4-4 4',aim:'M12 2v4M12 18v4M2 12h4M18 12h4M12 7a5 5 0 1 0 0 10 5 5 0 0 0 0-10',full:'M8 3H3v5M16 3h5v5M3 16v5h5M21 16v5h-5',pause:'M8 5v14M16 5v14',settings:'M9 3h6l1 3 3 1 2 5-2 5-3 1-1 3H9l-1-3-3-1-2-5 2-5 3-1zM12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8',bag:'M7 7V5a5 5 0 0 1 10 0v2M5 7h14v14H5zM8 13h8v5H8z',skill:'M13 2 5 14h6l-1 8 9-13h-7z',switch:'M4 7h16l-4-4M20 17H4l4 4M20 7l-4 4M4 17l4-4',reload:'M20 10a8 8 0 1 0-2 8M20 3v7h-7',mod:'M4 5h16M4 12h16M4 19h16M8 2v6M16 9v6M10 16v6',install:'M14 3a6 6 0 0 0-7 8L2 16a3 3 0 0 0 6 6l5-5a6 6 0 0 0 8-7l-4 4-4-4z',search:'M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14M15 15l7 7',pickup:'M12 2v12M7 9l5 5 5-5M4 15v6h16v-6',next:'M5 4l9 8-9 8M17 4v16',medic:'M9 3h6v6h6v6h-6v6H9v-6H3V9h6z',shield:'M12 2 3 6v6c0 5 9 10 9 10s9-5 9-10V6z',scout:'M12 3v9l6 3M12 3a9 9 0 1 0 9 9M16 3l5 5',engineer:'M5 3h14v18H5zM9 7h6M9 12h6M9 17h6',sneak:'M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6'},
 labels:{fullscreenButton:'full',pause:'pause',settingsButton:'settings',bagButton:'bag',classSkill:'skill',switchWeapon:'switch',reload:'reload',modToggle:'mod',installMod:'install',searchButton:'search',pickupWeapon:'pickup',continueBattle:'next'},
 icon(el,name){if(el.dataset.icon===name)return;el.dataset.icon=name;const svg='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="'+this.icons[name]+'"/></svg>';el.style.setProperty('--control-icon','url("data:image/svg+xml,'+encodeURIComponent(svg)+'")')},
 captions:{fullscreenButton:'全屏',pause:'暂停',settingsButton:'设置',bagButton:'背包',classSkill:'技能',switchWeapon:'切枪',reload:'换弹',modToggle:'配件',installMod:'安装',searchButton:'搜索',pickupWeapon:'拾取',continueBattle:'下一关'},
 preset:'photo-3.9',previousLandscape:null,
 metadata(){return{layoutPreset:this.preset,previousLandscape:this.previousLandscape}},
 adoptPhotoLayout(){try{const saved=JSON.parse(localStorage.getItem('tactical-settings-v2')||'{}');this.previousLandscape=saved.previousLandscape||null;if(saved.layoutPreset===this.preset)return;this.previousLandscape=JSON.parse(JSON.stringify(layout.landscape||{}));layout.landscape={};localStorage.setItem('tactical-settings-v2',JSON.stringify({...saved,...this.metadata(),layouts:layout}))}catch(_){layout.landscape={}}},
 selected:'moveStick',drag:null,disabledBefore:{},
 begin(){this.closePopover();for(const id of Object.keys(this.labels)){this.disabledBefore[id]=$(id).disabled;$(id).disabled=false}this.select(this.selected);this.apply()},
 end(){this.drag=null;for(const id of Object.keys(this.labels))$(id).disabled=!!this.disabledBefore[id];this.disabledBefore={};this.closePopover()},
 init(){
  this.adoptPhotoLayout();
  this.icons.bag='M8 6V4h8v2M7 6h10a3 3 0 0 1 3 3v11H4V9a3 3 0 0 1 3-3M8 13h8v5H8zM4 10H2v7h2M20 10h2v7h-2';
  this.icons.skill='M14 3a2 2 0 1 0 0 4 2 2 0 0 0 0-4M6 11l4-3 5 2 4 4M11 9l-2 6 5 2 2 5M9 15l-4 6M4 5H1M4 9H2';
  this.icons.mod='M4 8h16v8H4zM9 8V5h6v3M9 16v3h6v-3M12 9v6M9 12h6';
  $('app').appendChild($('modOptions'));
  for(const [id,name]of Object.entries(this.names)){
   const el=$(id);el.dataset.controlName=name;el.dataset.caption=this.captions[id]||name;
   if(!id.endsWith('Stick')){$('app').appendChild(el);el.classList.add('iconControl');if(id!=='fullscreenButton')el.classList.add('combatControl');this.icon(el,this.labels[id]);el.setAttribute('aria-label',name);el.title=name;this.bind(id)}
   const option=document.createElement('option');option.value=id;option.textContent=name;$('layoutControl').appendChild(option);
  }
  for(const [id,name]of [['moveStick','move'],['aimStick','aim']]){$(id).querySelector('.stickLabel').textContent=id==='moveStick'?'移动':'按住开火';this.icon($(id).querySelector('.stickLabel'),name);$(id).classList.add('iconStick');$(id).addEventListener('pointerdown',()=>{if(editing)this.select(id)},true);$(id).addEventListener('pointermove',()=>{if(editing)this.select(id)})}
  $('layoutControl').addEventListener('change',e=>{this.select(e.target.value)});
  for(const axis of ['X','Y'])$('control'+axis).addEventListener('input',e=>{const pos=this.current(this.selected);pos[axis.toLowerCase()]=Number(e.target.value)/100*(axis==='X'?width:height);this.move(this.selected,pos.x,pos.y);this.select(this.selected)});
  $('modToggle').addEventListener('click',()=>{const open=$('modOptions').hidden;$('modOptions').hidden=!open;$('modToggle').setAttribute('aria-expanded',String(open));this.positionPopover()});
  $('modClose').addEventListener('click',()=>this.closePopover());
  this.closePopover();this.apply();this.sync();
 },
 size(id){return id.endsWith('Stick')?{w:stickSize,h:stickSize}:{w:52,h:56}},
 bound(id,x,y){const {w,h}=this.size(id);return{x:clamp(x,safeArea.left+w/2+4,Math.max(safeArea.left+w/2+4,width-safeArea.right-w/2-4)),y:clamp(y,safeArea.top+h/2+4,Math.max(safeArea.top+h/2+4,height-safeArea.bottom-h/2-4))}},
 defaults(id){if(width>height){const photo={moveStick:[.164,.731],aimStick:[.815,.744],fullscreenButton:[.434,.078],pause:[.5,.078],settingsButton:[.566,.078],bagButton:[.338,.878],classSkill:[.845,.433],switchWeapon:[.757,.426],reload:[.656,.853],modToggle:[.467,.866],installMod:[.533,.866],searchButton:[.69,.58],pickupWeapon:[.61,.58],continueBattle:[.53,.58]};return{x:width*photo[id][0],y:height*photo[id][1]}}const l=safeArea.left,r=width-safeArea.right,b=height-safeArea.bottom,s=stickSize/2;return{
  moveStick:{x:l+s+8,y:b-s-8},aimStick:{x:r-s-8,y:b-s-8},fullscreenButton:{x:width/2-56,y:safeArea.top+30},pause:{x:width/2,y:safeArea.top+30},settingsButton:{x:width/2+56,y:safeArea.top+30},
  bagButton:{x:l+(height<350?88:32),y:Math.min(168,height*.43)},classSkill:{x:l+32,y:b-stickSize-44},switchWeapon:{x:r-88,y:b-stickSize-44},reload:{x:r-32,y:b-stickSize-44},modToggle:{x:width/2-28,y:b-32},installMod:{x:width/2+28,y:b-32},searchButton:{x:width/2+60,y:b-92},pickupWeapon:{x:width/2,y:b-92},continueBattle:{x:width/2-60,y:b-92}
 }[id]},
 current(id){const p=layout[orientation()]?.[id];return p&&Number.isFinite(p.x)&&Number.isFinite(p.y)?this.bound(id,p.x*width,p.y*height):this.bound(id,this.defaults(id).x,this.defaults(id).y)},
 place(id,p){const el=$(id),{w,h}=this.size(id);el.style.left=(p.x-w/2)+'px';el.style.top=(p.y-h/2)+'px';el.style.right=el.style.bottom='auto'},
 move(id,x,y){const p=this.bound(id,x,y);layout[orientation()]||={};layout[orientation()][id]={x:p.x/width,y:p.y/height};this.place(id,p);needsRender=true;return p},
 apply(){for(const id of Object.keys(this.names))this.place(id,this.current(id));this.positionPopover();if(editing)this.select(this.selected)},
 select(id){if(!Object.hasOwn(this.names,id))return;this.selected=id;$('layoutControl').value=id;const p=this.current(id);$('controlX').value=Math.round(p.x/width*100);$('controlY').value=Math.round(p.y/height*100);$('controlXValue').textContent=$('controlX').value+'%';$('controlYValue').textContent=$('controlY').value+'%';for(const k of Object.keys(this.names))$(k).classList.toggle('layoutSelected',k===id)},
 bind(id){const el=$(id);el.addEventListener('pointerdown',e=>{if(!editing)return;e.preventDefault();e.stopImmediatePropagation();const r=el.getBoundingClientRect();this.drag={id,pointer:e.pointerId,dx:e.clientX-r.left-r.width/2,dy:e.clientY-r.top-r.height/2};this.select(id);el.setPointerCapture(e.pointerId)},true);
  el.addEventListener('pointermove',e=>{const d=this.drag;if(!editing||d?.id!==id||d.pointer!==e.pointerId)return;e.preventDefault();e.stopImmediatePropagation();const r=$('app').getBoundingClientRect();this.move(id,e.clientX-r.left-d.dx,e.clientY-r.top-d.dy);this.select(id)},true);
  for(const event of ['pointerup','pointercancel','lostpointercapture'])el.addEventListener(event,e=>{if(this.drag?.pointer===e.pointerId)this.drag=null;if(editing){e.preventDefault();e.stopImmediatePropagation()}},true);
  el.addEventListener('click',e=>{if(editing){e.preventDefault();e.stopImmediatePropagation()}},true);
 },
 closePopover(){$('modOptions').hidden=true;$('modToggle').setAttribute('aria-expanded','false')},
 positionPopover(){const p=this.current('modToggle');$('modOptions').style.left=clamp(p.x-120,safeArea.left+8,Math.max(safeArea.left+8,width-safeArea.right-248))+'px';$('modOptions').style.top=clamp(p.y-156,safeArea.top+8,Math.max(safeArea.top+8,height-safeArea.bottom-160))+'px'},
 sync(){
  for(const id of Object.keys(this.labels)){const el=$(id),label=(id==='modToggle'?'选择配件':el.textContent)||this.names[id];el.setAttribute('aria-label',label);el.title=label}
  if(!player)return;
  const roles={assault:'skill',scout:'scout',medic:'medic',engineer:'install',guardian:'shield',marksman:'aim',infiltrator:'sneak',supplier:'engineer'};this.icon($('classSkill'),roles[player.classId]||'skill');
  $('classSkill').dataset.caption={assault:'疾跑',scout:'探测',medic:'治疗',engineer:'护甲',guardian:'减伤',marksman:'专注',infiltrator:'潜行',supplier:'补弹'}[player.classId]||'技能';
  const search=$('searchButton').textContent||'';$('searchButton').dataset.caption=/取消|搜索中/.test(search)?'搜索中':/复活|救援/.test(search)?'救援':/拾取/.test(search)?'拾取':/搜索|撬锁/.test(search)?'搜索':'交互';
  $('classSkill').dataset.badge=Math.max(0,Math.ceil((player.skillReadyAt||0)-time))||'';
  $('bagButton').dataset.badge=String(Expedition.bag.length);$('installMod').dataset.badge=String(player.modStock||0);
  $('reload').dataset.badge=player.reload>0?player.reload.toFixed(1):'';
  const active=player.reload>0||player.weapon==='AWP'&&player.cooldown>.15||player.scoped;$('weaponStatus').hidden=!active;
  $('weaponName').title=W[player.weapon].desc;$('bagButton').hidden=gameMode==='multi';
  $('modToggle').hidden=$('installMod').hidden=!running||player.hp<=0;
  $('continueBattle').style.display=gameMode==='solo'&&roundPhase==='extract'&&!!extraction?'block':'none';
 }
};
ControlUI.init();
