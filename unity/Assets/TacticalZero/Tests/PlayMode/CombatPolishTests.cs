using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class CombatPolishTests
    {
        GameManager game;PlayerHealth player;int priorRole,priorMap,priorDifficulty,priorQuality;bool priorAim;string priorProgress;
        [UnitySetUp] public IEnumerator Setup()
        {
            priorRole=ClassicRules.RoleIndex;priorMap=ClassicRules.MapIndex;priorDifficulty=ClassicRules.DifficultyIndex;priorAim=ClassicRules.AutoAim;
            priorProgress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.RoleIndex=0;ClassicRules.MapIndex=0;ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
            ClassicCampaignTests.IsolatedRange();
            game=GameManager.Instance;player=game.Player;priorQuality=game.Settings.quality;game.Settings.quality=2;SaveService.Apply();
            foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}
            foreach(var loot in new List<ClassicLoot>(ClassicEncounter.Instance.Supplies)){ClassicEncounter.Instance.ReleaseSupply(loot);Object.Destroy(loot.gameObject);}
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.08f,973);cc.enabled=true;player.transform.rotation=Quaternion.Euler(0,90,0);
            yield return new WaitForSeconds(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ClassicRules.RoleIndex=priorRole;ClassicRules.MapIndex=priorMap;ClassicRules.DifficultyIndex=priorDifficulty;ClassicRules.AutoAim=priorAim;
            game.Settings.quality=priorQuality;SaveService.Apply();PlayerPrefs.SetString(SaveService.ProgressKey,priorProgress);PlayerPrefs.Save();
            game.MainMenu();yield return null;yield return null;
        }
        [UnityTest] public IEnumerator ADSHitsThirtyMeterTorsoReliablyWithoutAutoAim()
        {
            var target=GameObject.CreatePrimitive(PrimitiveType.Cube);target.name="Thirty meter target";target.layer=LayerMask.NameToLayer("TZEnemy");target.transform.position=new Vector3(997,1,973);target.transform.localScale=new Vector3(.3f,1.4f,.5f);
            var view=player.GetComponent<PlayerLook>().View;player.GetComponent<PlayerLook>().enabled=false;view.transform.LookAt(target.transform.position);Physics.SyncTransforms();
            var guns=player.GetComponent<WeaponController>();
            foreach(string id in new[]{"M4A1","AK47","VECTOR","DMR","AWP","RAIL"})
            {
                guns.TakeWeapon(id);yield return new WaitForSeconds(.3f);Assert.IsTrue(guns.TryShoot(true));
                int hits=0;
                for(int i=0;i<200;i++)
                {
                    var direction=guns.ShotDirection(guns.EffectiveSpread(true));var muzzle=view.transform.TransformPoint(new Vector3(.18f,-.15f,.7f));
                    if(HitscanSolver.Trace(view.transform.position,muzzle,direction,guns.Current.Range,LayerMask.GetMask("TZWorld","TZEnemy","TZInteractable"),out var hit)&&hit.collider.gameObject==target)hits++;
                }
                Assert.GreaterOrEqual(hits,195,id+" must hit a 0.5 m wide torso at 30 m while stationary ADS");
                Assert.Less(guns.EffectiveSpread(true),.22f);
            }
            Object.Destroy(target);
        }
        [UnityTest] public IEnumerator VisibleEnemyFlashesAndFallsAndReportsPlayerKill()
        {
            var enemy=new List<EnemyHealth>(EnemyHealth.Active)[0];enemy.transform.position=new Vector3(975,0,973);
            var visual=enemy.GetComponentInChildren<EnemyCombatPresentation>();Assert.IsNotNull(visual);
            var view=player.GetComponent<PlayerLook>().View;player.GetComponent<PlayerLook>().enabled=false;view.transform.LookAt(enemy.AimPoint);Physics.SyncTransforms();
            enemy.Apply(new DamageInfo(12,player.transform.position,"玩家","M4A1"));yield return null;
            Assert.Greater(visual.ImpactIntensity,.2f);Assert.AreEqual(12,CombatFeedback.Instance.Damage);
            Assert.Less(Time.unscaledTime-CombatFeedback.Instance.HitAt,.2f);
            ClassicCampaignTests.Capture("polish-hit-feedback");
            enemy.Apply(new DamageInfo(1000,player.transform.position,"玩家","M4A1",true));
            yield return new WaitForSeconds(.35f);
            Assert.IsFalse(enemy.Alive);
            var ragdoll=visual.GetComponent<OperatorRagdoll>();Assert.IsNotNull(ragdoll,"Refined Blender operator uses physical bones on death");
            var bodies=ragdoll.GetComponentsInChildren<Rigidbody>();Assert.GreaterOrEqual(bodies.Length,10);Assert.IsTrue(System.Array.Exists(bodies,b=>!b.isKinematic));
            Assert.That(CombatFeedback.Instance.KillText,Does.Contain("爆头击杀").And.Contain(enemy.name));
            var banner=GameObject.Find("Kill confirmation");Assert.IsNotNull(banner);Assert.IsTrue(banner.activeInHierarchy);
            ClassicCampaignTests.Capture("polish-kill-feedback");
        }
        [UnityTest] public IEnumerator DamageAndDeathHaveDistinctVisibleHUDAndRespawnClearsDeath()
        {
            player.Apply(new DamageInfo(25,player.transform.position+Vector3.right*10,"守卫 07","AK47"));yield return null;
            Assert.AreEqual(75,player.Health);Assert.IsFalse(CombatFeedback.Instance.ShieldHit);Assert.IsTrue(GameObject.Find("Incoming direction").activeInHierarchy);
            player.Apply(new DamageInfo(1000,player.transform.position+Vector3.forward*8,"守卫 07","AK47",true));yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(GamePhase.Defeat,game.Phase);Assert.That(CombatFeedback.Instance.DeathText,Does.Contain("守卫 07").And.Contain("AK47"));
            var label=GameObject.Find("Death confirmation").GetComponent<UnityEngine.UI.Text>();Assert.That(label.text,Does.Contain("你已阵亡"));
            ClassicCampaignTests.Capture("polish-death-feedback");
            Assert.IsTrue(game.Respawn());yield return null;Assert.IsEmpty(label.text);
        }
        [UnityTest] public IEnumerator ModelAndMaterialUpgradeIsLoadedInActualCampaign()
        {
            var mesh=Resources.Load<Mesh>("Classic/Meshes/Bevel");Assert.That(mesh.name,Does.Contain("Precision"));
            Assert.IsNotNull(Resources.Load<Material>("Classic/Materials/Gunmetal").GetTexture("_BaseMap"));
            foreach(var role in ClassicRules.Data.roles)
            {
                var model=Resources.Load<GameObject>("Classic/Operators/"+role.id);var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.IsNotNull(skin,role.id);Assert.Greater(skin.bones.Length,20,role.id);Assert.Greater(skin.sharedMesh.vertexCount,1000,role.id);
            }
            foreach(var gun in ClassicRules.Data.weapons)
            {
                var model=Resources.Load<GameObject>("Classic/Models/"+gun.id);Assert.IsNotNull(model,gun.id);
                Assert.IsNotNull(model.GetComponentInChildren<WeaponReloadAnimation>(),gun.id+" articulated reload");
            }
            Assert.IsNotNull(Resources.Load<AudioClip>("Audio/confirm-kill"));
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(-12,.08f,-10);cc.enabled=true;player.transform.rotation=Quaternion.identity;player.GetComponent<PlayerLook>().ResetView();
            player.GetComponent<WeaponController>().TakeWeapon("M4A1");yield return new WaitForSeconds(.3f);ClassicCampaignTests.Capture("polish-world-and-weapon");
        }
    }
}
