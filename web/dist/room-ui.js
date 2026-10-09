'use strict';
const RoomUI = {
  fields:['maxHp','scoreLimit','matchSeconds','respawnSeconds','supplySeconds','npcCount','startingWeapon','victory'],
  defaults:{maxHp:100,scoreLimit:15,matchSeconds:600,respawnSeconds:3,supplySeconds:20,npcCount:3,startingWeapon:'M4A1',victory:'kills'},
  signature:'',combatUntil:0,supplyUntil:0,
  read(prefix='createRule') {
    const s={};for(const key of this.fields){const value=$(prefix+key).value;s[key]=['startingWeapon','victory'].includes(key)?value||this.defaults[key]:value===''?this.defaults[key]:Number(value)}return s;
  },
  lobby(s) {
    $('roomRulesSummary').textContent=s.rulesSummary||'';
    $('roomRuleEditor').hidden=s.owner!==TZNet.id||!['lobby','finished'].includes(s.status);
    const signature=s.code+JSON.stringify(s.settings);
    if(signature!==this.signature&&s.settings){this.signature=signature;for(const key of this.fields)$('lobbyRule'+key).value=String(s.settings[key])}
  },
  show(id,text,kind='kill') {const e=$(id);e.textContent=text;e.dataset.kind=kind;e.hidden=false;},
  sync(s,previous) {
    if(!['playing','finished'].includes(s.status))return;
    const fresh=(key)=>s.self[key]&&s.time-s.self[key].at<3&&s.self[key].seq!==previous?.self?.[key]?.seq;
    if(fresh('hitEvent')){hitTime=.2;hitHead=false;sound('hit',.55)}
    if(fresh('combatEvent')){const e=s.self.combatEvent;this.show('combatNotice',e.text,e.kind);this.combatUntil=s.time+2.4;killConfirm=.45;sound('kill',1)}
    if(fresh('rewardEvent')){this.show('resupplyNotice',s.self.rewardEvent.text,'supply');this.supplyUntil=s.time+3;sound('pickup',.65)}
    if(previous?.round===s.round&&previous.self.hp<=0&&s.self.hp>0){this.show('combatNotice','已复活 · 出生保护已开启','respawn');this.combatUntil=s.time+2.2;sound('success',.7)}
    $('combatNotice').hidden=s.self.hp<=0||s.time>this.combatUntil;
    $('resupplyNotice').hidden=s.self.hp<=0||s.time>this.supplyUntil;
    $('ammoWarning').hidden=s.self.hp<=0||s.self.reserve>0;
    $('ammoWarning').textContent=W[s.self.weapon]?.limited?'限定武器弹药不可补充 · 可切换常规武器':s.self.ammo>0?'备弹耗尽 · 搜索补给点或击杀敌人补弹':'弹药耗尽 · 切换手枪，或寻找补给';
  },
  reset(){this.signature='';this.combatUntil=this.supplyUntil=0;for(const id of ['combatNotice','resupplyNotice','ammoWarning'])$(id).hidden=true},
};
$('saveRoomRules').addEventListener('click',()=>{TZNet.send({type:'settings',settings:RoomUI.read('lobbyRule')});$('netStatus').textContent='正在保存房间规则…'});
