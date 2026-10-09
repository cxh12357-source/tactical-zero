(function(root){
'use strict';
const WEAPON_DEFS={
 M4A1:{name:'M4A1 突击步枪',category:'突击步枪',damage:30,fireRate:600,magazineSize:30,reserveAmmo:300,reloadTime:1.9,bulletSpeed:2100,range:920,spread:.014,recoil:.012,moveSpeedMultiplier:1,penetration:0,soundRadius:1000,autoAimLockTime:0,falloffStart:600,minDamage:.65,pellets:1},
 USP:{name:'USP 战术手枪',category:'手枪',damage:34,fireRate:250,magazineSize:12,reserveAmmo:180,reloadTime:1.15,bulletSpeed:1750,range:650,spread:.017,recoil:.016,moveSpeedMultiplier:1.18,penetration:0,soundRadius:560,autoAimLockTime:0,falloffStart:380,minDamage:.6,pellets:1},
 RPK:{name:'RPK 支援机枪',category:'轻机枪',damage:29,fireRate:720,magazineSize:60,reserveAmmo:420,reloadTime:3.6,bulletSpeed:2200,range:1000,spread:.021,recoil:.022,moveSpeedMultiplier:.83,penetration:0,soundRadius:1350,autoAimLockTime:0,falloffStart:650,minDamage:.65,pellets:1},
 AWP:{name:'AWP 栓动狙击枪',category:'狙击枪',damage:120,fireRate:42,magazineSize:5,reserveAmmo:65,reloadTime:2.65,bulletSpeed:4200,range:1350,spread:.003,recoil:.05,moveSpeedMultiplier:.91,penetration:0,soundRadius:1450,autoAimLockTime:.38,falloffStart:1000,minDamage:.9,pellets:1},
 VECTOR:{name:'VECTOR 冲锋枪',category:'冲锋枪',damage:22,fireRate:1100,magazineSize:25,reserveAmmo:400,reloadTime:1.4,bulletSpeed:1800,range:660,spread:.028,recoil:.009,moveSpeedMultiplier:1.1,penetration:0,soundRadius:720,autoAimLockTime:0,falloffStart:240,minDamage:.22,pellets:1},
 Shotgun:{name:'M1014 半自动霰弹枪',category:'霰弹枪',damage:17,fireRate:150,magazineSize:6,reserveAmmo:90,reloadTime:2.5,bulletSpeed:1750,range:520,spread:.14,recoil:.035,moveSpeedMultiplier:.96,penetration:0,soundRadius:1100,autoAimLockTime:0,falloffStart:130,minDamage:.12,pellets:8}
};
for(const [id,w]of Object.entries(WEAPON_DEFS))w.id=id;
const DIFFICULTIES={low:{label:'低',perFloor:5,accuracy:.18,reaction:.85,hearing:.75,chase:7,cover:.2,damage:.55,speed:90},medium:{label:'中',perFloor:8,accuracy:.11,reaction:.6,hearing:1,chase:11,cover:.38,damage:.75,speed:106},high:{label:'高',perFloor:11,accuracy:.065,reaction:.38,hearing:1.2,chase:15,cover:.55,damage:.9,speed:118},hell:{label:'地狱',perFloor:14,accuracy:.035,reaction:.24,hearing:1.4,chase:20,cover:.72,damage:1.05,speed:128}};
const descriptions={
 apartment:{name:'海湾公寓',code:'HB',briefing:'双楼梯逐户清剿。主楼梯在西侧，消防楼梯在东侧。清空三层住宅与屋顶后，在屋顶信标坚守 3 秒。',levels:['F1','F2','F3','RF'],themes:['门厅与公共服务','住宅与洗衣房','大户型与设备区','屋顶与消防平台'],names:[['物业室','快递间','门厅','地下车库入口','值班室','住户会客厅','自行车库','后门通道'],['201 客厅','201 卧室','202 厨房','202 卧室','203 客厅','203 卧室','洗衣房','储物间'],['301 主卧','301 起居室','301 餐厨','301 书房','302 起居室','302 卧室','设备间','屋顶入口'],['水箱区','通信机房','电气间','排风机房','维修库','消防水泵','露台花园','直升机物资区']],cuts:[192,512,896,1248,1600],top:480,bottom:736,extract:3},
 office:{name:'凌云商务中心',code:'LY',briefing:'利用双侧安全通道绕过长走廊。关闭的玻璃门仍会阻挡射击与锁定。逐层清空后在四层屋顶通道撤离。',levels:['F1','F2','F3','F4'],themes:['接待大厅','开放办公层','数据与管理层','会议与行政层'],names:[['安保室','前台接待','咖啡区','访客等候','快递中心','财务室','休息室','物业办公室'],['开放办公 A','开放办公 B','会议室 A','会议室 B','打印室','茶水间','项目室','电话间'],['服务器室','网络控制室','档案室','保密资料室','管理办公室','财务经理室','审计室','接待室'],['大型会议 A','大型会议 B','总经理办公室','董事休息室','会议准备室','同声传译室','行政办公室','屋顶通道']],cuts:[192,608,960,1280,1600],top:448,bottom:768,extract:3},
 hospital:{name:'市立急救中心',code:'ER',briefing:'注意隔墙与上下楼的脚步。清剿地下设备区和三层医疗区后，返回一层救护车通道撤离。',levels:['B1','F1','F2','F3'],themes:['地下设备与转运','急诊与检查','住院与护理','手术与无菌区'],names:[['设备区','医用气体站','储藏室','停尸间','物资库','清洗间','备用电源','地下通道'],['急诊大厅','接待分诊','药房','检查室 A','检查室 B','影像室','处置室','救护车接应室'],['病房 201','病房 202','护士站','治疗室','病房 203','病房 204','隔离室','备药室'],['手术室 1','手术室 2','器械室','麻醉准备','术后观察','医生办公室','无菌库','更衣消毒']],cuts:[192,544,896,1248,1600],top:512,bottom:736,extract:1},
 factory:{name:'东港制造厂',code:'EP',briefing:'机器与货架提供掩体。沿两侧钢梯清空夹层和行政层，再返回地面装卸平台。夹层可看见下方静态车间结构，射击与锁定仍严格分层。',levels:['G','M','F2'],themes:['地面生产与物流','夹层巡检步道','行政与调度'],names:[['生产车间 A','生产车间 B','原料仓库','装卸准备区','维修间','传送带区','成品仓库','控制室'],['西侧检修台','传动机房','中段巡检台','东侧检修台','工具库','消防设备','管线检查区','安全休息区'],['行政办公室','生产调度','质量检验室','设计办公室','会议室','员工食堂','更衣室','安全培训室']],cuts:[192,704,1088,1344,1600],top:480,bottom:800,extract:0}
};
function makeMap(id,d){
 const map={id,name:d.name,code:d.code,briefing:d.briefing,worldWidth:1792,worldHeight:1280,playerSpawn:{x:112,y:112,floorId:id==='hospital'?1:0},extractionZone:{id:id+'-extraction',x:id==='factory'?1680:id==='hospital'?112:896,y:id==='factory'?1168:id==='hospital'?112:624,floorId:d.extract,radius:52},floors:[]};
 for(let level=0;level<d.levels.length;level++){
  const f={id:level,level,name:d.levels[level],title:d.themes[level],walls:[],rooms:[],doors:[],windows:[],obstacles:[],decorations:[],stairLinks:[],enemySpawnZones:[],navigationGrid:null};
  const rect=(list,x,y,w,h,kind='wall')=>{const o={id:`${id}/${level}/${list}-${f[list].length}`,x,y,w,h,kind,floorId:level};f[list].push(o);return o};
  rect('walls',48,48,1696,16);rect('walls',48,1216,1696,16);rect('walls',48,64,16,1152);rect('walls',1728,64,16,1152);
  for(let band=0;band<2;band++)for(let col=0;col<4;col++){
   const x=d.cuts[col],end=d.cuts[col+1],y=band?d.bottom:192,bottom=band?1088:d.top,mid=Math.round((x+end)/64)*32,idx=band*4+col;
   const room=rect('rooms',x,y,end-x,bottom-y,'room');room.name=d.names[level][idx];room.material=d.levels[level]==='RF'?'concrete':id==='apartment'?(idx%3?'wood':'tile'):id==='office'?'carpet':id==='hospital'?'tile':'concrete';
   // Two doorways per room connect the central spine to the perimeter safety route.
   for(const edge of [y,bottom]){rect('walls',x,edge,mid-48-x,16);rect('walls',mid+48,edge,end-mid-48,16);const door=rect('doors',mid-48,edge,96,16,id==='office'&&idx%3===1?'glass':'door');door.open=false;door.roomId=room.id}
   rect('walls',x,y,16,bottom-y);if(col===3)rect('walls',end,y,16,bottom-y);
   const window=rect('windows',x+48,band?bottom:y,Math.max(32,mid-112-x),16,'window');
   const cx=x+64,cy=y+64,kind=id==='apartment'?['sofa','bed','counter','desk'][idx%4]:id==='office'?['desk','desk','conference','server'][idx%4]:id==='hospital'?['bed','medical','counter','bed'][idx%4]:['machine','conveyor','rack','console'][idx%4];
   const usage=room.name;
   const furnishing=/卧室|主卧|病房|观察|停尸/.test(usage)?'bed':/餐厨|厨房|茶水|食堂|药房|分诊/.test(usage)?'counter':/服务器|网络|通信|电气|电源/.test(usage)?'server':/库|档案|快递|储藏|储物|器械|备药/.test(usage)?'rack':/会议|培训|会客/.test(usage)?'conference':/办公|物业|值班|管理|财务|书房|审计|前台/.test(usage)?'desk':/客厅|起居|休息|等候|咖啡/.test(usage)?'sofa':/检查|手术|治疗|影像|处置|麻醉|医疗/.test(usage)?'medical':/传送|生产/.test(usage)?'conveyor':/设备|机房|水泵|水箱|排风/.test(usage)?'machine':/花园/.test(usage)?'planter':kind;
   rect('obstacles',cx,cy,Math.min(end-x-144,furnishing==='conference'?160:112),furnishing==='bed'?100:56,furnishing);
   if(end-x>320)rect('obstacles',end-112,bottom-112,64,64,id==='factory'?'rack':'cabinet');
   rect('decorations',x+32,bottom-42,28,20,'case');
   f.enemySpawnZones.push({id:room.id+'/spawn',floorId:level,x:mid,y:bottom-48,radius:24,roomId:room.id});
  }
  for(const [name,x,y,w,h]of [['中央交通厅',208,d.top+32,1376,d.bottom-d.top-32],['西安全通道',64,80,128,1120],['东消防通道',1616,80,112,1120],['后勤连廊',208,1104,1376,96]]){const room=rect('rooms',x,y,w,h,'corridor');room.name=name;room.material=id==='office'?'carpet':'concrete'}
  // Offset central cores leave north/south bypasses with generous turning clearance.
  for(const x of id==='factory'?[640,1152]:[672,1152]){rect('obstacles',x,Math.round((d.top+d.bottom)/64)*32-32,96,64,id==='office'?'elevator':id==='hospital'?'nurse':id==='factory'?'machine':'planter')}
  for(const side of [0,1])for(const direction of [-1,1]){const to=level+direction;if(to<0||to>=d.levels.length)continue;const s={id:`${id}/${level}/stair-${side}-${direction}`,floorId:level,x:side?1680:112,y:direction>0?544:704,radius:35,toFloorId:to,toId:`${id}/${to}/stair-${side}-${-direction}`,exit:{x:side?1680:112,y:direction>0?704:544,floorId:to},label:side?'消防楼梯':'主楼梯'};f.stairLinks.push(s)}
  map.floors.push(f);
 }
 return map;
}
const MAP_DEFS=Object.fromEntries(Object.entries(descriptions).map(([id,d])=>[id,makeMap(id,d)]));
const api={WEAPON_DEFS,DIFFICULTIES,MAP_DEFS,makeMap};if(typeof module!=='undefined')module.exports=api;else root.TZBuildingData=api;
})(globalThis);
