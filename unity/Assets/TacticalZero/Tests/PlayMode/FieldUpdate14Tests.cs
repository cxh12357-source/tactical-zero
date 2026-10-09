using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TacticalZero.Tests
{
    public sealed class FieldUpdate14Tests
    {
        GameManager game;int role,map,difficulty,stash;bool aim;string progress;
        [UnitySetUp]public IEnumerator Setup()
        {
            role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;aim=ClassicRules.AutoAim;stash=PlayerPrefs.GetInt("TZ3D.Stash",0);progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;game=GameManager.Instance;Freeze();
        }
        static void Freeze(){foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}}
        static void Clear(){foreach(var enemy in new List<EnemyHealth>(EnemyHealth.Active))enemy.Apply(new DamageInfo(10000,Vector3.zero,"QA","USP"));}
        [UnityTearDown]public IEnumerator Cleanup()
        {
            ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;ClassicRules.AutoAim=aim;PlayerPrefs.SetInt("TZ3D.Stash",stash);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();if(game)game.MainMenu();yield return null;yield return null;
        }
        [UnityTest]public IEnumerator MovementStopsAndReversesWithinOneFrame()
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.layer=LayerMask.NameToLayer("TZWorld");floor.transform.position=new Vector3(1000,-.5f,1000);floor.transform.localScale=new Vector3(30,1,30);
            var p=game.Player;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(1000,.03f,1000);p.transform.rotation=Quaternion.identity;cc.enabled=true;var motor=p.GetComponent<FPSPlayerController>();motor.enabled=false;motor.ResetMotion();Physics.SyncTransforms();
            for(int i=0;i<10;i++)motor.Simulate(Vector2.up,false,false,1f/60);
            Assert.Greater(motor.Speed,4);Vector3 before=p.transform.position;motor.Simulate(Vector2.zero,false,false,1f/60);Assert.Less(Mathf.Abs(p.transform.position.z-before.z),.001f,"No coasting after release");
            motor.Simulate(Vector2.up,false,false,1f/60);before=p.transform.position;motor.Simulate(Vector2.down,false,false,1f/60);Assert.Less(p.transform.position.z,before.z);
            Object.Destroy(floor);yield return null;
        }
        [UnityTest]public IEnumerator SuppliesAndNewWeaponsWorkAndReloadPartsMove()
        {
            Assert.AreEqual(CameraClearFlags.Skybox,game.Player.GetComponent<PlayerLook>().View.clearFlags);Assert.IsNotNull(RenderSettings.customReflectionTexture);
            var mission=ClassicEncounter.Instance;foreach(var loot in mission.Supplies){Assert.AreNotEqual("equipment",loot.Kind);Assert.AreNotEqual("ammo",loot.Kind);if(loot.Kind=="weapon")Assert.AreNotEqual("airdrop",ClassicRules.Gun(loot.WeaponId).tier);}
            Assert.IsNull(mission.SpawnLoot(Vector3.zero,"shield","equipment","Shield"));Assert.IsNull(mission.SpawnLoot(Vector3.zero,"ammo","ammo",""));
            var guns=game.Player.GetComponent<WeaponController>();
            foreach(string id in new[]{"P90","MG3","M700","M1014"})
            {
                Assert.IsTrue(guns.TakeWeapon(id));yield return new WaitForSeconds(.3f);Assert.IsTrue(guns.TryShoot());Assert.AreEqual(guns.Current.Magazine-1,guns.Ammo.Loaded);
                var model=game.Player.GetComponentInChildren<WeaponReloadAnimation>();Assert.IsNotNull(model);Assert.AreEqual(id,model.WeaponId);Assert.IsNotNull(model.LeftHand);
                Assert.IsTrue(guns.Ammo.StartReload(guns.Current.ReloadSeconds));var hand=model.LeftHand.localPosition;model.Pose(.4f);Assert.Greater((hand-model.LeftHand.localPosition).magnitude,.05f);model.Pose(-1);Assert.Less((hand-model.LeftHand.localPosition).magnitude,.001f);
                guns.Ammo.Tick(guns.Current.ReloadSeconds*.38f);yield return new WaitForSeconds(.16f);ClassicCampaignTests.Capture("field14-reload-"+id);
                guns.Ammo.Tick(guns.Current.ReloadSeconds+1);Assert.AreEqual(guns.Current.Magazine,guns.Ammo.Loaded);
                yield return new WaitForSeconds(.1f);ClassicCampaignTests.Capture("field14-first-person-"+id);
            }
        }
        [UnityTest]public IEnumerator ThreeConnectedObjectivesGateExtractionAndShowClearAnnouncement()
        {
            var mission=ClassicEncounter.Instance;var operation=mission.Operation;Assert.IsNotNull(operation);
            for(int i=0;i<3;i++)
            {
                var route=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(game.Player.transform.position,operation.Target,NavMesh.AllAreas,route));Assert.AreEqual(NavMeshPathStatus.PathComplete,route.status);
                operation.Advance(35,true,true);Assert.AreEqual(0,operation.Capture,"Nearby enemies contest capture");operation.Advance(35,true,false);Assert.IsTrue(operation.Uploading);operation.Advance(45,false,false);Assert.AreEqual(i+1,operation.Stage);Freeze();
            }
            Assert.IsFalse(operation.ReadyForExtraction);typeof(ClassicEncounter).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(mission,301f);Clear();yield return null;yield return null;
            Assert.IsTrue(operation.ReadyForExtraction);var title=GameObject.Find("Clear title");Assert.IsNotNull(title);Assert.IsTrue(title.activeInHierarchy);ClassicCampaignTests.Capture("field14-clear-announcement");
            var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;game.Player.transform.position=mission.Extraction+Vector3.up*.06f;cc.enabled=true;Physics.SyncTransforms();yield return new WaitForSecondsRealtime(5.7f);Assert.AreEqual(GamePhase.Victory,game.Phase);
        }
        [UnityTest]public IEnumerator TenMinuteDeadlineEndsTheRound()
        {
            typeof(ClassicEncounter).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ClassicEncounter.Instance,600f);yield return null;Assert.AreEqual(GamePhase.Defeat,game.Phase);
        }
    }
}


