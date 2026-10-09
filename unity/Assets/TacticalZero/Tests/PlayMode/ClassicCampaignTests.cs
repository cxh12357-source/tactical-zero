using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class ClassicCampaignTests
    {
        int priorRole,priorMap,priorDifficulty,priorStash;bool priorAim;
        [UnitySetUp] public IEnumerator Setup()
        {
            priorRole=ClassicRules.RoleIndex;priorMap=ClassicRules.MapIndex;priorDifficulty=ClassicRules.DifficultyIndex;priorAim=ClassicRules.AutoAim;priorStash=PlayerPrefs.GetInt("TZ3D.Stash",0);
            ClassicRules.RoleIndex=0;ClassicRules.MapIndex=0;ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ClassicRules.RoleIndex=priorRole;ClassicRules.MapIndex=priorMap;ClassicRules.DifficultyIndex=priorDifficulty;ClassicRules.AutoAim=priorAim;PlayerPrefs.SetInt("TZ3D.Stash",priorStash);PlayerPrefs.Save();
            GameManager.Ensure().MainMenu();yield return null;yield return null;
        }
        void FreezeEnemies(){foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}}
        [UnityTest] public IEnumerator EveryMapAndDifficultySpawnsReachableDistinctCampaign()
        {
            for(int i=0;i<4;i++)
            {
                ClassicRules.MapIndex=i;ClassicRules.DifficultyIndex=i;ClassicRules.RoleIndex=i;
                yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;
                var game=GameManager.Instance;var mission=ClassicEncounter.Instance;
                Assert.AreEqual(ClassicRules.Difficulty.count,EnemyHealth.Active.Count);
                Assert.AreEqual(ClassicRules.Map.id,mission.MapId);
                var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(game.Player.transform.position,mission.Extraction,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);
                foreach(var enemy in EnemyHealth.Active){Assert.Greater(Vector3.Distance(enemy.transform.position,game.Player.transform.position),5);Assert.IsNotNull(enemy.transform.Find("Detailed operator"));}
                FreezeEnemies();
                if(i==0)Capture("classic-warehouse-gameplay");
            }
        }
        [UnityTest] public IEnumerator TwelveWeaponsCanBeCollectedAndLimitedAmmoCannotBeRefilled()
        {
            FreezeEnemies();var weapons=GameManager.Instance.Player.GetComponent<WeaponController>();Assert.AreEqual("USP",weapons.Current.Id);
            foreach(var gun in ClassicRules.Data.weapons)
            {
                if(gun.id=="USP")continue;
                Assert.IsTrue(weapons.TakeWeapon(gun.id));Assert.AreEqual(gun.id,weapons.Current.Id);Assert.AreEqual(gun.mag,weapons.Ammo.Loaded);Assert.AreEqual(gun.reserve,weapons.Ammo.Reserve);
                if(gun.limited)Assert.IsFalse(weapons.SupplyCurrent());
                int vertices=0;foreach(var mesh in weapons.Current.Model.GetComponentsInChildren<MeshFilter>())if(mesh.sharedMesh)vertices+=mesh.sharedMesh.vertexCount;
                Assert.Greater(vertices,500,"Original detailed weapon geometry: "+gun.id);
                yield return null;
            }
        }
        [UnityTest] public IEnumerator RailPiercesThreeActorsAndNovaExplosionDamagesNearbyActors()
        {
            FreezeEnemies();IsolatedRange();var game=GameManager.Instance;var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;game.Player.transform.position=new Vector3(980,.1f,974.5f);cc.enabled=true;
            foreach(var loot in Object.FindObjectsByType<ClassicLoot>(FindObjectsSortMode.None))loot.gameObject.SetActive(false);
            var enemies=new System.Collections.Generic.List<EnemyHealth>(EnemyHealth.Active);
            for(int i=0;i<3;i++)enemies[i].transform.position=new Vector3(985+i*4,0,974.5f);
            game.Player.transform.rotation=Quaternion.Euler(0,90,0);var view=game.Player.GetComponent<PlayerLook>().View;view.transform.LookAt(enemies[0].AimPoint);Physics.SyncTransforms();
            var weapons=game.Player.GetComponent<WeaponController>();weapons.TakeWeapon("RAIL");yield return new WaitForSeconds(.3f);view.transform.position=new Vector3(980,1,974.5f);view.transform.LookAt(enemies[0].AimPoint);
            Assert.IsTrue(weapons.TryShoot(true));foreach(var enemy in enemies)Assert.Less(enemy.Health,75,"Rail must affect all three collinear targets: "+enemy.name+" at "+enemy.transform.position);
            float before=enemies[2].Health;NovaProjectile.Launch(enemies[2].AimPoint+Vector3.left*.4f,Vector3.right,ClassicRules.Gun("NOVA"),game.Player.gameObject);
            yield return new WaitForSeconds(.25f);Assert.Less(enemies[2].Health,before);
        }
        [UnityTest] public IEnumerator FullBagRejectsLootAndClearExtractionBanksOnlyOnSuccess()
        {
            FreezeEnemies();var mission=ClassicEncounter.Instance;var game=GameManager.Instance;
            for(int i=0;i<4;i++)Assert.IsTrue(mission.AddContents("tech"));Assert.AreEqual(8,mission.Bag.Count);Assert.IsFalse(mission.AddContents("tools"));
            int before=mission.Stored;Assert.AreEqual(priorStash,before);
            for(int stage=0;stage<3;stage++){mission.Operation.Advance(35,true,false);mission.Operation.Advance(45,false,false);}FreezeEnemies();
            typeof(ClassicEncounter).GetField("elapsed",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(mission,301f);
            foreach(var enemy in new System.Collections.Generic.List<EnemyHealth>(EnemyHealth.Active))enemy.Apply(new DamageInfo(1000,Vector3.zero,"娴嬭瘯","USP"));
            var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;game.Player.transform.position=mission.Extraction+Vector3.up*.1f;cc.enabled=true;
            yield return new WaitForSecondsRealtime(5.7f);
            Assert.AreEqual(GamePhase.Victory,game.Phase);Assert.AreEqual(before+mission.BagValue,mission.Stored);
        }
        [UnityTest] public IEnumerator DeploymentUsesEightPortraitsAndRealModelPreview()
        {
            GameManager.Instance.MainMenu();yield return null;yield return null;yield return null;
            foreach(var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
                if(button.name=="原战区行动"){button.onClick.Invoke();break;}
            yield return null;yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<BuildingDeploymentUI>());
            int portraitCount=0;foreach(var image in Object.FindObjectsByType<UnityEngine.UI.RawImage>(FindObjectsSortMode.None))if(image.texture&&image.texture.name.Contains("assault")||image.texture&&System.Array.Exists(ClassicRules.Data.roles,r=>r.id==image.texture.name))portraitCount++;
            Assert.AreEqual(8,portraitCount);Capture("classic-deployment");yield return null;
        }
        [UnityTest] public IEnumerator AutoAimAcquiresVisibleBodyButNeverShootsThroughWalls()
        {
            FreezeEnemies();IsolatedRange();ClassicRules.AutoAim=true;var player=GameManager.Instance.Player;
            foreach(var loot in Object.FindObjectsByType<ClassicLoot>(FindObjectsSortMode.None))loot.gameObject.SetActive(false);
            var enemies=new System.Collections.Generic.List<EnemyHealth>(EnemyHealth.Active);
            for(int i=0;i<enemies.Count;i++)enemies[i].transform.position=i==0?new Vector3(973,0,974.5f):new Vector3(1020+i*3,0,1020);
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.1f,974.5f);cc.enabled=true;
            yield return new WaitForSeconds(.3f); // Re-establish floor contact after the test teleport.
            var view=player.GetComponent<PlayerLook>().View;view.transform.rotation=Quaternion.Euler(0,70,0);Physics.SyncTransforms();
            var assist=player.GetComponent<AimAssistSystem>();Vector3 forward=view.transform.forward;
            Assert.Less(Vector3.Angle(assist.Magnet(forward,50),enemies[0].AimPoint-view.transform.position),.1f);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LayerMask.NameToLayer("TZWorld");wall.transform.position=new Vector3(970,1,974.5f);wall.transform.localScale=new Vector3(1,3,3);Physics.SyncTransforms();
            Assert.Less(Vector3.Angle(assist.Magnet(forward,50),forward),.01f);Object.Destroy(wall);yield return null;
        }
        [UnityTest] public IEnumerator AirdropLandsBeforeLimitedWeaponCanBeCollected()
        {
            FreezeEnemies();var mission=ClassicEncounter.Instance;var player=GameManager.Instance.Player;
            var crate=mission.SpawnLoot(player.transform.position+Vector3.right*4,"绌烘姇娴嬭瘯","weapon","CYCLONE");crate.BeginDrop();
            Assert.IsFalse(crate.CanInteract(player,out _));Assert.IsFalse(crate.Interact(player));
            yield return new WaitForSeconds(6.1f);
            Assert.IsTrue(crate.Interact(player));Assert.AreEqual("CYCLONE",player.GetComponent<WeaponController>().Current.Id);Assert.IsFalse(player.GetComponent<WeaponController>().SupplyCurrent());
        }
        internal static void IsolatedRange()
        {
            // Weapon rules are tested on a dedicated range, independent of authored map lanes.
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Isolated weapon test range";floor.layer=LayerMask.NameToLayer("TZWorld");floor.transform.position=new Vector3(1000,-.25f,1000);floor.transform.localScale=new Vector3(140,.5f,140);Physics.SyncTransforms();
        }
        internal static void Capture(string name,int width=1280,int height=720,System.Action inspect=null)
        {
            Directory.CreateDirectory("TestResults");var camera=Camera.main;Assert.IsNotNull(camera);
            var canvas=Object.FindAnyObjectByType<GameUI>().GetComponent<Canvas>();var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;float previousDistance=canvas.planeDistance;
            var rt=new RenderTexture(width,height,24);var previousTarget=camera.targetTexture;var previous=RenderTexture.active;Texture2D texture=null;
            try
            {
                camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.01f;Canvas.ForceUpdateCanvases();inspect?.Invoke();camera.Render();
                RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes("TestResults/"+name+".png",texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=previousTarget;canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;canvas.planeDistance=previousDistance;rt.Release();Object.Destroy(rt);if(texture)Object.Destroy(texture);
            }
        }
    }
}
