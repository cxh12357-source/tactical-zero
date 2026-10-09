using System.Collections;
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
    public sealed class AimModeTests
    {
        GameManager game;Mouse mouse;bool toggle,autoAim;int role,map,difficulty;string settingsJson,progress;
        InputSettings.BackgroundBehavior background;InputSettings.EditorInputBehaviorInPlayMode editorInput;
        [UnitySetUp]public IEnumerator Setup()
        {
            settingsJson=PlayerPrefs.GetString(SaveService.SettingsKey,"{}");progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;autoAim=ClassicRules.AutoAim;
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
            game=GameManager.Instance;toggle=game.Settings.toggleAim;game.Settings.toggleAim=false;game.Input.ForceTouch=false;game.Input.Clear();
            foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}
            background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(mouse);yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(mouse!=null)InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            game.Settings.toggleAim=toggle;game.Input.Clear();game.Input.ForceTouch=false;
            ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;ClassicRules.AutoAim=autoAim;
            PlayerPrefs.SetString(SaveService.SettingsKey,settingsJson);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();
            game.MainMenu();yield return null;yield return null;
        }
        IEnumerator Right(bool pressed){InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right,pressed));yield return null;yield return null;}
        [UnityTest]public IEnumerator HoldAndClickModesDriveTheActualScopeAndMultipleReadersAgree()
        {
            var guns=game.Player.GetComponent<WeaponController>();guns.TakeWeapon("AWP");var view=game.Player.GetComponent<PlayerLook>().View;
            yield return Right(true);Assert.IsTrue(game.Input.Aiming);yield return new WaitForSeconds(.5f);
            Assert.IsTrue(guns.ScopeActive);Assert.AreEqual(WeaponSight.For("AWP").Fov,view.fieldOfView,.5f);
            yield return Right(false);Assert.IsFalse(game.Input.Aiming);yield return new WaitForSeconds(.5f);Assert.AreEqual(84,view.fieldOfView,.5f);
            game.Settings.toggleAim=true;game.Input.Clear();yield return Right(true);
            for(int i=0;i<20;i++)Assert.IsTrue(game.Input.Aiming,"Reading input must not toggle it again");
            yield return Right(false);yield return new WaitForSeconds(.5f);Assert.IsTrue(guns.ScopeActive);Assert.AreEqual(WeaponSight.For("AWP").Fov,view.fieldOfView,.5f);
            yield return Right(true);Assert.IsFalse(game.Input.Aiming);yield return Right(false);yield return new WaitForSeconds(.5f);Assert.AreEqual(84,view.fieldOfView,.5f);
        }
        [UnityTest]public IEnumerator PauseFocusAndDeathClearToggleAndHeldMenuClicksCannotReopenScope()
        {
            game.Settings.toggleAim=true;yield return Right(true);Assert.IsTrue(game.Input.Aiming);
            game.Pause();Assert.IsFalse(game.Input.Aiming);game.Resume();yield return null;Assert.IsFalse(game.Input.Aiming,"Held mouse on resume needs release first");
            yield return Right(false);yield return Right(true);Assert.IsTrue(game.Input.Aiming);yield return Right(false);
            game.Input.SendMessage("OnApplicationFocus",false);Assert.AreEqual(GamePhase.Paused,game.Phase);Assert.IsFalse(game.Input.Aiming);
            game.Resume();yield return null;Assert.IsFalse(game.Input.Aiming);
            yield return Right(true);yield return Right(false);Assert.IsTrue(game.Input.Aiming);
            game.Player.Apply(new DamageInfo(1000,Vector3.zero,"测试守卫","AWP"));Assert.AreEqual(GamePhase.Defeat,game.Phase);Assert.IsFalse(game.Input.Aiming);
            Assert.IsTrue(game.Respawn());yield return null;Assert.IsFalse(game.Input.Aiming);
        }
        [UnityTest]public IEnumerator SettingsSelectBothModesSaveAcrossReloadAndKeepCombatMenuFree()
        {
            Assert.IsFalse(SaveService.Decode("{\"version\":1}").toggleAim,"Older settings retain hold-to-aim");
            game.Pause();yield return null;var safe=Object.FindFirstObjectByType<GameUI>().transform.Find("Safe Area");
            safe.Find("Menu/设置").GetComponent<Button>().onClick.Invoke();yield return null;
            var button=safe.Find("Settings/Aim mode").GetComponent<Button>();StringAssert.Contains("按住右键",button.GetComponentInChildren<Text>().text);
            button.onClick.Invoke();Assert.IsTrue(SaveService.Decode(PlayerPrefs.GetString(SaveService.SettingsKey)).toggleAim);StringAssert.Contains("点击右键切换",button.GetComponentInChildren<Text>().text);
            ClassicCampaignTests.Capture("aim09-settings");
            button.onClick.Invoke();Assert.IsFalse(SaveService.Decode(PlayerPrefs.GetString(SaveService.SettingsKey)).toggleAim);
            button.onClick.Invoke();game.MainMenu();yield return null;yield return null;SaveService.Load();Assert.IsTrue(game.Settings.toggleAim);
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
            Assert.IsFalse(game.Input.Aiming);Assert.AreEqual(0,Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Length);
            // Touch keeps its existing tap-to-aim control independently of the desktop preference.
            game.Settings.toggleAim=false;game.Input.ForceTouch=true;game.Input.Mobile.Aim=true;Assert.IsTrue(game.Input.Aiming);game.Input.Mobile.Aim=false;Assert.IsFalse(game.Input.Aiming);
        }
    }
}
