'use strict';
const DeathView={
 active:false,playedSolo:false,record:null,elapsed:0,remaining:0,mode:'solo',
 clear(){this.active=false;this.record=null;this.elapsed=0;$('deathHUD').hidden=true;document.body.classList.remove('death-view')},
 reset(){this.clear();this.playedSolo=false},
 text(d){if(!d)return '死亡原因未知';const weapon=W[d.weapon]?.label||'未知武器',cause={self:'自身爆炸误伤',explosion:'爆炸',finish:'补枪淘汰',bleedout:'倒地后未获救援',bullet:d.headshot?'爆头':'射击'}[d.reason]||'致命命中';return `${d.killerName} · ${weapon} · ${cause}`},
 start(d,mode='solo',remaining=3.2){this.record=d;this.mode=mode;this.active=true;this.elapsed=0;this.remaining=remaining;clearInput();damageFlash=0;shake=0;document.body.classList.add('death-view');$('deathHUD').hidden=false;$('deathTitle').textContent=d.reason==='self'?'爆炸误伤':'被 '+d.killerName+' 击杀';$('deathDetail').textContent=this.text(d)+(d.damage?' · '+d.damage+' 致命伤害':'');$('deathSkip').hidden=mode==='multi';this.hud();if(mode!=='solo')sound('death',1)},
 solo(){if(this.playedSolo||!player?.death)return false;this.playedSolo=true;this.start(player.death);sound('death',.6);return true},
 sync(s){const d=s.self.death;if(!['playing','finished'].includes(s.status)||s.self.hp>0||s.self.downed||!d){this.clear();return}const mode=s.status==='finished'?'final':'multi';if(!this.active||this.record?.id!==d.id||this.mode!==mode)this.start(d,mode,mode==='final'?3.2:Math.max(0,s.self.respawnAt-s.time));this.remaining=mode==='final'?Math.max(0,3.2-this.elapsed):Math.max(0,s.self.respawnAt-s.time);this.hud()},
 tick(dt){if(!this.active)return;this.elapsed+=dt;if(this.mode!=='multi'){this.remaining=Math.max(0,3.2-this.elapsed);if(this.remaining<=0){this.skip();return}}this.hud()},
 hud(){if(!this.record)return;const source=this.record.attacker;$('deathCameraLabel').textContent=source?'死亡现场 · 开火位置定格':'死亡现场 · 未知攻击来源';$('deathTimer').textContent=this.mode==='multi'?'自动复活倒计时 · '+Math.max(0,Math.ceil(this.remaining))+' 秒':Math.ceil(this.remaining)+' 秒后查看结算'},
 skip(){if(!this.active||this.mode==='multi')return;const mode=this.mode;this.clear();if(mode==='solo')finish(false);else{$('overlay').classList.remove('hidden');needsRender=true}},
 draw(){if(!this.active||!this.record||!mapCanvas)return false;const d=this.record,v=d.victim,a=d.attacker||v,g=ctx,t=Math.min(1,this.elapsed/.8),ease=t*t*(3-2*t),cx=v.x+(a.x-v.x)*.5*ease,cy=v.y+(a.y-v.y)*.5*ease,fit=Math.min(baseZoom*1.3,Math.max(180,width-70)/(Math.abs(a.x-v.x)+200),Math.max(160,height-170)/(Math.abs(a.y-v.y)+180)),zoom=baseZoom+(Math.max(.2,fit)-baseZoom)*ease;
  g.setTransform(dpr,0,0,dpr,0,0);g.fillStyle='#09141e';g.fillRect(0,0,width,height);g.save();g.translate(width/2,height*.48);g.scale(zoom,zoom);g.translate(-cx,-cy);g.drawImage(mapCanvas,0,0);g.fillStyle='#07132388';g.fillRect(0,0,WW,WH);
  if(d.attacker){const impact=d.impact||v;g.strokeStyle='#ffc194';g.lineWidth=2/zoom;g.setLineDash([9/zoom,6/zoom]);g.beginPath();g.moveTo(a.x,a.y);g.lineTo(impact.x,impact.y);g.stroke();g.setLineDash([]);if(d.impact){g.strokeStyle='#ffbc7180';g.fillStyle='#ffad5820';g.beginPath();g.arc(d.impact.x,d.impact.y,Math.max(1,d.blastRadius),0,TAU);g.fill();g.stroke()}}
  if(d.attacker&&d.killerId!==v.id){Art.actor({...a,hp:100,moving:0,reload:0,flash:0,weapon:W[a.weapon]?a.weapon:'USP'},false,false);this.marker(g,a,'开火位置','#ffb39d',zoom)}
  Art.actor({...v,hp:0,moving:0,reload:0,flash:0,weapon:W[v.weapon]?v.weapon:'USP'},true,true);this.marker(g,v,'你的倒地点','#9ae7e0',zoom);g.restore();return true;
 },
 marker(g,p,label,color,zoom){g.strokeStyle=color;g.lineWidth=2/zoom;g.beginPath();g.arc(p.x,p.y,32,0,TAU);g.stroke();g.font='bold '+(14/zoom)+'px system-ui';g.textAlign='center';g.fillStyle=color;g.fillText(label,p.x,p.y-40-8/zoom)}
};
$('deathSkip').addEventListener('click',()=>DeathView.skip());
