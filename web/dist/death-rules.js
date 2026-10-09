/* Minimal immutable fatal-hit data; never includes inventory, input, tokens or live tracking. */
(function(root,factory){const api=factory();if(typeof module==='object')module.exports=api;else root.TZDeath=api})(typeof globalThis==='object'?globalThis:this,function(){
 const point=a=>({x:Number.isFinite(a?.x)?a.x:0,y:Number.isFinite(a?.y)?a.y:0});
 function source(a,weapon){if(!a)return null;return {...point(a),id:a.id||a.opsId||null,name:a.name||(a.id==='solo'?'你':a.elite?'精英掠夺者':'掠夺者'),classId:a.classId||null,angle:a.angle||0,weapon:weapon||a.weapon||'USP',elite:!!a.elite}}
 function record(victim,b,attacker,at,amount,reason){const who=b.source||source(attacker,b.weapon),self=!!who?.id&&who.id===victim.id;return {id:String(at)+':'+String((victim.deaths||0)+1),at,killerId:who?.id||null,killerName:self?'你自己':who?.name||'未知来源',weapon:b.weapon||null,reason:self?'self':reason||'bullet',headshot:!!b.headshot,damage:Math.round(Math.max(0,amount||0)),attacker:who?{...who}:null,victim:{...source(victim,victim.weapon||victim[victim.slot]?.type),name:'你'},impact:b.impact?point(b.impact):null,blastRadius:b.blastRadius||0}}
 function brief(d){if(!d)return null;const {killerId,killerName,weapon,reason,damage,headshot}=d;return {killerId,killerName,weapon,reason,damage,headshot}}
 return {source,record,brief};
});
