using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TacticalZero
{
    public sealed class ClassicEncounter : MonoBehaviour
    {
        public static ClassicEncounter Instance {get;private set;}
        public string MapId;
        public ClassicMap Map {get;private set;}
        public Vector3 Extraction {get;private set;}
        public readonly List<string> Bag=new List<string>(8);
        public string Notice {get;set;}="";
        public float SearchProgress {get;set;}
        public bool Cleared=>EnemyHealth.Active.Count==0;
        public int BagValue {get {int total=0;foreach(var item in Bag)total+=Value(item);return total;}}
        public int Stored=>PlayerPrefs.GetInt("TZ3D.Stash",0);
        public static int Value(string item)=>item=="intel"?180:item=="circuit"?90:item=="med"?60:35;
        public static string ItemName(string item)=>item=="intel"?"加密情报":item=="circuit"?"电子元件":item=="med"?"医疗物资":"机械零件";
        readonly List<Vector3> floor=new List<Vector3>();
        readonly bool[,] walkable=new bool[28,20];
        GrassVolume[] brush;
        public bool WalkableCell(int x,int y)=>x>=0&&x<28&&y>=0&&y<20&&walkable[x,y];
        readonly List<ClassicLoot> drops=new List<ClassicLoot>();
        readonly List<ClassicLoot> supplies=new List<ClassicLoot>();
        public FieldOperation Operation {get;private set;}
        public float Elapsed=>elapsed;
        float elapsed,nextSupply=50;
        public int RespawnCount {get;private set;}
        public IReadOnlyList<ClassicLoot> Supplies=>supplies;
        float nextDrop=35,extractTime;bool settled;
        GameManager game;GameObject extractionMarker;
        void Awake(){Instance=this;Map=System.Array.Find(ClassicRules.Data.maps,x=>x.id==MapId);}
        void OnDestroy(){if(Instance==this)Instance=null;}
        void Start()
        {
            game=GameManager.Ensure();
            game.Player.gameObject.AddComponent<ClassicPlayerPresentation>();
            
            // Dense world-space samples also cover curved alleys and narrow non-grid routes.
            for(float z=-28.5f;z<29;z+=1.5f)for(float x=-40.5f;x<42;x+=1.5f)if(NavMesh.SamplePosition(new Vector3(x,0,z),out var hit,.6f,NavMesh.AllAreas))floor.Add(hit.position);
            foreach(var point in Object.FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None))if(NavMesh.SamplePosition(point.transform.position,out var hit,.8f,NavMesh.AllAreas))floor.Add(hit.position);
            if(floor.Count>0)
            {
                var connected=new NavMeshPath();var origin=floor[0];floor.RemoveAll(p=>!NavMesh.CalculatePath(origin,p,NavMesh.AllAreas,connected)||connected.status!=NavMeshPathStatus.PathComplete);
                foreach(var p in floor){int x=Mathf.Clamp(Mathf.FloorToInt(p.x/3+14),0,27),y=Mathf.Clamp(Mathf.FloorToInt(10-p.z/3),0,19);walkable[x,y]=true;}
            }
            brush=Object.FindObjectsByType<GrassVolume>(FindObjectsSortMode.None);
            if(floor.Count==0){Debug.LogError("Classic map has no walkable spawn locations.");return;}
            Vector3 spawn=floor[Random.Range(0,floor.Count)];WarpPlayer(spawn+Vector3.up*.08f);
            if(spawn.sqrMagnitude>1)game.Player.transform.rotation=Quaternion.LookRotation(new Vector3(-spawn.x,0,-spawn.z));
            Extraction=Farthest(spawn);extractionMarker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);extractionMarker.name="Extraction beacon";
            Destroy(extractionMarker.GetComponent<Collider>());extractionMarker.transform.position=Extraction+Vector3.up*.025f;extractionMarker.transform.localScale=new Vector3(5,.025f,5);extractionMarker.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Classic/Materials/GlowTeal");
            var diff=ClassicRules.Difficulty;var used=new List<Vector3>{spawn};
            for(int i=0;i<diff.count;i++)
            {
                Vector3 pos=SafeSpawn(used,spawn);used.Add(pos);var enemy=Instantiate(game.Catalog.EnemyPrefabs[i%2],pos,Quaternion.Euler(0,Random.Range(0,360),0));
                enemy.name="守卫 "+(i+1).ToString("00");
                foreach(var r in enemy.GetComponentsInChildren<Renderer>())r.enabled=false;
                var role=ClassicRules.Data.roles[(i+2)%ClassicRules.Data.roles.Length];
                var model=Instantiate(Resources.Load<GameObject>("Classic/Operators/"+role.id),enemy.transform);model.name="Detailed operator";model.AddComponent<EnemyCombatPresentation>();
                enemy.GetComponent<EnemyAnimatorBridge>().enabled=false;
                var displayGun=model.transform.Find("Holstered preview weapon");if(displayGun)displayGun.gameObject.SetActive(false);
                var brain=enemy.GetComponent<EnemyBrain>();
                var patrol=new Vector3[3];patrol[0]=pos;for(int j=1;j<3;j++)patrol[j]=ReachableNear(pos,5+j*3);
                brain.SetPatrol(patrol);enemy.GetComponent<EnemyCombat>().ClassicWeapon=ClassicRules.Gun(diff.guns[i%diff.guns.Length]);
                var gun=Instantiate(Resources.Load<GameObject>("Classic/Models/"+diff.guns[i%diff.guns.Length]),model.transform);
                gun.transform.localPosition=new Vector3(.07f,1.49f,.22f);gun.transform.localScale=Vector3.one*.85f;
                foreach(var part in gun.GetComponentsInChildren<Transform>())if(part.name.Contains("sleeve")||part.name.Contains("glove")||part.name.Contains("hand")||part.name.Contains("Finger"))part.gameObject.SetActive(false);
            }
            foreach(var point in Object.FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None))if(point.Supply)SpawnLoot(point.transform.position,"室内情报与物资","tech","");
            foreach(var poi in Map.pois)
            {
                Vector3 at=Nearest(ClassicMap.Position(poi.x+.5f,poi.y+.5f));
                SpawnLoot(at,poi.name,poi.type,"");SpawnLoot(ReachableNear(at,5),poi.name+"储藏箱",poi.type,"");
            }
            string[] field={"M4A1","P90","MG3","M700","M1014","VECTOR","DMR","RPK","Shotgun","AK47","MP6"};
            for(int i=0;i<field.Length;i++)SpawnLoot(i<5?ReachableNear(spawn,3+i*1.3f):floor[Random.Range(0,floor.Count)],field[i],"weapon",field[i]);
            if(Random.value<.1f)SpawnLoot(floor[Random.Range(0,floor.Count)],"AWP 稀有补给","weapon","AWP");
            for(int i=0;i<6;i++)SpawnLoot(i<2?ReachableNear(spawn,3+i):floor[Random.Range(0,floor.Count)],"急救包","heal","");
            Operation=gameObject.AddComponent<FieldOperation>();Operation.Configure(this,floor,spawn);
            Notice=Map.name+" / "+diff.label+"难度 · 清除 "+diff.count+" 名敌人后撤离";
            Debug.Log("Classic match ready: "+MapId+" / "+diff.id+" / "+"operator"+" / enemies="+diff.count);
        }
        public void SpawnReinforcements(int count)
        {
            var diff=ClassicRules.Difficulty;var used=new List<Vector3>{game.Player.transform.position};foreach(var active in EnemyHealth.Active)used.Add(active.transform.position);
            for(int i=0;i<count;i++)
            {
                Vector3 pos=SafeSpawn(used,game.Player.transform.position);used.Add(pos);var enemy=Instantiate(game.Catalog.EnemyPrefabs[i%2],pos,Quaternion.Euler(0,Random.Range(0,360),0));
                enemy.name="守卫 "+(i+1).ToString("00");
                foreach(var r in enemy.GetComponentsInChildren<Renderer>())r.enabled=false;
                var role=ClassicRules.Data.roles[(i+2)%ClassicRules.Data.roles.Length];
                var model=Instantiate(Resources.Load<GameObject>("Classic/Operators/"+role.id),enemy.transform);model.name="Detailed operator";model.AddComponent<EnemyCombatPresentation>();
                enemy.GetComponent<EnemyAnimatorBridge>().enabled=false;
                var displayGun=model.transform.Find("Holstered preview weapon");if(displayGun)displayGun.gameObject.SetActive(false);
                var brain=enemy.GetComponent<EnemyBrain>();
                var patrol=new Vector3[3];patrol[0]=pos;for(int j=1;j<3;j++)patrol[j]=ReachableNear(pos,5+j*3);
                brain.SetPatrol(patrol);enemy.GetComponent<EnemyCombat>().ClassicWeapon=ClassicRules.Gun(diff.guns[i%diff.guns.Length]);
                var gun=Instantiate(Resources.Load<GameObject>("Classic/Models/"+diff.guns[i%diff.guns.Length]),model.transform);
                gun.transform.localPosition=new Vector3(.07f,1.49f,.22f);gun.transform.localScale=Vector3.one*.85f;
                foreach(var part in gun.GetComponentsInChildren<Transform>())if(part.name.Contains("sleeve")||part.name.Contains("glove")||part.name.Contains("hand")||part.name.Contains("Finger"))part.gameObject.SetActive(false);
            }
        }
        void WarpPlayer(Vector3 at){var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;game.Player.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        public bool RespawnPlayer()
        {
            if(!game||game.Phase!=GamePhase.Defeat||game.Player.Health>0||floor.Count==0)return false;
            var death=game.Player.transform.position;
            var candidates=new List<Vector3>();var path=new NavMeshPath();Vector3 fallback=death;float best=-1;
            foreach(var point in floor)
            {
                if(Vector3.Distance(point,death)<8||Vector3.Distance(point,Extraction)<4)continue;
                if(!NavMesh.CalculatePath(point,Extraction,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                if(Physics.CheckCapsule(point+Vector3.up*.4f,point+Vector3.up*1.5f,.3f,LayerMask.GetMask("TZWorld","TZInteractable"),QueryTriggerInteraction.Ignore))continue;
                float nearest=100;bool exposed=false;
                foreach(var enemy in EnemyHealth.Active)
                {
                    float d=Vector3.Distance(point,enemy.transform.position);nearest=Mathf.Min(nearest,d);
                    if(d<24&&!Physics.Linecast(point+Vector3.up*1.4f,enemy.AimPoint,LayerMask.GetMask("TZWorld")))exposed=true;
                }
                float score=nearest+(exposed?0:20)+Random.value*3;
                if(score>best){best=score;fallback=point;}
                if(nearest>=12&&!exposed)candidates.Add(point);
            }
            if(best<0)return false;
            var spawn=candidates.Count>0?candidates[Random.Range(0,candidates.Count)]:fallback;
            WarpPlayer(spawn+Vector3.up*.08f);
            game.Player.GetComponent<FPSPlayerController>().ResetMotion();
            game.Player.GetComponent<WeaponController>().ResetLoadout();
            
            game.Player.GetComponent<PlayerVisibilityState>().ResetForRespawn();
            game.Player.Revive();
            game.Player.GetComponent<ClassicPlayerPresentation>().ResetForRespawn();
            Bag.Clear();SearchProgress=extractTime=0;RespawnCount++;
            SpawnLoot(ReachableNear(spawn,4),"复活武器补给","weapon","M4A1");
            Notice="随机复活 · 背包与装备已丢失 · 3 秒保护（开火即解除）";
            return true;
        }
        public void ReleaseSupply(ClassicLoot loot) {supplies.Remove(loot);drops.Remove(loot);}
        public Vector3 Nearest(Vector3 at){Vector3 best=floor[0];float distance=float.MaxValue;foreach(var p in floor){float d=(p-at).sqrMagnitude;if(d<distance){best=p;distance=d;}}return best;}
        Vector3 Farthest(Vector3 at){Vector3 best=floor[0];float score=-1;var path=new NavMeshPath();foreach(var p in floor){float d=(p-at).sqrMagnitude;if(d>score&&NavMesh.CalculatePath(at,p,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){best=p;score=d;}}return best;}
        Vector3 SafeSpawn(List<Vector3> used,Vector3 player)
        {
            Vector3 best=floor[Random.Range(0,floor.Count)];float bestScore=-1;var path=new NavMeshPath();
            for(int i=0;i<80;i++){Vector3 p=floor[Random.Range(0,floor.Count)];float nearest=float.MaxValue;foreach(var other in used)nearest=Mathf.Min(nearest,Vector3.Distance(p,other));float score=nearest+(Physics.Linecast(p+Vector3.up,player+Vector3.up,LayerMask.GetMask("TZWorld"))?8:0);if(score>bestScore&&NavMesh.CalculatePath(player,p,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){best=p;bestScore=score;}}
            return best;
        }
        public Vector3 ReachableNear(Vector3 at,float distance)
        {
            Vector3 best=at;float score=float.MaxValue;var path=new NavMeshPath();
            foreach(var p in floor){float d=Mathf.Abs(Vector3.Distance(p,at)-distance);if(d<score&&NavMesh.CalculatePath(at,p,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete){best=p;score=d;}}
            return best;
        }
        public bool InBrush(Vector3 at)
        {
            if(brush!=null)foreach(var volume in brush)if(volume&&volume.Contains(at))return true;
            return false;
        }
        public ClassicLoot SpawnLoot(Vector3 at,string label,string kind,string weapon)
        {
            if(kind=="equipment"||kind=="ammo")return null;
            supplies.RemoveAll(x=>!x||!x.gameObject.activeSelf);
            if(floor.Count==0||supplies.Count>=40)return null;
            Vector3 best=at;float distance=float.MaxValue;
            foreach(var p in floor)
            {
                if(Vector3.Distance(p,game.Player.transform.position)<2||Vector3.Distance(p,Extraction)<3)continue;
                bool used=false;foreach(var prior in supplies)if(prior&&prior.gameObject.activeSelf&&(prior.GroundPosition-p).sqrMagnitude<2){used=true;break;}if(used)continue;
                float d=(p-at).sqrMagnitude;if(d<distance){distance=d;best=p;}
            }
            if(distance==float.MaxValue)return null;
            at=best;
            var prefab=Resources.Load<GameObject>("Classic/Models/FieldCache");var root=Instantiate(prefab,at+Vector3.up*.5f,Quaternion.identity);
            root.name=label;root.layer=LayerMask.NameToLayer("TZInteractable");
            var loot=root.AddComponent<ClassicLoot>();loot.Configure(label,kind,weapon);supplies.Add(loot);return loot;
        }
        void Update()
        {
            if(!game||!game.IsPlaying||floor.Count==0)return;
            elapsed+=Time.deltaTime;
            drops.RemoveAll(x=>!x||!x.gameObject.activeSelf);
            supplies.RemoveAll(x=>!x||!x.gameObject.activeSelf);
            if(elapsed>=nextSupply)
            {
                nextSupply=elapsed+50;
                if(supplies.Count<=32)
                {
                    string[] guns={"P90","MG3","M700","M1014","MP6","AK47","M4A1","Shotgun","VECTOR","DMR","RPK","AWP"};
                    string gun=guns[Random.Range(0,guns.Length)];
                    SpawnLoot(floor[Random.Range(0,floor.Count)],"战地武器 · "+gun,"weapon",gun);
                    SpawnLoot(floor[Random.Range(0,floor.Count)],"急救包","heal","");
                    if(!Cleared)Notice="普通武器与急救包已刷新";
                }
            }
            if(Cleared&&Operation&&Operation.ReadyForExtraction)
            {
                game.Objective.Acquire();
                Vector3 delta=game.Player.transform.position-Extraction;
                bool inside=new Vector2(delta.x,delta.z).magnitude<2.7f&&Mathf.Abs(delta.y)<1&&game.Player.GetComponent<FPSPlayerController>().Grounded;
                extractTime=inside?extractTime+Time.deltaTime:0;
                if(extractTime>=5&&!settled){settled=true;PlayerPrefs.SetInt("TZ3D.Stash",Mathf.Min(1000000000,Stored+BagValue));PlayerPrefs.Save();game.Stats.supplies=BagValue;game.EndMatch(true);}
            }
        }
        public string ObjectiveText=>Operation&&!Operation.ReadyForExtraction?"行动目标 "+Operation.Stage+" / 3 · 守卫 "+EnemyHealth.Active.Count:Cleared?(extractTime>0?"撤离倒计时 "+(5-extractTime).ToString("0.0")+" 秒":"清场完成 · 前往绿色撤离点 "+Vector3.Distance(game.Player.transform.position,Extraction).ToString("0")+" m"):"清除守卫 "+EnemyHealth.Active.Count+" / "+ClassicRules.Difficulty.count+" · 搜刮后撤离";
        public bool AddContents(string kind)
        {
            if(Bag.Count>=8)return false;
            string[] items=kind=="tech"?new[]{"circuit","intel"}:kind=="med"?new[]{"med","scrap"}:kind=="arms"?new[]{"scrap","intel"}:new[]{"scrap","scrap"};
            foreach(var item in items)if(Bag.Count<8)Bag.Add(item);

            game.Stats.supplies=BagValue;Notice="搜索完成 · "+Bag.Count+" / 8 格 · 战利品 "+BagValue;return true;
        }
    }
}


