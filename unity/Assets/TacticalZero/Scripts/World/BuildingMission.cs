using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace TacticalZero
{
    [DefaultExecutionOrder(-550)]
    public sealed class BuildingMission : MonoBehaviour
    {
        public const float FloorHeight=5;
        public static BuildingMission Instance {get;private set;}
        public int MapIndex;
        public Material WallMaterial, FloorMaterial, MetalMaterial, WoodMaterial, AccentMaterial, GlassMaterial;
        public Transform[] FloorRoots;
        public Vector3[] SpawnZones;
        public Bounds[] RoomBounds;
        public BuildingDefinition Definition=>BuildingCatalog.Maps[MapIndex];
        public BuildingStage Stage {get;private set;}=BuildingStage.MISSION_BRIEFING;
        public int Remaining {get;private set;}
        public int Total {get;private set;}
        public float ExtractionProgress {get;private set;}
        public int CurrentFloor {get;private set;}=-1;
        public bool Transitioning {get;private set;}
        public float Fade {get;private set;}
        public string Notice {get;private set;}="";
        public readonly List<EnemyHealth> Roster=new List<EnemyHealth>();
        public readonly HashSet<int> Explored=new HashSet<int>();
        readonly List<NavMeshLinkInstance> links=new List<NavMeshLinkInstance>();
        readonly List<EnemyDefinition> enemyDefinitions=new List<EnemyDefinition>();
        readonly List<Renderer[]> actorRenderers=new List<Renderer[]>();
        readonly List<Behaviour[]> actorAnimation=new List<Behaviour[]>();
        readonly List<int> actorVisibleFloor=new List<int>();
        MaterialPropertyBlock beaconStyle;
        Renderer[][] floorRenderers;
        Light[][] floorLights;
        Renderer beacon;
        float noticeUntil,nextVisual;
        GameManager game;
        public static int FloorAt(Vector3 point)=>Mathf.Max(0,Mathf.FloorToInt((point.y+.3f)/FloorHeight));
        public static bool SameFloor(Vector3 a,Vector3 b)=>!Instance||FloorAt(a)==FloorAt(b);
        public static Vector3 Stair(int floor,int side,bool up)=>BuildingArchitecture18.Stair(Instance?Instance.MapIndex:BuildingCatalog.SelectedMap,floor,side,up);
        BuildingLayout18 layout; public BuildingLayout18 Layout=>layout??=BuildingLayout18.Create(MapIndex);
        int[] roomStarts,roomCounts;
        void CacheRooms(){if(roomStarts!=null)return;roomStarts=new int[Definition.Floors.Length];roomCounts=new int[Definition.Floors.Length];for(int i=0;i<Layout.Rooms.Count;i++){int f=Layout.Rooms[i].Floor;if(roomCounts[f]==0)roomStarts[f]=i;roomCounts[f]++;}}
        public int FloorStart(int f){if(!BuildingPerformance18.Optimized)return Layout.Rooms.FindIndex(r=>r.Floor==f);CacheRooms();return roomStarts[f];}
        public int FloorCount(int f){if(!BuildingPerformance18.Optimized)return Layout.Rooms.FindAll(r=>r.Floor==f).Count;CacheRooms();return roomCounts[f];}
        public string ObjectiveText=>Definition.Floors[Mathf.Clamp(CurrentFloor,0,Definition.Floors.Length-1)]+"  ·  剩余敌人 "+Remaining+" / "+Total+"\n"+(Remaining>0?"清剿整栋建筑 · 撤离锁定":ExtractionProgress>0?"正在撤离 "+(3-ExtractionProgress).ToString("0.0")+" 秒":"区域已清空 · 前往 "+Definition.Floors[Definition.ExitFloor]+" 撤离");
        public string RecordKey=>"TZ3D.Building.Best.v18."+Definition.Id+"."+ClassicRules.DifficultyIndex+"."+BuildingCatalog.SelectedWeapon;
        void Awake(){Instance=this;beaconStyle=new MaterialPropertyBlock();}
        IEnumerator Start()
        {
            game=GameManager.Ensure();
            var surface=GetComponent<NavMeshSurface>();if(surface&&!surface.navMeshData)surface.BuildNavMesh();
            Physics.SyncTransforms();
            Warp(game.Player,Definition.Spawn);
            game.Player.transform.rotation=Quaternion.Euler(0,90,0);
            if(!game.Player.GetComponent<ClassicPlayerPresentation>())game.Player.gameObject.AddComponent<ClassicPlayerPresentation>();

            // WeaponController.Start owns model creation; equip only after that original initialization.
            yield return null;
            var weapons=game.Player.GetComponent<WeaponController>();
            if(BuildingCatalog.SelectedWeapon!="USP")weapons.TakeWeapon(BuildingCatalog.SelectedWeapon);
            floorRenderers=new Renderer[FloorRoots.Length][];
            floorLights=new Light[FloorRoots.Length][];
            for(int f=0;f<FloorRoots.Length;f++){floorRenderers[f]=FloorRoots[f].GetComponentsInChildren<Renderer>();floorLights[f]=FloorRoots[f].GetComponentsInChildren<Light>();}
            for(int i=0;i<SpawnZones.Length;i++)SpawnZones[i]=FindSpawn(i,0,.3f,1.8f,out _);
            SpawnRoster();
            Stage=BuildingStage.INFILTRATION;Say("行动开始 · 检查所有楼层，清空后撤离");
            var exit=Part("撤离信标",transform,Definition.Exit+Vector3.up*.025f,new Vector3(3,.045f,3),AccentMaterial,false);
            beacon=exit.GetComponent<Renderer>();
            UpdateFloor();
        }
        public Vector3 FindSpawn(int room,int variant,float radius,float height,out string support)
        {
            var bounds=RoomBounds[room];int floor=Layout.Rooms[room].Floor;var path=new NavMeshPath();
            for(int k=0;k<81;k++)
            {
                int n=(k+variant*17)%81;Vector3 at=SpawnZones[room]+new Vector3(((n%9)-4)*.65f,0,((n/9)-4)*.65f);
                if(at.x<bounds.min.x+radius+.25f||at.x>bounds.max.x-radius-.25f||at.z<bounds.min.z+radius+.25f||at.z>bounds.max.z-radius-.25f)continue;
                if(BuildingSupport18.Validate(at,floor,radius+.035f,height,out var grounded,out support)&&BuildingSupport18.Validate(grounded,floor,radius+.035f,height,out var stable,out support)&&NavMesh.CalculatePath(Definition.Spawn,stable,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)return stable;
            }
            throw new System.InvalidOperationException("NPC18_INVALID map="+Definition.Id+" floor="+floor+" room="+room+" reason=no capsule-safe supported spawn");
        }
        void SpawnRoster()
        {
            int perFloor=new[]{5,8,11,14}[ClassicRules.DifficultyIndex];
            Total=Remaining=perFloor*Definition.Floors.Length;
            for(int f=0;f<Definition.Floors.Length;f++)for(int i=0;i<perFloor;i++)
            {
                var prefab=game.Catalog.EnemyPrefabs[i%game.Catalog.EnemyPrefabs.Length];var capsule=prefab.GetComponent<CapsuleCollider>();
                float radius=capsule?capsule.radius:.3f,height=capsule?capsule.height:1.8f;
                Vector3 at=FindSpawn(FloorStart(f)+i%FloorCount(f),i/Mathf.Max(1,FloorCount(f)),radius,height,out var support);
                var enemy=Instantiate(prefab,at,Quaternion.Euler(0,i*137,0));
                enemy.GetComponent<NavMeshAgent>().enabled=false;
                enemy.name=Definition.Floors[f]+"守卫"+i;
                foreach(var r in enemy.GetComponentsInChildren<Renderer>())r.enabled=false;
                var model=Instantiate(Resources.Load<GameObject>("Classic/Operators/"+BuildingLayout18.Operators[(i+f)%BuildingLayout18.Operators.Length]),enemy.transform);
                model.name="Detailed operator";model.AddComponent<EnemyCombatPresentation>();
                var holster=model.transform.Find("Holstered preview weapon");if(holster)holster.gameObject.SetActive(false);
                enemy.GetComponent<EnemyAnimatorBridge>().enabled=false;
                enemy.GetComponent<EnemyBrain>().enabled=false;
                var data=ScriptableObject.CreateInstance<EnemyDefinition>();enemyDefinitions.Add(data);
                data.Configure("建筑守卫",100,2.1f+ClassicRules.DifficultyIndex*.22f,38,105,10,7,3,.16f);
                enemy.GetComponent<EnemyHealth>().Configure(data);
                var gun=ClassicRules.Gun(ClassicRules.Difficulty.guns[i%ClassicRules.Difficulty.guns.Length]);
                enemy.GetComponent<EnemyCombat>().ClassicWeapon=gun;
                var firearm=Instantiate(Resources.Load<GameObject>("Classic/Models/"+gun.id),model.transform);
                firearm.transform.localPosition=new Vector3(.07f,1.49f,.22f);firearm.transform.localScale=Vector3.one*.85f;
                foreach(var arm in firearm.GetComponentsInChildren<FirstPersonArm>())arm.gameObject.SetActive(false);
                foreach(var t in firearm.GetComponentsInChildren<Transform>())if(t.name.Contains("sleeve")||t.name.Contains("glove")||t.name.Contains("Finger"))t.gameObject.SetActive(false);
                var health=enemy.GetComponent<EnemyHealth>();Roster.Add(health);health.Died+=OnEnemyDeath;
                actorRenderers.Add(model.GetComponentsInChildren<Renderer>());actorVisibleFloor.Add(-1);
                var animated=new List<Behaviour>();animated.AddRange(model.GetComponentsInChildren<OperatorRig>());animated.AddRange(model.GetComponentsInChildren<FootContact16>());animated.AddRange(model.GetComponentsInChildren<EnemyCombatPresentation>());actorAnimation.Add(animated.ToArray());
                enemy.AddComponent<BuildingActor18>().Place(at,f,support);
                enemy.AddComponent<BuildingEnemy>().Configure(this,data,SpawnZones[FloorStart(f)+(i+1)%FloorCount(f)]);
                Debug.Log("NPC18_READY id="+enemy.name+" map="+Definition.Id+" floor="+f+" spawn="+at+" support="+support+" height="+at.y+" phase=validated-before-ai");
            }
        }
        void OnEnemyDeath()
        {
            Remaining=0;foreach(var enemy in Roster)if(enemy&&enemy.Alive)Remaining++;
            if(Remaining==0){Stage=BuildingStage.EXTRACTION_AVAILABLE;Say("区域已清空，前往 "+Definition.Floors[Definition.ExitFloor]+" 撤离点");AudioService.Instance?.Play("victory",Vector3.zero,.75f,false);}
        }
        public void Say(string text){Notice=text;noticeUntil=Time.time+5;}
        void Update()
        {
            if(!game||!game.IsPlaying||!game.Player||Total==0)return;
            if(Stage==BuildingStage.INFILTRATION&&game.Stats.seconds>2)Stage=BuildingStage.CLEAR_BUILDING;
            TickExtraction(Time.deltaTime,game.Player.transform.position,game.Player.Health);
            if(Time.time>=nextVisual){nextVisual=Time.time+.2f;UpdateFloor();}
            if(Time.time>noticeUntil)Notice="";
            if(beacon){beacon.enabled=CurrentFloor==Definition.ExitFloor;beaconStyle.SetColor("_BaseColor",Remaining>0?new Color(.6f,.06f,.03f):Color.Lerp(new Color(.1f,.5f,.22f),Color.green,Mathf.PingPong(Time.time,1)));beacon.SetPropertyBlock(beaconStyle);}
        }
        public void TickExtraction(float dt,Vector3 position,float health)
        {
            if(Stage==BuildingStage.MISSION_SUCCESS||Stage==BuildingStage.MISSION_FAILED)return;
            if(health<=0){Stage=BuildingStage.MISSION_FAILED;ExtractionProgress=0;return;}
            bool inside=FloorAt(position)==Definition.ExitFloor&&(position-Definition.Exit).sqrMagnitude<2.2f*2.2f;
            if(Remaining>0){ExtractionProgress=0;if(inside)Say("区域尚未清空 · 撤离锁定");return;}
            ExtractionProgress=inside?ExtractionProgress+Mathf.Max(0,dt):0;
            Stage=inside?BuildingStage.EXTRACTING:BuildingStage.EXTRACTION_AVAILABLE;
            if(ExtractionProgress<3)return;
            Stage=BuildingStage.MISSION_SUCCESS;
            float best=PlayerPrefs.GetFloat(RecordKey,float.MaxValue);
            if(game.Stats.seconds<best){PlayerPrefs.SetFloat(RecordKey,game.Stats.seconds);PlayerPrefs.Save();}
            game.EndMatch(true);
        }
        void UpdateFloor()
        {
            int floor=Mathf.Clamp(FloorAt(game.Player.transform.position),0,Definition.Floors.Length-1);
            if(floor!=CurrentFloor&&floorRenderers!=null)
            {
                CurrentFloor=floor;
                for(int f=0;f<floorRenderers.Length;f++)foreach(var r in floorRenderers[f])if(r)r.enabled=f==floor||MapIndex==0||(MapIndex==3&&floor<=1&&f<=1)||(r.name.Contains("楼梯")&&f==floor-1);
                for(int f=0;f<floorLights.Length;f++)foreach(var light in floorLights[f])if(light)light.enabled=f==floor;
            }
            for(int i=0;i<Roster.Count;i++)if(Roster[i])
            {
                int visible=FloorAt(Roster[i].transform.position)==floor?1:0;
                if(visible!=actorVisibleFloor[i]){actorVisibleFloor[i]=visible;foreach(var r in actorRenderers[i])if(r)r.enabled=visible==1;foreach(var motion in actorAnimation[i])if(motion)motion.enabled=visible==1&&(Roster[i].Alive||motion is EnemyCombatPresentation);}
            }
            for(int i=FloorStart(floor);i<FloorStart(floor)+FloorCount(floor);i++)if(RoomBounds[i].Contains(game.Player.transform.position+Vector3.up))Explored.Add(i);
        }
        public IEnumerator ChangeFloor(int target,int side,bool goingUp)
        {
            if(Transitioning||target<0||target>=Definition.Floors.Length)yield break;
            Transitioning=true;game.Input.Clear();
            for(float t=0;t<.12f;t+=Time.deltaTime){Fade=t/.12f;yield return null;}
            Warp(game.Player,Stair(target,side,!goingUp)+Vector3.forward*Mathf.Sign(Stair(target,side,!goingUp).z)*1.2f);
            game.Input.Clear();UpdateFloor();BuildingAcoustics.Emit(game.Player.transform.position,18,NoiseKind.Stair);
            for(float t=0;t<.15f;t+=Time.deltaTime){Fade=1-t/.15f;yield return null;}
            Fade=0;Transitioning=false;game.Input.Clear();
        }
        public static void Warp(PlayerHealth player,Vector3 point)
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=point+Vector3.up*.06f;cc.enabled=true;
            player.GetComponent<FPSPlayerController>().ResetMotion();Physics.SyncTransforms();
        }
        public List<string> ValidateNavigation()
        {
            var errors=new List<string>();var path=new NavMeshPath();
            if(!NavMesh.SamplePosition(Definition.Spawn,out var spawn,.5f,NavMesh.AllAreas)||FloorAt(spawn.position)!=Definition.SpawnFloor)errors.Add(Definition.Id+" player spawn is not on its floor navigation mesh");
            foreach(var p in SpawnZones)if(!NavMesh.CalculatePath(Definition.Spawn,p,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)errors.Add(Definition.Id+" floor "+FloorAt(p)+" spawn "+p+" unreachable");
            if(!NavMesh.CalculatePath(Definition.Spawn,Definition.Exit,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)errors.Add(Definition.Id+" extraction unreachable");
            var stairs=GetComponentsInChildren<BuildingStair>();
            foreach(var stair in stairs)
            {
                bool paired=false;int target=stair.Floor+(stair.Up?1:-1);
                foreach(var other in stairs)if(other.Floor==target&&other.Side==stair.Side&&other.Up!=stair.Up){paired=true;break;}
                if(!paired)errors.Add(Definition.Id+" floor "+stair.Floor+" stair "+stair.name+" has no return link");
                var exit=Stair(stair.Floor,stair.Side,stair.Up)+Vector3.forward*Mathf.Sign(Stair(stair.Floor,stair.Side,stair.Up).z)*1.2f;
                if(Physics.CheckCapsule(exit+Vector3.up*.4f,exit+Vector3.up*1.4f,.28f,LayerMask.GetMask("TZWorld","TZInteractable"),QueryTriggerInteraction.Ignore))errors.Add(Definition.Id+" floor "+stair.Floor+" stair exit overlaps collision: "+exit);
            }
            foreach(var door in GetComponentsInChildren<BuildingDoor>())
            {
                var box=door.GetComponent<BoxCollider>();
                if(Physics.CheckBox(box.bounds.center,Vector3.Scale(box.bounds.extents,new Vector3(.9f,.9f,.6f)),Quaternion.identity,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore)){var collision=Physics.OverlapBox(box.bounds.center,Vector3.Scale(box.bounds.extents,new Vector3(.9f,.9f,.6f)),Quaternion.identity,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore);foreach(var c in collision)errors.Add(Definition.Id+" floor "+door.Floor+" door overlaps: "+door.name+" with "+c.name+" at "+c.transform.position);}
            }
            return errors;
        }
        void OnDestroy()
        {
            foreach(var link in links)if(NavMesh.IsLinkValid(link))NavMesh.RemoveLink(link);
            foreach(var data in enemyDefinitions)if(data)Destroy(data);
            if(Instance==this)Instance=null;
        }

        // Geometry is generated into the actual Unity scenes by the editor builder.
        public void GenerateGeometry(){BuildingArchitecture18.Generate(this);}
        void MakeStair(Transform root,int floor,int side,bool up)
        {
            Vector3 local=Stair(0,side,up);var go=new GameObject(up?"上楼触发":"下楼触发");go.transform.SetParent(root,false);go.transform.localPosition=local;
            var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up;trigger.size=new Vector3(2,2,1.3f);
            var stair=go.AddComponent<BuildingStair>();stair.Floor=floor;stair.Side=side;stair.Up=up;
            Part("楼梯引导",root,local+Vector3.up*.035f,new Vector3(1.8f,.04f,1.2f),AccentMaterial,false);
            Sign(root,up?"↑ 上楼":"↓ 下楼",local+Vector3.up*2.1f,side<0?90:-90);
        }
        void MakeDoorWall(Transform root,float x,float z,float width,int floor,string label)
        {
            float opening=1.9f;
            foreach(int side in new[]{-1,1})Part("门侧墙",root,new Vector3(x+side*(width+opening)*.25f,1.7f,z),new Vector3((width-opening)*.5f,3.4f,.24f),WallMaterial);
            Part("门楣",root,new Vector3(x,2.95f,z),new Vector3(opening,.9f,.24f),WallMaterial);
            var door=Part(label+" 门",root,new Vector3(x,1.2f,z),new Vector3(1.8f,2.4f,.16f),MapIndex==1?GlassMaterial:WoodMaterial);
            door.layer=LayerMask.NameToLayer("TZInteractable");door.AddComponent<NavMeshModifier>().ignoreFromBuild=true;door.AddComponent<BuildingDoor>().Floor=floor;
        }
        public void Furniture(Transform root,string label,Vector3 p,int theme,int i)
        {
            if(theme==0&&(label.Contains("卧")||label.Contains("客房")))
            {
                Part("床架",root,p+Vector3.up*.22f,new Vector3(1.8f,.4f,2.2f),WoodMaterial);
                Part("床垫",root,p+Vector3.up*.5f,new Vector3(1.75f,.2f,2.15f),WallMaterial);
                Part("床头",root,p+new Vector3(0,.8f,1.1f),new Vector3(1.9f,1,.16f),WoodMaterial);
                Part("床头柜",root,p+new Vector3(1.45f,.35f,.8f),new Vector3(.65f,.7f,.65f),WoodMaterial);return;
            }
            if(theme==0&&(label.Contains("厨房")||label.Contains("洗衣")))
            {
                Part("厨房柜台",root,p+Vector3.up*.5f,new Vector3(2.8f,1,.65f),WoodMaterial);
                Part("操作台面",root,p+Vector3.up*1.03f,new Vector3(2.9f,.09f,.72f),WallMaterial);
                Part(label.Contains("洗衣")?"滚筒洗衣机":"冰箱",root,p+new Vector3(2,.7f,0),new Vector3(.85f,1.4f,.8f),MetalMaterial);
                Part("水槽",root,p+new Vector3(-.7f,1.085f,0),new Vector3(.65f,.035f,.5f),MetalMaterial);return;
            }
            if((theme==1&&(label.Contains("服务器")||label.Contains("档案")||label.Contains("运维")))||(theme==2&&(label.Contains("药")||label.Contains("器械")||label.Contains("停尸"))))
            {
                for(int n=0;n<3;n++)
                {
                    var at=p+Vector3.right*n*.95f;
                    Part(label.Contains("停尸")?"冷藏柜":"器材机柜",root,at+Vector3.up*1.05f,new Vector3(.82f,2.1f,.65f),MetalMaterial);
                    for(int h=0;h<5;h++)Part("抽屉面板",root,at+new Vector3(0,.22f+h*.38f,.34f),new Vector3(.72f,.29f,.035f),WoodMaterial);
                }
                return;
            }
            if(theme==2)
            {
                foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}){Part("病床支腿",root,p+new Vector3(x*.43f,.2f,z*.9f),new Vector3(.08f,.4f,.08f),MetalMaterial,false);Part("病床脚轮",root,p+new Vector3(x*.43f,.07f,z*.9f),new Vector3(.16f,.14f,.12f),MetalMaterial,false);}
                foreach(int x in new[]{-1,1}){Part("病床护栏",root,p+new Vector3(x*.57f,.85f,0),new Vector3(.035f,.035f,1.6f),MetalMaterial,false);for(int z=-1;z<=1;z++)Part("护栏立杆",root,p+new Vector3(x*.57f,.65f,z*.65f),new Vector3(.025f,.4f,.025f),MetalMaterial,false);}
                Part("监护显示屏",root,p+new Vector3(1.6f,1.22f,.738f),new Vector3(.39f,.25f,.025f),AccentMaterial,false);
                Part("病床底架",root,p+Vector3.up*.38f,new Vector3(1.1f,.16f,2.2f),MetalMaterial);
                Part("床垫",root,p+Vector3.up*.58f,new Vector3(1.08f,.24f,2.15f),WallMaterial);
                Part("枕头",root,p+new Vector3(0,.75f,.75f),new Vector3(.8f,.15f,.4f),WallMaterial);
                Part("医疗设备",root,p+new Vector3(1.6f,.8f,1),new Vector3(.5f,1.6f,.5f),MetalMaterial);
            }
            else if(theme==3)
            {
                Part("机床上壳",root,p+new Vector3(-.5f,1.6f,.15f),new Vector3(1.25f,.6f,1.05f),MetalMaterial,false);
                Part("观察防护玻璃",root,p+new Vector3(-.5f,1.62f,-.39f),new Vector3(.95f,.35f,.035f),GlassMaterial,false);
                Part("机床控制台",root,p+new Vector3(.92f,1.52f,-.1f),new Vector3(.45f,.5f,.2f),MetalMaterial,false);
                Part("机床显示屏",root,p+new Vector3(.92f,1.57f,-.215f),new Vector3(.33f,.19f,.025f),AccentMaterial,false);
                for(int button=0;button<3;button++)Part("机床按钮",root,p+new Vector3(.8f+button*.11f,1.35f,-.215f),new Vector3(.045f,.045f,.025f),WoodMaterial,false);
                Part("生产设备",root,p+Vector3.up*.65f,new Vector3(2.6f,1.3f,1.5f),MetalMaterial);
                for(int r=0;r<3;r++)Part("货架层",root,p+new Vector3(2.5f,.4f+r*.65f,0),new Vector3(1,.1f,2.1f),MetalMaterial);
                foreach(int s in new[]{-1,1})Part("货架立柱",root,p+new Vector3(2.5f+s*.45f,1,0),new Vector3(.12f,2,.15f),MetalMaterial);
            }
            else
            {
                Part(theme==0?"木桌":"办公桌",root,p+Vector3.up*.74f,new Vector3(2,.15f,1),WoodMaterial);
                foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})Part("桌腿",root,p+new Vector3(a*.84f,.35f,b*.35f),new Vector3(.09f,.7f,.09f),MetalMaterial);
                Part(theme==0?"沙发坐垫":"办公椅",root,p+new Vector3(0,.46f,1.5f),new Vector3(theme==0?2:.65f,.35f,.7f),FloorMaterial);
                Part("靠背",root,p+new Vector3(0,.85f,1.82f),new Vector3(theme==0?2:.65f,.7f,.15f),FloorMaterial);
                foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Part("座椅支脚",root,p+new Vector3(x*(theme==0?.8f:.24f),.16f,1.5f+z*.24f),new Vector3(.08f,.32f,.08f),MetalMaterial,false);
                if(theme==1)Part("显示器",root,p+new Vector3(0,1.12f,-.25f),new Vector3(.75f,.5f,.07f),MetalMaterial);
            }
            Part("储物柜",root,p+new Vector3(-1.5f,1,-.5f),new Vector3(.8f,2,.55f),WoodMaterial);
        }
        public static GameObject Part(string name,Transform parent,Vector3 at,Vector3 scale,Material material,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localScale=scale;go.layer=LayerMask.NameToLayer("TZWorld");go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid){var c=go.GetComponent<Collider>();if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);}return go;
        }
        static void Sign(Transform parent,string label,Vector3 at,float yaw)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);go.transform.localPosition=at;go.transform.localRotation=Quaternion.Euler(0,yaw+180,0);
            var text=go.AddComponent<TextMesh>();text.text=label;text.fontSize=36;text.characterSize=.045f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.7f,.94f,.83f);
            go.AddComponent<BuildingSign>();
        }
    }
}
