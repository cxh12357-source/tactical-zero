(function(root){
'use strict';
const keys=['tactical-career-v2','tactical-stash-v1','tactical-settings-v2','tactical-map-v1','tactical-difficulty','tactical-class','tactical-building-settings-v1','tactical-building-records-v1'];
function validate(raw){
 if(typeof raw!=='string'||raw.length>1000000)throw Error('存档文件过大或无法读取');
 const data=JSON.parse(raw);if(data.format!=='tactical-zero-save'||data.version!==1||!data.values||typeof data.values!=='object'||Array.isArray(data.values))throw Error('这不是战术禁区存档');
 const result={};for(const key of keys){const value=data.values[key];if(value===undefined)continue;if(typeof value!=='string'||value.length>200000)throw Error('存档内容无效');
  if(['tactical-map-v1','tactical-difficulty','tactical-class'].includes(key)){const allowed={'tactical-map-v1':['random','sector07','docks','ruins','outpost'],'tactical-difficulty':['low','medium','high','hell'],'tactical-class':['assault','scout','medic','engineer','guardian','marksman','infiltrator','supplier']};if(!allowed[key].includes(value))throw Error('存档选项无效')}
  else {const parsed=JSON.parse(value);if(!parsed||typeof parsed!=='object'||Array.isArray(parsed)||/"(?:__proto__|constructor|prototype)"\s*:/.test(value))throw Error('存档结构无效')}
  result[key]=value;
 }if(!Object.keys(result).length)throw Error('存档为空');return result;
}
if(typeof module==='object'){module.exports={keys,validate};return}
const panel=document.getElementById('offlinePanel'),status=document.getElementById('offlineStatus');let pending=null;
document.getElementById('homeMultiplayer').addEventListener('click',()=>{panel.hidden=false;status.textContent='存档保存在这台设备上。更新前可导出备份。'});
document.getElementById('offlineClose').addEventListener('click',()=>panel.hidden=true);
document.getElementById('saveExport').addEventListener('click',()=>{
 try{if(typeof CareerStats!=='undefined')CareerStats.save();const values={};for(const key of keys){const value=localStorage.getItem(key);if(value!==null)values[key]=value}
  const text=JSON.stringify({format:'tactical-zero-save',version:1,gameVersion:root.OFFLINE_VERSION,savedAt:new Date().toISOString(),values},null,2);
  if(root.NativeGame?.exportSave){root.NativeGame.exportSave(text);status.textContent='请选择存档保存位置';return}
  const url=URL.createObjectURL(new Blob([text],{type:'application/json'})),a=document.createElement('a');a.href=url;a.download='TacticalZero-save.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),30000);status.textContent='存档备份已生成，请保留下载的 JSON 文件。';
 }catch(_){status.textContent='无法导出，请检查设备存储空间。'}
});
function preview(text){try{pending=validate(text);document.getElementById('saveRestore').hidden=false;status.textContent='已读取存档。恢复将替换这台设备上的游戏进度和设置。'}catch(e){pending=null;document.getElementById('saveRestore').hidden=true;status.textContent=e.message}}
root.previewLocalSave=preview;
document.getElementById('saveImport').addEventListener('change',async e=>{const file=e.target.files[0];if(!file)return;try{preview(await file.text())}catch(_){pending=null;document.getElementById('saveRestore').hidden=true;status.textContent='无法读取这个存档文件，请重新选择。'}});
document.getElementById('saveRestore').addEventListener('click',()=>{
 if(!pending)return;const before={};try{for(const key of keys)before[key]=localStorage.getItem(key);for(const key of keys){if(key in pending)localStorage.setItem(key,pending[key]);else localStorage.removeItem(key)}location.reload()}
 catch(_){try{for(const key of keys){if(before[key]===null)localStorage.removeItem(key);else if(before[key]!==undefined)localStorage.setItem(key,before[key])}}catch(_){}status.textContent='恢复失败，原存档已尽量保留，请检查存储空间。'}
});
})(typeof globalThis==='object'?globalThis:this);
