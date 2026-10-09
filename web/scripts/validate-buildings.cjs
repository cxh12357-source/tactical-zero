const {MAP_DEFS}=require('../dist/src/building-data');
const {NavigationSystem}=require('../dist/src/building-systems');
let count=0;
for(const map of Object.values(MAP_DEFS)){
 const errors=new NavigationSystem(JSON.parse(JSON.stringify(map))).validate();
 for(const error of errors)console.error(error);
 count+=errors.length;
 console.log(`${map.name}: ${map.floors.length} floors, ${map.floors.reduce((n,f)=>n+f.enemySpawnZones.length,0)} spawn zones, ${errors.length} errors`);
}
if(count)process.exitCode=1;
