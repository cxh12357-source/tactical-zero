using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class Contact16Tests
    {
        string settings,progress;int role,map,difficulty;GameManager game;
        [UnitySetUp]public IEnumerator Setup()
        {
            settings=PlayerPrefs.GetString(SaveService.SettingsKey,"{}");progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;game=GameManager.Instance;
            foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<EnemyCombat>().enabled=false;}
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(game){game.Input.Clear();game.Input.ForceTouch=false;game.MainMenu();}ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;PlayerPrefs.SetString(SaveService.SettingsKey,settings);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();yield return null;yield return null;
        }
        [UnityTest]public IEnumerator ContactHandsRemainAttachedThroughoutEachReload()
        {
            var guns=game.Player.GetComponent<WeaponController>();
            foreach(var id in new[]{"USP","M4A1","AK47","MP6","VECTOR","DMR","AWP","RPK","Shotgun","P90","MG3","M700","M1014"})
            {
                if(id=="USP")guns.Switch(0);else Assert.IsTrue(guns.TakeWeapon(id));yield return new WaitForSeconds(.4f);
                var model=game.Player.GetComponentInChildren<WeaponReloadAnimation>();Assert.IsTrue(model.ContactGrip,id);
                var arms=model.GetComponentsInChildren<FirstPersonArm>();Assert.AreEqual(2,arms.Length);
                foreach(var arm in arms){var skin=arm.GetComponentInChildren<SkinnedMeshRenderer>();Assert.GreaterOrEqual(skin.bones.Length,17);Assert.GreaterOrEqual(skin.sharedMesh.GetBlendShapeIndex("ReleaseGrip"),0);}
                foreach(var arm in arms)Assert.Less(Vector3.Distance(arm.Hand.position,arm.Target.position),.115f,id+" firing grip");
                if(id=="M4A1")
                {
                    var left=arms.First(a=>a.Left);var finger=left.GetComponentsInChildren<Transform>().First(t=>t.name=="digit3_2.L");var point=model.transform.InverseTransformPoint(finger.position);
                    Assert.That(point.x,Is.InRange(-.015f,.065f),"Support fingers must wrap across the handguard, rather than float outside it");
                }
                Assert.IsTrue(guns.TryShoot(),id+" ready to shoot");
                if(guns.Ammo.Reserve==0){Assert.IsFalse(guns.Ammo.StartReload(guns.Current.ReloadSeconds),"Limited AWP retains its no-reserve rule");continue;}
                Assert.IsTrue(guns.Ammo.StartReload(guns.Current.ReloadSeconds),id+" reload begins");
                for(int p=0;p<8;p++)
                {
                    guns.Ammo.Tick(guns.Current.ReloadSeconds*.10f);yield return null;yield return null;
                    foreach(var arm in arms)Assert.Less(Vector3.Distance(arm.Hand.position,arm.Target.position),.115f,id+" phase "+p);
                    if(id=="MG3"&&(p==3||p==4))
                    {
                        var support=arms.First(a=>a.Left);var belt=model.Belt.GetComponentInChildren<Renderer>().bounds;
                        Assert.Less(Vector3.Distance(support.Hand.position,belt.center),.15f,"MG3 support hand must accompany the raised ammunition belt");
                        var cover=model.Cover.GetComponentInChildren<Renderer>().bounds;
                        Assert.Greater(model.transform.InverseTransformPoint(cover.center).y,.12f,"MG3 feed cover must open above the receiver");
                    }
                }
                ClassicCampaignTests.Capture("contact16-reload-"+id,1600,900);guns.Ammo.Tick(30);yield return null;
                Assert.AreEqual(guns.Ammo.Capacity,guns.Ammo.Loaded);Assert.AreEqual(0,model.GripRelease,.001f);
            }
        }
        [UnityTest]public IEnumerator IdleAndWalkingOperatorsMaintainGroundContact()
        {
            var enemy=EnemyHealth.Active[0];var agent=enemy.GetComponent<NavMeshAgent>();agent.ResetPath();
            var foot=enemy.GetComponentInChildren<FootContact16>();Assert.IsNotNull(foot);
            yield return new WaitForSeconds(.3f);Assert.AreEqual(2,foot.PlantedFeet);Assert.Less(foot.MaximumPlantDrift,.16f);
            Vector3 start=enemy.transform.position;var candidates=Object.FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None).OrderBy(p=>Vector3.Distance(p.transform.position,start)).ToArray();
            var route=candidates.First(p=>Vector3.Distance(p.transform.position,start)>2);
            agent.SetDestination(route.transform.position);yield return new WaitForSeconds(2);
            Assert.Greater(Vector3.Distance(start,enemy.transform.position),.2f);Assert.Less(foot.MaximumPlantDrift,.24f);
            foreach(var t in foot.GetComponentsInChildren<Transform>())if(t.name=="foot.L"||t.name=="foot.R")Assert.IsFalse(float.IsNaN(t.position.y));
        }
        [UnityTest]public IEnumerator FurnishedRoomsPreserveConnectedObjectiveRoutes()
        {
            foreach(var id in new[]{"sector07","docks","ruins","outpost"})
            {
                if(id!="sector07"){yield return SceneManager.LoadSceneAsync("Classic_"+id);yield return null;yield return null;game=GameManager.Instance;}
                var detail=GameObject.Find("Contact architecture 16");Assert.IsNotNull(detail);Assert.GreaterOrEqual(detail.GetComponentsInChildren<Transform>().Count(t=>t.name=="Contact detail Vent"),8);
                var terminal=GameObject.Find("Relay terminal");Assert.IsNotNull(terminal);
                var terminalMaterial=terminal.GetComponent<Renderer>().sharedMaterial;
                Assert.IsNotNull(terminalMaterial,"Active communication terminal needs a shipped material");
                Assert.IsTrue(terminalMaterial.shader.isSupported,"Communication terminal shader must render in URP");
                foreach(var prop in detail.GetComponentsInChildren<Transform>().Where(t=>t.name=="Contact detail Desk"||t.name=="Contact detail Door"||t.name=="Contact detail Lockers"))
                {
                    var visible=prop.GetComponentInChildren<Renderer>();Assert.IsNotNull(visible);Assert.IsTrue(visible.enabled);Assert.Greater(visible.bounds.size.y,prop.name.Contains("Desk")?.65f:1.7f,prop.name+" visible height");Assert.Greater(visible.bounds.size.magnitude,1,prop.name+" metre-scale geometry");
                }
                var points=Object.FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None);Assert.GreaterOrEqual(points.Length,20);var path=new NavMeshPath();
                Assert.IsTrue(NavMesh.SamplePosition(points[0].transform.position,out var first,1.2f,NavMesh.AllAreas));
                foreach(var point in points){Assert.IsTrue(NavMesh.SamplePosition(point.transform.position,out var hit,1.2f,NavMesh.AllAreas));Assert.IsTrue(NavMesh.CalculatePath(first.position,hit.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
            }
        }
    }
}
