using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace TacticalZero.Tests
{
    public sealed class HudVisualTests
    {
        GameManager game;int quality,role,map,difficulty;bool aim;string progress;
        [UnitySetUp]public IEnumerator Setup()
        {
            quality=SaveService.Settings.quality;role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;aim=ClassicRules.AutoAim;progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;game=GameManager.Instance;game.Input.ForceTouch=true;game.Input.Mobile.Clear();Freeze();game.Settings.quality=2;SaveService.Apply();yield return new WaitForSeconds(.35f);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            game.Input.ForceTouch=false;game.Input.Clear();game.Settings.quality=quality;SaveService.Apply();ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;ClassicRules.AutoAim=aim;
            PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();game.MainMenu();yield return null;yield return null;
        }
        static void Freeze(){foreach(var e in EnemyHealth.Active){e.GetComponent<EnemyBrain>().enabled=false;e.GetComponent<NavMeshAgent>().enabled=false;}}
        static Text Text(string name)=>GameObject.Find(name).GetComponent<Text>();
        static RectTransform Rect(string name)=>GameObject.Find(name).GetComponent<RectTransform>();
        [UnityTest]public IEnumerator HealthArmorAmmoAndReloadReflectActualCombatState()
        {
            var p=game.Player;var guns=p.GetComponent<WeaponController>();guns.TakeWeapon("M4A1");yield return new WaitForSeconds(.3f);
            Assert.IsTrue(guns.TryShoot());yield return new WaitForSeconds(.15f);Assert.AreEqual(guns.Ammo.Loaded.ToString(),Text("Loaded ammo").text);Assert.AreEqual("/ "+guns.Ammo.Reserve,Text("Reserve ammo").text);
            p.Apply(new DamageInfo(80,Vector3.zero,"测试守卫","AK47"));yield return null;
            Assert.AreEqual(p.Health*3.76f,Rect("Health fill").sizeDelta.x,.05f);Assert.Greater(Rect("Damage trail").sizeDelta.x,Rect("Health fill").sizeDelta.x);
            yield return new WaitForSeconds(.15f);Assert.AreEqual("20",Text("Health").text);Assert.Greater(Rect("Health fill").GetComponent<Image>().color.r,.9f);
            Assert.IsTrue(p.AddArmor(50));yield return null;Assert.AreEqual(188,Rect("Armor fill").sizeDelta.x,.1f);
            Assert.IsTrue(guns.Ammo.StartReload(guns.Current.ReloadSeconds));yield return new WaitForSeconds(.15f);StringAssert.Contains("装填",Text("Weapon state").text);Assert.Greater(Rect("Reload fill").sizeDelta.x,0);
            ClassicCampaignTests.Capture("hud10-low-health-reload");
            p.Heal(40);yield return null;Assert.AreEqual(60*3.76f,Rect("Health fill").sizeDelta.x,.1f);
        }
        [UnityTest]public IEnumerator HudFitsCommonAspectRatiosAndRemainsVisibleOverScope()
        {
            var p=game.Player;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=new Vector3(-33,.1f,-25);cc.enabled=true;p.transform.rotation=Quaternion.Euler(0,65,0);p.GetComponent<PlayerLook>().ResetView();
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,810)})
                ClassicCampaignTests.Capture("hud10-layout-"+size.x+"x"+size.y,size.x,size.y,()=>CheckLayout(Camera.main,size));
            var guns=p.GetComponent<WeaponController>();guns.TakeWeapon("AWP");game.Input.ForceTouch=true;game.Input.Mobile.Aim=true;yield return new WaitForSeconds(.55f);
            Assert.IsTrue(guns.ScopeActive);Assert.IsTrue(GameObject.Find("Health fill").activeInHierarchy);
            var status=Rect("Battle status");Assert.Greater(status.GetSiblingIndex(),status.parent.Find("Weapon optic").GetSiblingIndex());
            ClassicCampaignTests.Capture("hud10-scope");Assert.AreEqual(0,Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length);
        }
        static void CheckLayout(Camera camera,Vector2Int size)
        {
            var names=new[]{"Vitals","Ammunition","Minimap frame","Compass","Match counters","Skill cooldown"};
            var bounds=new Rect[names.Length];var corners=new Vector3[4];
            for(int i=0;i<names.Length;i++)
            {
                Rect(names[i]).GetWorldCorners(corners);Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),max=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);bounds[i]=UnityEngine.Rect.MinMaxRect(min.x,min.y,max.x,max.y);
                Assert.GreaterOrEqual(min.x,-1,names[i]);Assert.GreaterOrEqual(min.y,-1,names[i]);Assert.LessOrEqual(max.x,size.x+1,names[i]);Assert.LessOrEqual(max.y,size.y+1,names[i]);
                for(int j=0;j<i;j++)Assert.IsFalse(bounds[i].Overlaps(bounds[j]),names[i]+" overlaps "+names[j]);
            }
        }
        [UnityTest]public IEnumerator FourMapsHaveDistinctAtmospheresAndQualityControlsExpensiveEffects()
        {
            var profiles=new System.Collections.Generic.HashSet<UnityEngine.Rendering.VolumeProfile>();
            for(int m=0;m<4;m++)
            {
                game.Input.ForceTouch=false;ClassicRules.MapIndex=m;yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;Freeze();game.Input.ForceTouch=true;game.Input.Mobile.Clear();
                var style=Object.FindFirstObjectByType<SceneVisualStyle>();Assert.IsNotNull(style);Assert.IsNotNull(RenderSettings.skybox);Assert.IsTrue(RenderSettings.skybox.shader.isSupported);Assert.IsNotNull(RenderSettings.skybox.GetTexture("_Tex"));profiles.Add(style.Grading.sharedProfile);
                game.Settings.quality=2;SaveService.Apply();yield return new WaitForSeconds(.3f);
                Assert.IsTrue(style.Grading.enabled);if(style.ContactShadows)Assert.IsTrue(style.ContactShadows.isActive);Assert.IsTrue(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing);
                Assert.AreEqual(LightShadows.Soft,RenderSettings.sun.shadows);ClassicCampaignTests.Capture("hud10-world-"+ClassicRules.Map.id);
                game.Settings.quality=0;SaveService.Apply();yield return null;
                Assert.IsFalse(style.Grading.enabled);if(style.ContactShadows)Assert.IsFalse(style.ContactShadows.isActive);Assert.AreEqual(LightShadows.None,RenderSettings.sun.shadows);
            }
            Assert.AreEqual(4,profiles.Count);game.Settings.quality=2;SaveService.Apply();
        }
    }
}
