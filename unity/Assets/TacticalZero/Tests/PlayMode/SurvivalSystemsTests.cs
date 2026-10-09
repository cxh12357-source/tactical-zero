using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TacticalZero.Tests
{
    public sealed class SurvivalSystemsTests
    {
        GameManager game;PlayerHealth player;FPSPlayerController move;
        Keyboard testKeyboard;
        InputSettings.BackgroundBehavior priorBackground;
        InputSettings.EditorInputBehaviorInPlayMode priorEditorInput;
        bool changedBackground;
        int priorMap,priorRole,priorDifficulty,priorStash;bool priorAim;string priorProgress;
        [UnitySetUp] public IEnumerator Setup()
        {
            priorMap=ClassicRules.MapIndex;priorRole=ClassicRules.RoleIndex;priorDifficulty=ClassicRules.DifficultyIndex;priorAim=ClassicRules.AutoAim;
            priorStash=PlayerPrefs.GetInt("TZ3D.Stash",0);priorProgress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.MapIndex=0;ClassicRules.RoleIndex=0;ClassicRules.DifficultyIndex=0;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
            game=GameManager.Instance;player=game.Player;move=player.GetComponent<FPSPlayerController>();
            foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}
            yield return new WaitForSeconds(.3f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(testKeyboard!=null){InputSystem.RemoveDevice(testKeyboard);testKeyboard=null;}
            if(changedBackground){InputSystem.settings.backgroundBehavior=priorBackground;InputSystem.settings.editorInputBehaviorInPlayMode=priorEditorInput;changedBackground=false;}
            Time.timeScale=1;ClassicRules.MapIndex=priorMap;ClassicRules.RoleIndex=priorRole;ClassicRules.DifficultyIndex=priorDifficulty;ClassicRules.AutoAim=priorAim;
            PlayerPrefs.SetInt("TZ3D.Stash",priorStash);PlayerPrefs.SetString(SaveService.ProgressKey,priorProgress);PlayerPrefs.Save();
            game.MainMenu();yield return null;yield return null;
        }
        [UnityTest] public IEnumerator SpaceAndTouchJumpRiseOnceThenLandAndRestoreAccuracy()
        {
            // This test measures an unobstructed jump. Random spawns can legitimately be
            // below the new catwalk; the separate head-bump test covers low ceilings.
            bool found=false;Vector3 clear=Vector3.zero;
            for(int y=1;y<19&&!found;y++)for(int x=1;x<27&&!found;x++)
                if(NavMesh.SamplePosition(ClassicMap.Position(x+.5f,y+.5f),out var point,.6f,NavMesh.AllAreas)&&
                   !Physics.CheckCapsule(point.position+Vector3.up*.4f,point.position+Vector3.up*3.1f,.35f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore))
                {clear=point.position;found=true;}
            Assert.IsTrue(found,"Map must have an open area with full jump clearance");
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=clear+Vector3.up*.08f;controller.enabled=true;move.ResetMotion();Physics.SyncTransforms();
            for(int i=0;i<30;i++)move.Simulate(Vector2.zero,false,false,false,1f/60);
            Assert.IsTrue(move.Grounded);move.enabled=false;
            var guns=player.GetComponent<WeaponController>();float groundSpread=guns.EffectiveSpread(true),baseHeight=player.transform.position.y;
            // Exercise the real input route used by the mobile jump button as well.
            game.Input.ForceTouch=true;game.Input.Mobile.Press(MobileAction.Jump);
            Assert.IsTrue(game.Input.JumpPressed);Assert.IsFalse(game.Input.JumpPressed);game.Input.ForceTouch=false;
            // Batch mode has no focused player window. Only the test ignores focus.
            priorBackground=InputSystem.settings.backgroundBehavior;changedBackground=true;
            priorEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(testKeyboard);
            yield return null;
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState(Key.Space));yield return null;
            bool jump=game.Input.JumpPressed;
            bool pressed=testKeyboard.spaceKey.isPressed;
            InputSystem.QueueStateEvent(testKeyboard,new KeyboardState());yield return null;
            Assert.IsTrue(jump,"Space is bound to the gameplay jump action (key state="+pressed+", device enabled="+testKeyboard.enabled+", touch="+game.Input.TouchMode+")");
            move.Simulate(Vector2.zero,false,false,jump,1f/60);
            Assert.IsTrue(move.IsAirborne);Assert.Greater(move.VerticalSpeed,0);Assert.GreaterOrEqual(guns.EffectiveSpread(true),2.2f);
            float prior=move.VerticalSpeed;move.Simulate(Vector2.zero,false,false,true,1f/60);Assert.Less(move.VerticalSpeed,prior,"No double jump");
            ClassicRules.AutoAim=true;
            var forward=player.GetComponent<PlayerLook>().View.transform.forward;
            Assert.Less(Vector3.Angle(forward,player.GetComponent<AimAssistSystem>().Magnet(forward,50)),.01f);
            var seen=new List<Vector3>();float largest=0;
            for(int i=0;i<24;i++){var direction=guns.ShotDirection(guns.EffectiveSpread(true));seen.Add(direction);largest=Mathf.Max(largest,Vector3.Angle(forward,direction));}
            Assert.Greater(largest,.75f);Assert.Greater(Vector3.Angle(seen[0],seen[1]),.01f);
            float peak=player.transform.position.y;
            for(int i=0;i<90;i++){move.Simulate(Vector2.zero,false,false,false,1f/60);peak=Mathf.Max(peak,player.transform.position.y);}
            Assert.That(peak-baseHeight,Is.InRange(.9f,1.3f));Assert.IsTrue(move.Grounded);Assert.AreEqual(groundSpread,guns.EffectiveSpread(true),.01f);
            move.Simulate(Vector2.zero,false,true,true,1f/60);Assert.IsTrue(move.Grounded,"Crouch cannot jump through a ceiling");
            yield return null;
        }
        [UnityTest] public IEnumerator DeathRespawnsAwayFromEnemiesAndClearsBagGearMomentumAndDeathCamera()
        {
            var mission=ClassicEncounter.Instance;var guns=player.GetComponent<WeaponController>();var gear=player.GetComponent<SpecialEquipment>();
            mission.AddContents("tech");guns.TakeWeapon("RAIL");gear.Collect(EquipmentKind.Stim);gear.Use();
            move.Simulate(Vector2.zero,false,false,true,.016f);
            Vector3 death=player.transform.position;player.Apply(new DamageInfo(1000,death+Vector3.forward*6,"复活测试守卫","M4A1"));
            Assert.AreEqual(GamePhase.Defeat,game.Phase);yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(player.GetComponent<PlayerLook>().View.transform.Find("Weapon mount").gameObject.activeSelf);
            Assert.IsTrue(game.Respawn());Assert.AreEqual(GamePhase.Playing,game.Phase);Assert.IsFalse(game.Respawn());
            Assert.Greater(Vector3.Distance(death,player.transform.position),8);Assert.AreEqual(100,player.Health);Assert.AreEqual(0,player.Armor);
            Assert.AreEqual(0,mission.Bag.Count);Assert.AreEqual("USP",guns.Current.Id);Assert.AreEqual(0,gear.StimSeconds);Assert.Less(move.VerticalSpeed,0);
            foreach(var enemy in EnemyHealth.Active)Assert.Greater(Vector3.Distance(player.transform.position,enemy.transform.position),10);
            Assert.IsTrue(player.GetComponent<PlayerLook>().View.transform.Find("Weapon mount").gameObject.activeSelf);
            Assert.AreEqual(1.6f,player.GetComponent<PlayerLook>().View.transform.localPosition.y,.01f);
            player.Apply(new DamageInfo(40,death,"守卫","USP"));Assert.AreEqual(100,player.Health,"Spawn shield blocks damage");
            yield return new WaitForSeconds(.3f);Assert.IsTrue(guns.TryShoot());Assert.AreEqual(0,player.SpawnProtectionSeconds);
            player.Apply(new DamageInfo(20,death,"守卫","USP"));Assert.AreEqual(80,player.Health);
            Assert.AreEqual(priorStash,mission.Stored,"Respawn cannot bank loot");
        }
        [UnityTest] public IEnumerator OnlyHealthSuppliesRemainAndRestoreHealth()
        {
            var mission=ClassicEncounter.Instance;var gear=player.GetComponent<SpecialEquipment>();
            foreach(EquipmentKind kind in System.Enum.GetValues(typeof(EquipmentKind)))
            {Assert.IsNull(mission.SpawnLoot(player.transform.position+Vector3.right*5,"Removed device","equipment",kind.ToString()));Assert.IsFalse(gear.Collect(kind));}
            player.CancelSpawnProtection();player.Apply(new DamageInfo(40,Vector3.zero,"QA","USP"));
            var pack=mission.SpawnLoot(player.transform.position+Vector3.right*5,"急救包","heal","");Assert.IsNotNull(pack);Assert.IsTrue(pack.Interact(player));Assert.AreEqual(100,player.Health);
            yield return null;
        }
        [UnityTest] public IEnumerator EveryMapSupportsRepeatedRandomRespawnsAndProtectionExpires()
        {
            for(int map=0;map<4;map++)
            {
                ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=3;
                yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;
                player=game.Player;
                foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}
                var used=new HashSet<Vector3>();
                for(int i=0;i<3;i++)
                {
                    player.CancelSpawnProtection();player.Apply(new DamageInfo(1000,Vector3.zero,"测试守卫","AK47"));
                    Assert.IsTrue(game.Respawn(),ClassicRules.Map.id);used.Add(player.transform.position);
                    var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(player.transform.position,ClassicEncounter.Instance.Extraction,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);
                    yield return null;
                }
                Assert.GreaterOrEqual(used.Count,2,"Repeated respawns must not use one fixed point");
            }
            yield return new WaitForSeconds(3.1f);Assert.AreEqual(0,player.SpawnProtectionSeconds);
            player.Apply(new DamageInfo(20,Vector3.zero,"测试守卫","USP"));Assert.AreEqual(80,player.Health);
        }
        [UnityTest] public IEnumerator HeadCollisionStopsJumpAndSupplyDisplayRenders()
        {
            move.enabled=false;
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(-20,.08f,-25.5f);cc.enabled=true;move.ResetMotion();
            foreach(var loot in new List<ClassicLoot>(ClassicEncounter.Instance.Supplies)){ClassicEncounter.Instance.ReleaseSupply(loot);Object.Destroy(loot.gameObject);}
            for(int i=0;i<30;i++)move.Simulate(Vector2.zero,false,false,false,1f/60);
            float initial=player.transform.position.y;
            var ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.layer=LayerMask.NameToLayer("TZWorld");ceiling.transform.position=player.transform.position+Vector3.up*2.45f;ceiling.transform.localScale=new Vector3(4,.2f,4);Physics.SyncTransforms();
            move.Simulate(Vector2.zero,false,false,true,1f/60);float peak=initial;
            for(int i=0;i<90;i++){move.Simulate(Vector2.zero,false,false,false,1f/60);peak=Mathf.Max(peak,player.transform.position.y);}
            Assert.Less(peak-initial,.65f);Assert.IsTrue(move.Grounded);Object.Destroy(ceiling);
            var mission=ClassicEncounter.Instance;
            for(int i=0;i<3;i++)mission.SpawnLoot(new Vector3(-24+i*3,0,-21),"急救包","heal","");
            mission.SpawnLoot(new Vector3(-15,0,-21),"M4A1","weapon","M4A1");
            player.transform.rotation=Quaternion.identity;player.GetComponent<PlayerLook>().ResetView();
            yield return new WaitForSeconds(.15f);ClassicCampaignTests.Capture("survival-supplies");
        }
        [UnityTest] public IEnumerator PeriodicSuppliesReplenishAndRespawnDoesNotResetDropClock()
        {
            var mission=ClassicEncounter.Instance;
            foreach(var loot in new List<ClassicLoot>(mission.Supplies))if(loot.Kind=="weapon"||loot.Kind=="heal"){mission.ReleaseSupply(loot);Object.Destroy(loot.gameObject);}
            // Advance the actual scaled mission clock, leaving the rest of the match active.
            Time.timeScale=20;yield return new WaitForSeconds(51);Time.timeScale=1;
            int guns=0,gear=0;foreach(var loot in mission.Supplies){if(loot.Kind=="weapon")guns++;if(loot.Kind=="heal")gear++;}
            Assert.GreaterOrEqual(guns,1,"Replenished ordinary field weapon");Assert.GreaterOrEqual(gear,1);Assert.LessOrEqual(mission.Supplies.Count,36);
            player.Apply(new DamageInfo(1000,Vector3.zero,"守卫","USP"));Assert.IsTrue(game.Respawn());
            Time.timeScale=20;yield return new WaitForSeconds(51);Time.timeScale=1;
            int equipmentAfter=0;foreach(var loot in mission.Supplies)if(loot.Kind=="heal")equipmentAfter++;
            Assert.Greater(equipmentAfter,gear,"Replenishment uses mission time across respawns");
        }
    }
}
