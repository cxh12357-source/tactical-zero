using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TacticalZero.Tests
{
    public sealed class CleanHUDTests
    {
        GameManager game;PlayerHealth player;Keyboard keyboard;Transform safe;
        int priorMap,priorRole,priorDifficulty;bool priorAim;string priorProgress;
        InputSettings.BackgroundBehavior background;InputSettings.EditorInputBehaviorInPlayMode editorInput;
        [UnitySetUp] public IEnumerator Setup()
        {
            priorMap=ClassicRules.MapIndex;priorRole=ClassicRules.RoleIndex;priorDifficulty=ClassicRules.DifficultyIndex;priorAim=ClassicRules.AutoAim;priorProgress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.MapIndex=ClassicRules.RoleIndex=ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
            ClassicCampaignTests.IsolatedRange();
            game=GameManager.Instance;player=game.Player;game.Input.ForceTouch=false;
            safe=Object.FindAnyObjectByType<GameUI>().transform.Find("Safe Area");
            foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}
            foreach(var loot in new List<ClassicLoot>(ClassicEncounter.Instance.Supplies)){ClassicEncounter.Instance.ReleaseSupply(loot);Object.Destroy(loot.gameObject);}
            background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();InputSystem.EnableDevice(keyboard);yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            ClassicRules.MapIndex=priorMap;ClassicRules.RoleIndex=priorRole;ClassicRules.DifficultyIndex=priorDifficulty;ClassicRules.AutoAim=priorAim;
            PlayerPrefs.SetString(SaveService.ProgressKey,priorProgress);PlayerPrefs.Save();game.MainMenu();yield return null;yield return null;
        }
        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
        }
        [UnityTest] public IEnumerator DesktopCombatHasNoClickableMenusAndEscapeOpensSettingsAndExit()
        {
            Assert.AreEqual(0,Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length,"Combat HUD must not steal clicks from firing");
            Assert.IsFalse(safe.Find("Menu").gameObject.activeInHierarchy);
            yield return Press(Key.Escape);
            Assert.AreEqual(GamePhase.Paused,game.Phase);Assert.IsTrue(game.PauseMenuRequested);
            var menu=safe.Find("Menu");Assert.IsTrue(menu.gameObject.activeInHierarchy);Assert.IsTrue(menu.Find("退出游戏").gameObject.activeInHierarchy);
            ClassicCampaignTests.Capture("hud-escape-menu");
            menu.Find("设置").GetComponent<Button>().onClick.Invoke();yield return null;Assert.IsTrue(safe.Find("Settings").gameObject.activeInHierarchy);
            yield return Press(Key.Escape);Assert.AreEqual(GamePhase.Playing,game.Phase);Assert.IsFalse(safe.Find("Settings").gameObject.activeInHierarchy);
            Assert.AreEqual(0,Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length);
        }
        [UnityTest] public IEnumerator BagAndFocusPauseDoNotExposeSettingsAndDeathUsesEscapeOptions()
        {
            yield return Press(Key.B);Assert.AreEqual(GamePhase.Paused,game.Phase);Assert.IsFalse(game.PauseMenuRequested);
            Assert.IsTrue(safe.Find("Inventory overlay").gameObject.activeInHierarchy);Assert.IsFalse(safe.Find("Menu").gameObject.activeInHierarchy);
            yield return Press(Key.Escape);Assert.AreEqual(GamePhase.Playing,game.Phase);Assert.IsFalse(safe.Find("Inventory overlay").gameObject.activeInHierarchy);
            game.Pause(false);yield return null;Assert.IsFalse(safe.Find("Menu").gameObject.activeInHierarchy);yield return Press(Key.Escape);
            player.Apply(new DamageInfo(1000,Vector3.zero,"守卫","AK47"));yield return null;
            Assert.IsFalse(safe.Find("Menu/设置").gameObject.activeInHierarchy);Assert.IsFalse(safe.Find("Menu/退出游戏").gameObject.activeInHierarchy);
            yield return Press(Key.Escape);Assert.AreEqual(GamePhase.Defeat,game.Phase);Assert.IsTrue(safe.Find("Menu/设置").gameObject.activeInHierarchy);
            safe.Find("Menu/设置").GetComponent<Button>().onClick.Invoke();yield return null;
            yield return Press(Key.Escape);Assert.IsFalse(safe.Find("Settings").gameObject.activeInHierarchy);Assert.IsFalse(game.PauseMenuRequested);
            Assert.IsTrue(safe.Find("Menu/随机复活 · 丢失背包与装备").gameObject.activeInHierarchy);
            Assert.IsTrue(game.Respawn());game.Input.ForceTouch=true;game.Pause(false);yield return null;
            var touchResume=safe.Find("Passive pause").GetComponent<Button>();Assert.IsTrue(touchResume.enabled);Assert.IsTrue(touchResume.targetGraphic.raycastTarget);
            touchResume.onClick.Invoke();Assert.AreEqual(GamePhase.Playing,game.Phase);game.Input.ForceTouch=false;
        }
        [UnityTest] public IEnumerator PickupIconsFollowTargetAndHideWhenConsumedOrOccluded()
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.1f,973);cc.enabled=true;player.transform.rotation=Quaternion.Euler(0,90,0);
            var look=player.GetComponent<PlayerLook>();look.enabled=false;look.ResetView();
            var mission=ClassicEncounter.Instance;
            var crate=mission.SpawnLoot(new Vector3(970,0,973),"M4A1","weapon","M4A1");crate.transform.position=player.transform.position+player.transform.forward*2+Vector3.up*.5f;
            look.View.transform.LookAt(crate.transform.position);Physics.SyncTransforms();yield return new WaitForSeconds(.25f);
            var icon=GameObject.Find("Pickup icon").GetComponent<SupplyIcon>();Assert.AreEqual("weapon",icon.Kind);Assert.AreEqual("M4A1",icon.ItemId);Assert.IsFalse(icon.raycastTarget);
            ClassicCampaignTests.Capture("hud-weapon-pickup");Assert.IsTrue(crate.Interact(player));yield return new WaitForSeconds(.2f);
            Assert.IsFalse(safe.Find("Supply HUD/Pickup card").gameObject.activeInHierarchy);
            crate=mission.SpawnLoot(new Vector3(970,0,973),"急救包","heal","");crate.transform.position=player.transform.position+player.transform.forward*2+Vector3.up*.5f;
            look.View.transform.LookAt(crate.transform.position);Physics.SyncTransforms();yield return new WaitForSeconds(.2f);
            Assert.AreEqual("heal",icon.Kind);Assert.AreEqual("",icon.ItemId);
            var gear=player.GetComponent<SpecialEquipment>();gear.Collect(EquipmentKind.Shield);gear.Collect(EquipmentKind.Shield);yield return new WaitForSeconds(.2f);
            Assert.That(GameObject.Find("Pickup key").GetComponent<Text>().text,Does.Contain("已满"));
            ClassicCampaignTests.Capture("hud-health-pickup");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=LayerMask.NameToLayer("TZWorld");wall.transform.position=player.transform.position+player.transform.forward+Vector3.up;wall.transform.localScale=new Vector3(.2f,3,3);Physics.SyncTransforms();yield return new WaitForSeconds(.25f);
            Assert.IsFalse(safe.Find("Supply HUD/Pickup card").gameObject.activeInHierarchy);Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator SupplyMarkerTracksTheRenderedCameraEveryFrame()
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.1f,973);cc.enabled=true;
            var look=player.GetComponent<PlayerLook>();look.enabled=false;look.ResetView();
            var crate=ClassicEncounter.Instance.SpawnLoot(new Vector3(973,0,973),"M4A1","weapon","M4A1");
            crate.transform.position=new Vector3(973,.5f,973);Physics.SyncTransforms();
            var root=(RectTransform)safe.Find("Supply HUD");var marker=(RectTransform)root.Find("World supply marker 0");
            var canvas=root.GetComponentInParent<Canvas>();
            var harness=new GameObject("Supply frame regression");int frames=0;float worst=0;bool allVisible=true;
            harness.AddComponent<SupplyFrameDriver>().Drive=()=>
            {
                look.View.fieldOfView=frames%2==0?55:75;
                look.View.transform.LookAt(crate.GroundPosition+Vector3.up*1.6f+Vector3.forward*(frames%2==0?1:-1));
            };
            harness.AddComponent<SupplyFrameObserver>().Observe=()=>
            {
                var screen=look.View.WorldToScreenPoint(crate.GroundPosition+Vector3.up*1.6f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var expected);
                worst=Mathf.Max(worst,Vector2.Distance(expected,marker.anchoredPosition));allVisible&=marker.gameObject.activeInHierarchy;frames++;
            };
            try
            {
                while(frames<12)yield return null;
                Assert.IsTrue(allVisible,"Visible supply marker must appear on the first rendered frame");
                Assert.Less(worst,.5f,"Supply marker lagged behind the rendered camera (UI pixels)");
            }
            finally {Object.Destroy(harness);}
        }
        [UnityTest] public IEnumerator PickupCardAcquiresAndClearsTargetOnTheSameRenderedFrame()
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.1f,973);cc.enabled=true;
            var look=player.GetComponent<PlayerLook>();look.enabled=false;look.ResetView();
            var crate=ClassicEncounter.Instance.SpawnLoot(new Vector3(970,0,973),"M4A1","weapon","M4A1");
            crate.transform.position=player.transform.position+Vector3.right*2+Vector3.up*.5f;Physics.SyncTransforms();
            var card=safe.Find("Supply HUD/Pickup card");var interaction=player.GetComponent<PlayerInteraction>();
            var harness=new GameObject("Pickup frame regression");int frames=0,mismatches=0;bool aiming=false;
            harness.AddComponent<SupplyFrameDriver>().Drive=()=>
            {
                aiming=frames%2==0;look.View.transform.LookAt(aiming?crate.transform.position:look.View.transform.position+Vector3.up);
            };
            harness.AddComponent<SupplyFrameObserver>().Observe=()=>
            {
                if((interaction.Target==crate)!=aiming||card.gameObject.activeInHierarchy!=aiming)mismatches++;
                frames++;
            };
            try
            {
                while(frames<12)yield return null;
                Assert.AreEqual(0,mismatches,"Target acquisition / dismissal must match the camera on every rendered frame");
                bool consumedImmediately=false;
                harness.GetComponent<SupplyFrameObserver>().Observe=()=>
                {
                    if(game.Input.InteractPressed)consumedImmediately=!card.gameObject.activeInHierarchy&&interaction.Target==null&&(!crate||!crate.gameObject.activeInHierarchy);
                };
                harness.GetComponent<SupplyFrameDriver>().Drive=()=>{if(crate)look.View.transform.LookAt(crate.transform.position);};
                yield return null;yield return Press(Key.E);
                Assert.IsTrue(consumedImmediately,"E must consume the supply and clear its target/card before this frame renders");
                Assert.IsFalse(card.gameObject.activeInHierarchy,"Picking up a supply must immediately clear its card");
            }
            finally {Object.Destroy(harness);}
        }
        [UnityTest] public IEnumerator HolographicSuppliesNeverBlockMovementOrBullets()
        {
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.1f,973);cc.enabled=true;
            var look=player.GetComponent<PlayerLook>();look.enabled=false;look.ResetView();
            var mission=ClassicEncounter.Instance;var crates=new List<ClassicLoot>();
            foreach(var kind in new[]{"weapon","heal","tech","arms"})
            {
                var crate=mission.SpawnLoot(new Vector3(970,0,973),kind,kind,kind=="weapon"?"AWP":kind=="equipment"?"Shield":"");crates.Add(crate);
            }
            yield return null;
            foreach(var crate in crates){Assert.AreEqual(0,crate.GetComponentsInChildren<Collider>().Length);Assert.IsNotNull(crate.transform.Find("Supply ground halo"));}
            foreach(var crate in crates)if(crate!=crates[0]){mission.ReleaseSupply(crate);Object.Destroy(crate.gameObject);}
            var supply=crates[0];supply.transform.position=new Vector3(969,.5f,973);look.View.transform.LookAt(supply.GroundPosition+Vector3.up*.5f);Physics.SyncTransforms();yield return null;yield return null;
            Assert.AreEqual(supply,player.GetComponent<PlayerInteraction>().Target);
            Assert.IsFalse(Physics.Raycast(new Vector3(967,.5f,973),Vector3.right,4,LayerMask.GetMask("TZInteractable"),QueryTriggerInteraction.Collide));
            ClassicCampaignTests.Capture("art06-holographic-supplies");
            cc.Move(Vector3.right*4);Assert.Greater(player.transform.position.x,970.5f,"Player must walk through the holographic supply");
        }
        [UnityTest] public IEnumerator WeaponSpecificOpticsRenderAndResetAfterReloadSwitchAndPause()
        {
            var guns=player.GetComponent<WeaponController>();game.Input.ForceTouch=true;
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=new Vector3(967,.1f,973);cc.enabled=true;
            player.transform.rotation=Quaternion.Euler(0,90,0);player.GetComponent<PlayerLook>().ResetView();
            var view=player.GetComponent<PlayerLook>().View;
            foreach(string id in new[]{"AWP","DMR","RAIL","M4A1","AK47","Shotgun","VECTOR","CYCLONE","NOVA","MP6","RPK"})
            {
                guns.TakeWeapon(id);game.Input.Mobile.Aim=true;yield return new WaitForSeconds(.55f);
                var sight=WeaponSight.For(id);Assert.IsTrue(guns.SightEngaged);Assert.AreEqual(sight.Magnified,guns.ScopeActive);Assert.AreEqual(sight.Fov,view.fieldOfView,.5f);
                Assert.IsTrue(safe.Find("HUD/Weapon optic").gameObject.activeInHierarchy);
                var reticle=safe.Find("HUD/Weapon optic/Optic aperture and reticle").GetComponent<OpticReticle>();Canvas.ForceUpdateCanvases();
                Assert.IsFalse(reticle.raycastTarget);if(sight.Style!=SightStyle.Iron)Assert.Greater(reticle.canvasRenderer.GetMesh().vertexCount,0);
                if(id=="AWP"||id=="DMR"||id=="RAIL"||id=="M4A1"||id=="AK47")ClassicCampaignTests.Capture("art06-optic-"+id);
            }
            guns.TakeWeapon("DMR");yield return new WaitForSeconds(.3f);Assert.IsTrue(guns.TryShoot(true));Assert.IsTrue(guns.Ammo.StartReload(guns.Current.ReloadSeconds));yield return null;yield return null;
            Assert.IsFalse(guns.ScopeActive);Assert.IsFalse(safe.Find("HUD/Weapon optic").gameObject.activeInHierarchy);
            guns.Ammo.CancelReload();game.Input.Mobile.Aim=false;yield return new WaitForSeconds(.5f);Assert.AreEqual(84,view.fieldOfView,.5f);
            game.Pause();yield return null;Assert.IsFalse(safe.Find("HUD/Weapon optic").gameObject.activeInHierarchy);game.Resume();game.Input.ForceTouch=false;
        }
        [UnityTest] public IEnumerator FullscreenToggleIsSavedAndDoesNotOpenCombatMenus()
        {
            bool prior=game.Settings.fullscreen;string json=PlayerPrefs.GetString(SaveService.SettingsKey,"{}");
            try
            {
                yield return Press(Key.F11);Assert.AreEqual(!prior,game.Settings.fullscreen);Assert.AreEqual(GamePhase.Playing,game.Phase);
                Assert.AreEqual(!prior,SaveService.Decode(PlayerPrefs.GetString(SaveService.SettingsKey)).fullscreen);
                Assert.AreEqual(0,Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length);
                yield return Press(Key.Escape);safe.Find("Menu/设置").GetComponent<Button>().onClick.Invoke();yield return null;
                var button=safe.Find("Settings/全屏 F11："+(prior?"开":"关"));
                // Settings button name is created at scene load; its action reads the current preference.
                if(!button)foreach(var b in safe.Find("Settings").GetComponentsInChildren<Button>())if(b.name.StartsWith("全屏 F11"))button=b.transform;
                Assert.IsNotNull(button);button.GetComponent<Button>().onClick.Invoke();Assert.AreEqual(prior,game.Settings.fullscreen);
            }
            finally{game.Settings.fullscreen=prior;PlayerPrefs.SetString(SaveService.SettingsKey,json);PlayerPrefs.Save();}
        }
        [UnityTest] public IEnumerator IconAtlasAndEquipmentSelectionHaveDistinctVisuals()
        {
            var gallery=UIFactory.Rect("Icon gallery QA",safe,new Vector2(.5f,.5f),Vector2.zero,new Vector2(980,455));UIFactory.Panel(gallery,new Color(.025f,.05f,.075f,.98f));
            int i=0;foreach(var gun in ClassicRules.Data.weapons)
            {
                float x=-405+(i%6)*162,y=145-(i/6)*140;var tile=UIFactory.Rect(gun.id,gallery,new Vector2(.5f,.5f),new Vector2(x,y),new Vector2(145,104));var icon=tile.gameObject.AddComponent<SupplyIcon>();icon.Set("weapon",gun.id,new Color(1,.78f,.35f));
                UIFactory.Text("Label",gallery,gun.label,new Vector2(.5f,.5f),new Vector2(x,y-55),new Vector2(155,25),16,TextAnchor.MiddleCenter);i++;
            }
            for(int j=0;j<3;j++){var tile=UIFactory.Rect("Device",gallery,new Vector2(.5f,.5f),new Vector2(-162+j*162,-132),new Vector2(83,83));tile.gameObject.AddComponent<SupplyIcon>().Set("equipment",((EquipmentKind)j).ToString(),SpecialEquipment.Tint((EquipmentKind)j));}
            yield return null;Canvas.ForceUpdateCanvases();
            foreach(var icon in gallery.GetComponentsInChildren<SupplyIcon>())
            {
                var mesh=icon.canvasRenderer.GetMesh();Assert.IsNotNull(mesh);Assert.Greater(mesh.vertexCount,12);Assert.Greater(mesh.bounds.size.sqrMagnitude,10);
                Assert.IsTrue(System.Array.Exists(mesh.colors32,c=>c.a>100));
            }
            var view=player.GetComponent<PlayerLook>().View;Vector3 position=view.transform.position;view.transform.position=Vector3.one*10000;
            ClassicCampaignTests.Capture("hud-supply-icon-atlas");view.transform.position=position;
            var screenshot=new Texture2D(2,2);screenshot.LoadImage(System.IO.File.ReadAllBytes("TestResults/hud-supply-icon-atlas.png"));int gold=0;
            for(int y=455;y<565;y++)for(int x=150;x<1130;x++){var c=screenshot.GetPixel(x,y);if(c.r>.6f&&c.g>.35f&&c.b<.5f)gold++;}
            Assert.Greater(gold,600,"Gun icons must produce visible colored pixels, not just empty UI geometry");Object.Destroy(screenshot);Object.Destroy(gallery.gameObject);
            // The later field release removed special supplies; icons remain available to legacy art.
            var gear=player.GetComponent<SpecialEquipment>();Assert.IsFalse(gear.Collect(EquipmentKind.Armor));Assert.IsFalse(gear.Collect(EquipmentKind.Shield));yield return Press(Key.G);yield return Press(Key.F);Assert.AreEqual(0,player.Armor);
        }
    }
}
