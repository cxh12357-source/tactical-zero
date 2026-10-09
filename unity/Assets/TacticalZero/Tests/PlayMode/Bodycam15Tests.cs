using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class Bodycam15Tests
    {
        string settings,progress;int role,map,difficulty;GameManager game;
        [UnitySetUp]public IEnumerator Setup()
        {
            settings=PlayerPrefs.GetString(SaveService.SettingsKey,"{}");progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;game=GameManager.Instance;game.Settings.quality=2;SaveService.Apply();Freeze();
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(game){game.Input.Clear();game.Input.ForceTouch=false;game.MainMenu();}ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;PlayerPrefs.SetString(SaveService.SettingsKey,settings);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();yield return null;yield return null;
        }
        static void Freeze(){foreach(var e in EnemyHealth.Active){e.GetComponent<EnemyBrain>().enabled=false;e.GetComponent<NavMeshAgent>().enabled=false;}}
        void Place(Vector3 at,Vector3 target)
        {
            var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;if(NavMesh.SamplePosition(at,out var hit,3,NavMesh.AllAreas))at=hit.position;game.Player.transform.position=at+Vector3.up*.05f;cc.enabled=true;
            var look=game.Player.GetComponent<PlayerLook>();look.enabled=false;look.View.transform.localPosition=new Vector3(0,1.6f,0);game.Player.transform.rotation=Quaternion.identity;look.View.transform.rotation=Quaternion.LookRotation(target-look.View.transform.position);Physics.SyncTransforms();
        }
        [UnityTest]public IEnumerator FourMapsRenderPhotographicSurfacesAndIndoorExposure()
        {
            foreach(string id in new[]{"sector07","docks","ruins","outpost"})
            {
                if(id!="sector07"){yield return SceneManager.LoadSceneAsync("Classic_"+id);yield return null;yield return null;game=GameManager.Instance;Freeze();}
                game.Settings.quality=2;SaveService.Apply();var style=Object.FindAnyObjectByType<SceneVisualStyle>();Assert.IsTrue(style.BodyCamera);Assert.IsNotNull(RenderSettings.customReflectionTexture);
                Assert.IsTrue(style.Grading.sharedProfile.TryGet<LensDistortion>(out var lens));Assert.Less(lens.intensity.value,-.1f);
                Assert.Greater(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(r=>r.sharedMaterial&&r.sharedMaterial.shader.name=="TacticalZero/Scanned Surface"),30);
                game.Player.GetComponent<WeaponController>().TakeWeapon("M700");Place(new Vector3(-14,0,-13),new Vector3(1,1.8f,6));yield return new WaitForSeconds(1.0f);ClassicCampaignTests.Capture("bodycam15-"+id+"-exterior",1600,900);Clean("bodycam15-"+id+"-clean");
                var area=Object.FindObjectsByType<MapArea>(FindObjectsSortMode.None).First(a=>a.Interior&&a.Size.x>=6);
                Place(area.transform.TransformPoint(new Vector3(0,0,1.5f)),area.transform.TransformPoint(new Vector3(1,1.55f,-area.Size.z)));yield return new WaitForSeconds(1.5f);ClassicCampaignTests.Capture("bodycam15-"+id+"-interior",1600,900);
            }
        }
        static void Clean(string name)
        {
            var canvas=Object.FindAnyObjectByType<GameUI>().GetComponent<Canvas>();bool previous=canvas.enabled;
            try{canvas.enabled=false;ClassicCampaignTests.Capture(name,1600,900);}finally{canvas.enabled=previous;}
        }
        [UnityTest]public IEnumerator AnatomicalArmsFollowReloadTargetsWithoutDetaching()
        {
            Place(new Vector3(-14,0,-13),new Vector3(1,1.6f,6));var guns=game.Player.GetComponent<WeaponController>();
            foreach(var id in new[]{"P90","MG3","M700","M1014","M4A1","USP"})
            {
                if(id=="USP")guns.Switch(0);else Assert.IsTrue(guns.TakeWeapon(id));yield return new WaitForSeconds(.3f);
                var model=game.Player.GetComponentInChildren<WeaponReloadAnimation>();var arms=model.GetComponentsInChildren<FirstPersonArm>();Assert.AreEqual(2,arms.Length,id);
                foreach(var arm in arms){Assert.IsNotNull(arm.Upper);Assert.IsNotNull(arm.Hand);Assert.IsTrue(arm.GetComponentInChildren<SkinnedMeshRenderer>().enabled);}
                guns.TryShoot();Assert.IsTrue(guns.Ammo.StartReload(guns.Current.ReloadSeconds));guns.Ammo.Tick(guns.Current.ReloadSeconds*.36f);yield return new WaitForSeconds(.1f);
                foreach(var arm in arms)Assert.Less(Vector3.Distance(arm.Hand.position,arm.Target.position),.10f,id+" wrist target");
                ClassicCampaignTests.Capture("bodycam15-reload-"+id,1600,900);guns.Ammo.Tick(20);yield return null;
                Assert.AreEqual(guns.Ammo.Capacity,guns.Ammo.Loaded);
            }
            game.Input.ForceTouch=true;game.Input.Mobile.Aim=true;yield return new WaitForSeconds(.7f);Assert.IsTrue(guns.SightEngaged);Assert.AreEqual(WeaponSight.For("USP").Fov,Camera.main.fieldOfView,.5f);ClassicCampaignTests.Capture("bodycam15-ads",1600,900);game.Input.Mobile.Aim=false;
        }
        [UnityTest]public IEnumerator DeadOperatorsBecomeStableArticulatedBodies()
        {
            var enemy=EnemyHealth.Active[0];var model=enemy.GetComponentInChildren<OperatorRig>();Assert.IsNotNull(model);
            enemy.Apply(new DamageInfo(10000,enemy.transform.position-Vector3.forward,"QA","M700"));yield return new WaitForSeconds(1);
            var ragdoll=model.GetComponent<OperatorRagdoll>();Assert.IsNotNull(ragdoll);Assert.IsFalse(model.enabled);var bodies=ragdoll.GetComponentsInChildren<Rigidbody>();Assert.GreaterOrEqual(bodies.Length,10);
            var dropped=model.GetComponentInChildren<WeaponReloadAnimation>();Assert.IsNotNull(dropped.GetComponent<Rigidbody>(),"The held gun must fall with the operator");
            foreach(var body in bodies){Assert.IsFalse(float.IsNaN(body.position.y));Assert.Less(body.linearVelocity.magnitude,30);}
            yield return new WaitForSeconds(5.3f);foreach(var body in bodies)Assert.IsTrue(body.isKinematic);
        }
    }
}
