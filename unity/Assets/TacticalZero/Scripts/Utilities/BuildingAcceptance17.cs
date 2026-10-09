using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalZero
{
    // Explicit standalone QA flag only. No runner or altered combat on ordinary launches.
    public sealed class BuildingAcceptance17 : MonoBehaviour
    {
        [Serializable] public class Sample
        {
            public string map; public int frames, enemies, audioVoices;
            public double meanMs, p95Ms, managedMegabytes; public float fps;
        }
        [Serializable] public class Report
        {
            public string gpu, unity, resolution;
            public bool walkedIntoExtractionAndCompleted;
            public string conditions="High quality; live navigation, enemy attacks disabled for repeatable rendering measurements. This is not a human combat/pacing test.";
            public Sample[] samples;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(),"--review-building17")<0 || FindAnyObjectByType<BuildingAcceptance17>()) return;
            var go=new GameObject("Building standalone acceptance"); DontDestroyOnLoad(go); go.AddComponent<BuildingAcceptance17>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Acceptance"); Directory.CreateDirectory(folder);
            string[] keys={SaveService.SettingsKey,SaveService.ProgressKey,"TZ3D.Building.Map","TZ3D.Building.Weapon","TZ3D.Stash"};
            string settings=PlayerPrefs.GetString(keys[0],"{}"),progress=PlayerPrefs.GetString(keys[1],"{}");
            int oldMap=BuildingCatalog.SelectedMap,oldRole=ClassicRules.RoleIndex,oldDifficulty=ClassicRules.DifficultyIndex,stash=PlayerPrefs.GetInt(keys[4],0);
            string oldWeapon=BuildingCatalog.SelectedWeapon;
            string proofRecord="TZ3D.Building.Best.harbor.1.M4A1";bool hadProofRecord=PlayerPrefs.HasKey(proofRecord);float oldProofRecord=PlayerPrefs.GetFloat(proofRecord);bool extracted=false;
            var samples=new List<Sample>();
            yield return new WaitForSecondsRealtime(2);
            try
            {
                yield return SceneManager.LoadSceneAsync("MainMenu"); yield return new WaitForSecondsRealtime(.6f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"building-menu.png")); yield return new WaitForSecondsRealtime(.3f);
                foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                    if(button.name=="开始行动 →"){button.onClick.Invoke();break;}
                yield return new WaitForSecondsRealtime(.4f);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"building-loadout.png")); yield return new WaitForSecondsRealtime(.3f);
                for(int round=0;round<5;round++)
                {
                    int index=round%4; var map=BuildingCatalog.Maps[index];
                    BuildingCatalog.SelectedMap=index; BuildingCatalog.SelectedWeapon=new[]{"M4A1","P90","M1014","MG3"}[index]; ClassicRules.DifficultyIndex=1;
                    yield return SceneManager.LoadSceneAsync(map.Scene); yield return null; yield return null;
                    var game=GameManager.Instance; game.Settings.quality=2; SaveService.Apply(); QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
                    game.Input.ForceTouch=false;
                    foreach(var enemy in EnemyHealth.Active) enemy.GetComponent<EnemyCombat>().enabled=false;
                    var mission=BuildingMission.Instance; var look=game.Player.GetComponent<PlayerLook>();look.enabled=false;
                    var cc=game.Player.GetComponent<CharacterController>(); cc.enabled=false;
                    game.Player.transform.position=new Vector3(-16,map.SpawnFloor*5+.1f,0); game.Player.transform.rotation=Quaternion.Euler(0,90,0); cc.enabled=true;
                    look.ResetView(); Physics.SyncTransforms();
                    var times=new List<double>(); float start=Time.realtimeSinceStartup; bool captured=false;
                    while(Time.realtimeSinceStartup-start<10)
                    {
                        if(game.Phase==GamePhase.Paused)game.Resume();
                        float age=Time.realtimeSinceStartup-start;
                        look.View.transform.localRotation=Quaternion.Euler(3,Mathf.Sin(age*.2f)*18,0);
                        if(age>3)times.Add(Time.unscaledDeltaTime*1000);
                        if(age>5&&!captured)
                        {
                            ScreenCapture.CaptureScreenshot(Path.Combine(folder,map.Id+(round==4?"-repeat":"")+"-standalone.png"));captured=true;
                        }
                        yield return null;
                    }
                    times.Sort();double sum=0;foreach(double value in times)sum+=value;double mean=times.Count==0?0:sum/times.Count;
                    samples.Add(new Sample{map=map.Id+(round==4?"-repeat":""),frames=times.Count,enemies=mission.Remaining,audioVoices=FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,meanMs=mean,p95Ms=times.Count==0?0:times[Mathf.Min(times.Count-1,(int)(times.Count*.95f))],managedMegabytes=GC.GetTotalMemory(false)/1048576.0,fps=mean>0?(float)(1000/mean):0});
                    var guns=game.Player.GetComponent<WeaponController>();guns.TryShoot();yield return null;guns.Ammo.StartReload(guns.Current.ReloadSeconds);yield return new WaitForSecondsRealtime(.9f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,map.Id+"-reload.png"));yield return new WaitForSecondsRealtime(.2f);
                    game.Input.ForceTouch=true;game.Pause();game.Resume();yield return new WaitForSecondsRealtime(.3f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,map.Id+"-touch.png"));yield return new WaitForSecondsRealtime(.2f);game.Input.ForceTouch=false;
                    // Interior evidence on every physical floor; navigation coverage is tested separately.
                    for(int floor=0;floor<map.Floors.Length;floor++)
                    {
                        Vector3 room=mission.SpawnZones[floor*8];cc.enabled=false;
                        game.Player.transform.position=room+new Vector3(2,.1f,2);cc.enabled=true;game.Player.transform.LookAt(new Vector3(room.x,game.Player.transform.position.y,room.z-2));look.ResetView();
                        yield return new WaitForSecondsRealtime(.7f);
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder,map.Id+"-"+map.Floors[floor]+"-room.png"));yield return new WaitForSecondsRealtime(.2f);
                    }
                    if(round==4)
                    {
                        // Validate the real Update/CharacterController/extraction UI path in the built player.
                        // Test damage clears the roster; this is deliberately not claimed as an unaided win.
                        foreach(var enemy in mission.Roster)if(enemy.Alive)enemy.Apply(new DamageInfo(10000,game.Player.transform.position,"Acceptance","M4A1"));
                        cc.enabled=false;game.Player.transform.position=map.Exit+Vector3.back*5+Vector3.up*.1f;cc.enabled=true;game.Player.transform.rotation=Quaternion.identity;look.ResetView();game.Player.GetComponent<FPSPlayerController>().ResetMotion();
                        game.Input.ForceTouch=true;game.Input.Clear();game.Pause();game.Resume();yield return new WaitForSecondsRealtime(.3f);
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"extraction-unlocked.png"));
                        game.Input.Mobile.Move=Vector2.up;yield return new WaitForSeconds(.95f);game.Input.Mobile.Move=Vector2.zero;
                        yield return new WaitForSeconds(.8f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"extraction-countdown.png"));
                        yield return new WaitForSecondsRealtime(3);extracted=game.Phase==GamePhase.Victory;
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"extraction-result.png"));yield return new WaitForSecondsRealtime(.3f);
                    }
                }
                File.WriteAllText(Path.Combine(folder,"building-standalone.json"),JsonUtility.ToJson(new Report{gpu=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,resolution=Screen.width+"x"+Screen.height,walkedIntoExtractionAndCompleted=extracted,samples=samples.ToArray()},true));
            }
            finally
            {
                BuildingCatalog.SelectedMap=oldMap;BuildingCatalog.SelectedWeapon=oldWeapon;ClassicRules.RoleIndex=oldRole;ClassicRules.DifficultyIndex=oldDifficulty;
                PlayerPrefs.SetString(keys[0],settings);PlayerPrefs.SetString(keys[1],progress);PlayerPrefs.SetInt(keys[4],stash);PlayerPrefs.Save();
                if(hadProofRecord)PlayerPrefs.SetFloat(proofRecord,oldProofRecord);else PlayerPrefs.DeleteKey(proofRecord);PlayerPrefs.Save();
            }
            Application.Quit(extracted?0:2);
        }
    }
}
