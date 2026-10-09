using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class BuildingSweepTests
    {
        GameManager game;BuildingMission mission;int priorMap,priorDifficulty;string priorWeapon;bool priorEnabled;
        string priorSettings,priorProgress;bool hadSettings,hadProgress;
        string bestKey;float priorBest;bool hadBest;
        [UnitySetUp] public IEnumerator Setup()
        {
            priorMap=BuildingCatalog.SelectedMap;priorDifficulty=ClassicRules.DifficultyIndex;priorWeapon=BuildingCatalog.SelectedWeapon;priorEnabled=BuildingCatalog.Enabled;
            hadSettings=PlayerPrefs.HasKey(SaveService.SettingsKey);hadProgress=PlayerPrefs.HasKey(SaveService.ProgressKey);priorSettings=PlayerPrefs.GetString(SaveService.SettingsKey);priorProgress=PlayerPrefs.GetString(SaveService.ProgressKey);
            bestKey="TZ3D.Building.Best.harbor.0.M4A1";hadBest=PlayerPrefs.HasKey(bestKey);priorBest=PlayerPrefs.GetFloat(bestKey);
            BuildingCatalog.Enabled=true;BuildingCatalog.SelectedMap=0;BuildingCatalog.SelectedWeapon="M4A1";ClassicRules.DifficultyIndex=0;
            yield return Load(0);
        }
        IEnumerator Load(int index)
        {
            Time.timeScale=1;
            yield return SceneManager.LoadSceneAsync(BuildingCatalog.Maps[index].Scene);
            yield return null;yield return null;yield return null;
            game=GameManager.Instance;mission=BuildingMission.Instance;
            Assert.IsNotNull(game);Assert.IsNotNull(mission);Assert.Greater(mission.Total,0);
            foreach(var enemy in mission.Roster){enemy.GetComponent<BuildingEnemy>().enabled=false;enemy.GetComponent<EnemyCombat>().enabled=false;}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(game){game.Input.Clear();game.Input.ForceTouch=false;}
            BuildingCatalog.SelectedMap=priorMap;BuildingCatalog.SelectedWeapon=priorWeapon;BuildingCatalog.Enabled=priorEnabled;ClassicRules.DifficultyIndex=priorDifficulty;
            if(hadSettings)PlayerPrefs.SetString(SaveService.SettingsKey,priorSettings);else PlayerPrefs.DeleteKey(SaveService.SettingsKey);
            if(hadProgress)PlayerPrefs.SetString(SaveService.ProgressKey,priorProgress);else PlayerPrefs.DeleteKey(SaveService.ProgressKey);
            if(hadBest)PlayerPrefs.SetFloat(bestKey,priorBest);else PlayerPrefs.DeleteKey(bestKey);PlayerPrefs.Save();SaveService.Load();
            Time.timeScale=1;yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
        }
        [UnityTest] public IEnumerator FourBuildingsHaveConnectedFloorsFixedRosterAndPairedStairs()
        {
            for(int i=0;i<4;i++)
            {
                if(i>0)yield return Load(i);
                Assert.AreEqual(BuildingCatalog.Maps[i].Floors.Length,mission.FloorRoots.Length);
                Assert.AreEqual(mission.Layout.Rooms.Count,mission.SpawnZones.Length);
                Assert.AreEqual(mission.FloorRoots.Length*5,mission.Total);
                Assert.AreEqual(mission.Total,EnemyHealth.Active.Count);
                Assert.IsEmpty(mission.ValidateNavigation(),mission.Definition.Name);
                var stairs=Object.FindObjectsByType<BuildingStair>();
                Assert.AreEqual((mission.FloorRoots.Length-1)*4,stairs.Length);
                foreach(var stair in stairs)Assert.IsTrue(stairs.Any(s=>s.Floor==stair.Floor+(stair.Up?1:-1)&&s.Side==stair.Side&&s.Up!=stair.Up));
                Assert.IsNull(ClassicEncounter.Instance);Assert.IsNull(Object.FindAnyObjectByType<FieldOperation>());
                ClassicCampaignTests.Capture("building17-"+mission.Definition.Id,1280,720);
            }
        }
        void Click(string prefix)
        {
            var button=Object.FindObjectsByType<UnityEngine.UI.Button>().First(b=>b.gameObject.activeInHierarchy&&b.name.StartsWith(prefix));Assert.IsTrue(button.interactable,prefix);button.onClick.Invoke();
        }
        [UnityTest] public IEnumerator DeploymentUsesOriginalModelsAndPersistsWeaponAcrossMenuReload()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;yield return null;
            Click("开始行动 →");yield return null;Click("武器 P90");yield return null;
            Assert.AreEqual("P90",BuildingCatalog.SelectedWeapon);
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;yield return null;
            Click("开始行动 →");yield return null;
            Assert.IsTrue(Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text.Contains("P90")&&t.text.Contains("✓")));
            Click("选择建筑 →");yield return null;Click("Map card hospital");yield return null;
            Click("确认建筑 →");yield return null;Click("查看任务简报 →");yield return null;Click("进入建筑 →");
            float until=Time.realtimeSinceStartup+15;
            while(SceneManager.GetActiveScene().name!=BuildingCatalog.Maps[2].Scene&&Time.realtimeSinceStartup<until)yield return null;
            yield return null;yield return null;yield return null;
            game=GameManager.Instance;mission=BuildingMission.Instance;
            Assert.AreEqual("hospital",mission.Definition.Id);Assert.AreEqual("P90",game.Player.GetComponent<WeaponController>().Current.Id);Assert.AreEqual(1,mission.CurrentFloor);
        }
        [UnityTest] public IEnumerator EveryStairExitIsClearAndInputIsReset()
        {
            foreach(var map in BuildingCatalog.Maps)
            {
                yield return Load(System.Array.IndexOf(BuildingCatalog.Maps,map));
                foreach(var stair in Object.FindObjectsByType<BuildingStair>())
                {
                    BuildingMission.Warp(game.Player,BuildingMission.Stair(stair.Floor,stair.Side,stair.Up)+Vector3.back*3);
                    game.Input.Mobile.Move=Vector2.one;game.Input.Mobile.Fire=true;
                    yield return mission.ChangeFloor(stair.Floor+(stair.Up?1:-1),stair.Side,stair.Up);
                    Assert.AreEqual(stair.Floor+(stair.Up?1:-1),mission.CurrentFloor);
                    Assert.AreEqual(Vector2.zero,game.Input.Mobile.Move);Assert.IsFalse(game.Input.Mobile.Fire);
                    Assert.IsFalse(Physics.CheckCapsule(game.Player.transform.position+Vector3.up*.4f,game.Player.transform.position+Vector3.up*1.4f,.28f,LayerMask.GetMask("TZWorld","TZInteractable"),QueryTriggerInteraction.Ignore));
                    Assert.IsFalse(mission.Transitioning);
                }
            }
        }
        [UnityTest] public IEnumerator OriginalWeaponsEquipFireReloadAndKeepBlenderArms()
        {
            var guns=game.Player.GetComponent<WeaponController>();
            foreach(var id in BuildingCatalog.Weapons)
            {
                if(id=="USP")guns.Switch(0);else Assert.IsTrue(guns.TakeWeapon(id),id);
                yield return new WaitForSeconds(.3f);
                Assert.AreEqual(id,guns.Current.Id);
                int loaded=guns.Ammo.Loaded;Assert.IsTrue(guns.TryShoot(),id);Assert.AreEqual(loaded-1,guns.Ammo.Loaded);
                Assert.IsTrue(guns.Ammo.StartReload(guns.Current.ReloadSeconds),id+" needs usable reserves");
                Assert.IsNotNull(game.Player.GetComponentInChildren<WeaponReloadAnimation>());
                Assert.AreEqual(2,game.Player.GetComponentsInChildren<FirstPersonArm>().Length,id+" original articulated arms");
                guns.Ammo.Tick(guns.Current.ReloadSeconds+.1f);Assert.AreEqual(guns.Ammo.Capacity,guns.Ammo.Loaded);
            }
        }
        [UnityTest] public IEnumerator PlayerActuallyEntersStairTrigger()
        {
            var start=BuildingMission.Stair(0,-1,true);BuildingMission.Warp(game.Player,start+Vector3.back*1.8f);game.Player.transform.rotation=Quaternion.identity;
            var movement=game.Player.GetComponent<FPSPlayerController>();
            for(int i=0;i<75&&!mission.Transitioning&&mission.CurrentFloor==0;i++){movement.Simulate(Vector2.up,false,false,.02f);yield return new WaitForFixedUpdate();}
            Assert.IsTrue(mission.Transitioning||mission.CurrentFloor==1,"walking into the trigger must start a transition");
            yield return new WaitForSeconds(.5f);Assert.AreEqual(1,mission.CurrentFloor);
        }
        [UnityTest] public IEnumerator NavMeshAgentTraversesRealCrossFloorLink()
        {
            var enemy=mission.Roster[0];var agent=enemy.GetComponent<NavMeshAgent>();
            Assert.IsTrue(agent.Warp(BuildingMission.Stair(0,-1,true)+Vector3.back));agent.speed=5;
            Assert.IsTrue(agent.SetDestination(BuildingMission.Stair(1,-1,false)+Vector3.forward*2));
            float deadline=Time.time+12;
            var destination=BuildingMission.Stair(1,-1,false)+Vector3.forward*2;
            while(Time.time<deadline&&(enemy.transform.position-destination).sqrMagnitude>1.5f)yield return null;
            Assert.AreEqual(1,BuildingMission.FloorAt(enemy.transform.position));Assert.Less(Vector3.Distance(enemy.transform.position,destination),1.5f);
        }
        [UnityTest] public IEnumerator SoundCarriesSnapshotAndContinuousNoiseDoesNotFreezeInvestigation()
        {
            var enemy=mission.Roster[0];var brain=enemy.GetComponent<BuildingEnemy>();var agent=enemy.GetComponent<NavMeshAgent>();
            agent.Warp(new Vector3(-21,0,9));brain.enabled=true;
            Vector3 heard=new Vector3(-21,0,13);
            for(int i=0;i<10;i++){NoiseBus.Emit(heard,30,NoiseKind.Gunshot);yield return new WaitForSeconds(.1f);}
            Assert.AreEqual(heard,brain.LastKnown);Assert.AreNotEqual(SweepEnemyState.HEARD_SOUND,brain.State);
            BuildingMission.Warp(game.Player,new Vector3(22,0,14));yield return new WaitForSeconds(.2f);
            Assert.AreEqual(heard,brain.LastKnown,"concealed player movement must not update the remembered source");
        }
        [UnityTest] public IEnumerator WallsAndFloorsBlockAimAndEnemyFire()
        {
            var enemy=mission.Roster[0];var agent=enemy.GetComponent<NavMeshAgent>();var assist=game.Player.GetComponent<AimAssistSystem>();
            var valid=typeof(AimAssistSystem).GetMethod("Valid",BindingFlags.NonPublic|BindingFlags.Instance);
            BuildingMission.Warp(game.Player,new Vector3(-22,0,-14));game.Player.transform.rotation=Quaternion.Euler(0,0,0);
            agent.Warp(new Vector3(-22,5,-6));yield return null;
            Assert.IsFalse((bool)valid.Invoke(assist,new object[]{enemy}));
            float hp=game.Player.Health;var combat=enemy.GetComponent<EnemyCombat>();combat.enabled=true;
            var data=ScriptableObject.CreateInstance<EnemyDefinition>();combat.Tick(game.Player,data,10);Assert.AreEqual(hp,game.Player.Health);
            agent.Warp(new Vector3(-14,0,-8));BuildingMission.Warp(game.Player,new Vector3(-14,0,0));
            Assert.IsTrue(Physics.Linecast(game.Player.transform.position+Vector3.up,enemy.AimPoint,LayerMask.GetMask("TZWorld","TZInteractable")));
            combat.Tick(game.Player,data,10);Assert.AreEqual(hp,game.Player.Health);Object.Destroy(data);
        }
        [UnityTest] public IEnumerator ClosedDoorBlocksRayAndOpeningRestoresPassage()
        {
            var door=Object.FindObjectsByType<BuildingDoor>().First(d=>d.Floor==0&&!d.Open);
            Vector3 p=door.transform.position;var normal=door.SlideDirection.x>.5f?Vector3.forward:Vector3.right;
            Assert.IsTrue(Physics.Linecast(p-normal,p+normal,LayerMask.GetMask("TZInteractable")));
            door.SetOpen(true);Physics.SyncTransforms();yield return null;
            Assert.IsTrue(door.Open);Assert.IsFalse(Physics.Linecast(p-normal,p+normal,LayerMask.GetMask("TZInteractable")));
        }
        [UnityTest] public IEnumerator AudioHasActualLeftRightPanningAndOcclusion()
        {
            var p=new Vector3(-22,0,0);BuildingMission.Warp(game.Player,p);game.Player.transform.rotation=Quaternion.identity;
            var clip=AudioClip.Create("pan probe",4410,1,44100,false);
            AudioService.Instance.PlayClip(clip,p+Vector3.left*2,1,true);
            var sources=AudioService.Instance.GetComponentsInChildren<AudioSource>();
            var left=sources.First(s=>s.clip==clip);Assert.AreEqual(1,left.spatialBlend);Assert.Less(game.Player.GetComponent<PlayerLook>().View.transform.InverseTransformPoint(left.transform.position).x,0);
            float clear=BuildingAcoustics.Attenuation(p,p+Vector3.forward*5,30);
            float behind=BuildingAcoustics.Attenuation(new Vector3(-14,0,0),new Vector3(-14,0,-8),30);Assert.Less(behind,clear);
            float upper=BuildingAcoustics.Attenuation(p,p+Vector3.up*5,30);Assert.Less(upper,clear);
            left.Stop();AudioService.Instance.PlayClip(clip,p+Vector3.right*2,1,true);
            Assert.Greater(game.Player.GetComponent<PlayerLook>().View.transform.InverseTransformPoint(sources.First(s=>s.clip==clip&&s.isPlaying).transform.position).x,0);
            Assert.LessOrEqual(sources.Length,16);Object.Destroy(clip);yield return null;
        }
        [UnityTest] public IEnumerator FixedRosterUnlocksOnlyAfterLastDeathAndRequiresThreeSeconds()
        {
            string key=mission.RecordKey;bool had=PlayerPrefs.HasKey(key);float prior=PlayerPrefs.GetFloat(key);
            mission.TickExtraction(20,mission.Definition.Exit,100);Assert.AreEqual(0,mission.ExtractionProgress);Assert.AreEqual(GamePhase.Playing,game.Phase);
            int total=mission.Total;
            for(int i=0;i<mission.Roster.Count-1;i++)mission.Roster[i].Apply(new DamageInfo(1000,Vector3.zero,"Test","Test"));
            Assert.AreEqual(1,mission.Remaining);mission.TickExtraction(5,mission.Definition.Exit,100);Assert.AreEqual(0,mission.ExtractionProgress);
            mission.Roster.Last().Apply(new DamageInfo(1000,Vector3.zero,"Test","Test"));
            Assert.AreEqual(0,mission.Remaining);Assert.AreEqual(total,mission.Total);Assert.AreEqual(BuildingStage.EXTRACTION_AVAILABLE,mission.Stage);Assert.AreEqual(GamePhase.Playing,game.Phase);
            mission.TickExtraction(2,mission.Definition.Exit,100);mission.TickExtraction(.1f,mission.Definition.Exit+Vector3.right*4,100);Assert.AreEqual(0,mission.ExtractionProgress);
            mission.TickExtraction(2.99f,mission.Definition.Exit,100);Assert.AreEqual(GamePhase.Playing,game.Phase);
            mission.TickExtraction(.02f,mission.Definition.Exit,100);Assert.AreEqual(GamePhase.Victory,game.Phase);Assert.AreEqual(total,game.Stats.kills);Assert.IsTrue(PlayerPrefs.HasKey(key));
            if(had)PlayerPrefs.SetFloat(key,prior);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();yield return null;
        }
        [UnityTest] public IEnumerator FourIndependentTouchesReleaseOnlyTheirOwnerAndClearResetsAll()
        {
            game.Input.ForceTouch=true;var ui=Object.FindAnyObjectByType<GameUI>();var canvas=ui.GetComponent<Canvas>();
            var nodes=new GameObject[4];var controls=new TouchControl[4];var actions=new[]{MobileAction.Move,MobileAction.Fire,MobileAction.Reload,MobileAction.Interact};
            var data=new PointerEventData[4];
            for(int i=0;i<4;i++){nodes[i]=new GameObject("touch test",typeof(RectTransform));nodes[i].transform.SetParent(canvas.transform);controls[i]=nodes[i].AddComponent<TouchControl>();controls[i].Configure(actions[i]);data[i]=new PointerEventData(EventSystem.current){pointerId=40+i,position=new Vector2(200,200)};controls[i].OnPointerDown(data[i]);}
            data[0].position=new Vector2(240,200);controls[0].OnDrag(data[0]);
            Assert.Greater(game.Input.Mobile.Move.x,0);Assert.IsTrue(game.Input.Mobile.Fire);Assert.IsTrue(game.Input.Mobile.Take(MobileAction.Reload));Assert.IsTrue(game.Input.Mobile.Take(MobileAction.Interact));
            controls[2].OnPointerUp(data[2]);Assert.IsTrue(game.Input.Mobile.Fire);Assert.Greater(game.Input.Mobile.Move.x,0);
            game.Input.Clear();Assert.IsFalse(game.Input.Mobile.Fire);Assert.AreEqual(Vector2.zero,game.Input.Mobile.Move);
            controls[1].OnPointerDown(data[1]);Assert.IsTrue(game.Input.Mobile.Fire,"old pointer ownership must be released");
            game.Input.Clear();foreach(var n in nodes)Object.Destroy(n);yield return null;
        }
        [UnityTest] public IEnumerator DeathFailsAndSecondRoundResetsState()
        {
            game.Player.Apply(new DamageInfo(10000,Vector3.zero,"Test","Test"));Assert.AreEqual(GamePhase.Defeat,game.Phase);Assert.AreEqual(BuildingStage.MISSION_FAILED,mission.Stage);
            Assert.IsFalse(game.Respawn());yield return Load(0);
            Assert.AreEqual(GamePhase.Playing,game.Phase);Assert.AreEqual(mission.Total,mission.Remaining);Assert.AreEqual(0,game.Stats.kills);Assert.AreEqual(0,mission.ExtractionProgress);Assert.IsFalse(game.Input.Mobile.Fire);
        }
        [UnityTest] public IEnumerator ClosingDoorDoesNotTrapAnActorInItsOpening()
        {
            var door=Object.FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None).First(d=>d.Floor==0);
            Vector3 opening=door.transform.position;door.SetOpen(true);
            var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;game.Player.transform.position=opening-Vector3.up*1.1f;cc.enabled=true;Physics.SyncTransforms();
            door.SetOpen(false);Assert.IsTrue(door.Open,"Occupied doorway must stay clear even when leaf is retracted");
            cc.enabled=false;game.Player.transform.position=mission.Definition.Spawn+Vector3.up*.1f;cc.enabled=true;Physics.SyncTransforms();
            door.SetOpen(false);Assert.IsFalse(door.Open);yield return null;
        }
    }
}
