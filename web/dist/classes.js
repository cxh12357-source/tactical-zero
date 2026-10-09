/* Shared profession rules. PVP executes these only on the authoritative server. */
(function(root,factory){const api=factory(typeof module==='object'?require('./world.js'):root.TZWorld);if(typeof module==='object')module.exports=api;else root.TZClasses=api})(typeof globalThis==='object'?globalThis:this,function(R){
'use strict';
const ROLES=Object.freeze({
 assault:{name:'先锋',tag:'突击',mark:'尖',color:'#ffb078',skill:'疾速推进',cooldown:22,duration:5,detail:'移动速度 +30%，持续 5 秒。依然会受伤，适合绕侧和脱离交火。'},
 scout:{name:'侦察兵',tag:'侦察',mark:'侦',color:'#76d9ef',skill:'脉冲探测',cooldown:28,duration:6,detail:'6 秒内提示 480 范围敌人的大致方位。可以隔墙探测，不显示位置或穿墙瞄准。'},
 medic:{name:'医疗兵',tag:'支援',mark:'医',color:'#97edb8',skill:'急救脉冲',cooldown:32,duration:0,detail:'自己与 160 范围内、无遮挡的队友恢复 30 生命。不复活倒地者。'},
 engineer:{name:'工程师',tag:'防护',mark:'工',color:'#ffd279',skill:'装甲加固',cooldown:32,duration:0,detail:'自己与 130 范围内、无遮挡的队友获得 35 临时护甲，最多保留 10 秒，不叠加。'},
 guardian:{name:'重装卫士',tag:'防守',mark:'盾',color:'#a6b9fa',skill:'坚守阵地',cooldown:28,duration:5,detail:'受到的伤害减少 45%，持续 5 秒；期间移动速度降低 20%。'},
 marksman:{name:'神射手',tag:'远射',mark:'狙',color:'#e6b0ec',skill:'专注射击',cooldown:24,duration:6,detail:'基础射击散射减少 65%，持续 6 秒；移动速度降低 35%。不改变瞄准方向或弹药。'},
 infiltrator:{name:'渗透者',tag:'潜行',mark:'隐',color:'#a7d594',skill:'无声潜行',cooldown:26,duration:6,detail:'消除脚步声并提升 15% 移速，持续 6 秒。开火立即结束；仍能被视野和雷达发现。'},
 supplier:{name:'补给官',tag:'后勤',mark:'补',color:'#e9c59e',skill:'弹药支援',cooldown:38,duration:0,detail:'自己与 160 范围内、无遮挡的队友，当前常规武器增加一个基础弹匣的备弹。不补空投枪或 AWP。'}
});
const valid=id=>typeof id==='string'&&Object.hasOwn(ROLES,id);
function init(p,id='assault',now=0,preserve=false){const ready=preserve?Math.max(now,p.skillReadyAt||0):now;Object.assign(p,{classId:valid(id)?id:'assault',skillReadyAt:ready,skillUntil:0,skillCastAt:-100,armor:0,armorUntil:0})}
const alive=p=>p.hp>0&&!p.downed&&p.connected!==false;
const active=(p,t)=>alive(p)&&(p.skillUntil||0)>t;
function speed(p,t){if(!active(p,t))return 1;return ({assault:1.3,guardian:.8,marksman:.65,infiltrator:1.15})[p.classId]||1}
const spread=(p,t)=>active(p,t)&&p.classId==='marksman'?.35:1;
const silent=(p,t)=>active(p,t)&&p.classId==='infiltrator';
function shot(p,t){if(silent(p,t))p.skillUntil=t}
function absorb(p,amount,t){if(active(p,t)&&p.classId==='guardian')amount*=.55;const armor=p.armorUntil>t?p.armor||0:0,used=Math.min(armor,amount);p.armor=Math.max(0,armor-used);return amount-used}
function state(p,t){return {classId:valid(p.classId)?p.classId:'assault',skillReadyAt:p.skillReadyAt||0,skillUntil:alive(p)?p.skillUntil||0:0,skillCastAt:p.skillCastAt??-100,armor:alive(p)&&p.armorUntil>t?p.armor||0:0,armorUntil:p.armorUntil||0}}
function cast(p,a,t){if(!alive(p)||!valid(p.classId)||(p.skillReadyAt||0)>t)return {ok:false,reason:'技能尚未就绪'};const role=ROLES[p.classId],friends=a.actors().filter(x=>alive(x)&&(x===p||a.allied(p,x))&&R.distance(x,p)<(p.classId==='engineer'?130:160)&&a.world.los(p,x));let changed=0;
 if(p.classId==='medic'){for(const x of friends){const hp=Math.min(x.maxHp||100,x.hp+30);changed+=hp-x.hp;x.hp=hp}}
 else if(p.classId==='engineer'){for(const x of friends){x.armor=Math.max(x.armorUntil>t?x.armor||0:0,35);x.armorUntil=t+10;changed++}}
 else if(p.classId==='supplier'){for(const x of friends){const gun=a.gun(x),w=R.W[gun?.type];if(!w||w.limited)continue;const n=Math.min(w.mag,Math.max(0,(x.maxHp?w.reserve*2:w.reserve)-gun.reserve));gun.reserve+=n;changed+=n}}
 else changed=1;
 if(!changed)return {ok:false,reason:p.classId==='medic'?'附近没有需要治疗的存活友军':'附近常规武器备弹已满，限定武器不能补给'};
 p.skillReadyAt=t+role.cooldown;p.skillUntil=t+role.duration;p.skillCastAt=t;p.task=null;p.searchId=null;p.searchProgress=0;
 if(p.classId!=='infiltrator')a.noise?.(p,'skill',360);
 return {ok:true,reason:role.skill+'已启动'};
}
function signals(p,actors,allied,t){if(!active(p,t)||p.classId!=='scout')return [];return actors.filter(x=>x!==p&&alive(x)&&!allied(p,x)&&R.distance(p,x)<480).slice(0,8).map(x=>({id:'scan'+(x.id||x.opsId)+Math.floor(t),kind:'radar',direction:Math.round(Math.atan2(x.y-p.y,x.x-p.x)/(Math.PI/4))*Math.PI/4}))}
return {ROLES,valid,init,active,speed,spread,silent,shot,absorb,state,cast,signals};
});
