using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalZero
{
    // Opt-in native-player crash replay; absent from ordinary gameplay.
    public sealed class BuildingCrashCheck18 : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(),"--crash-check18")<0 || FindAnyObjectByType<BuildingCrashCheck18>())return;
            var host=new GameObject("Native crash replay 18");DontDestroyOnLoad(host);host.AddComponent<BuildingCrashCheck18>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            int mapBefore=BuildingCatalog.SelectedMap,difficultyBefore=ClassicRules.DifficultyIndex;
            string weaponBefore=BuildingCatalog.SelectedWeapon;
            string progressBefore=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"CrashCheck18");Directory.CreateDirectory(folder);
            string tag=SystemInfo.graphicsDeviceType.ToString();
            Debug.Log("CRASHCHECK18 BEGIN "+tag+" build="+Application.buildGUID);
            try
            {
                for(int round=0;round<16;round++)
                {
                    GameManager.Ensure().MainMenu();
                    yield return new WaitUntil(()=>SceneManager.GetActiveScene().name=="MainMenu" && GameManager.Instance.Phase==GamePhase.MainMenu);
                    yield return new WaitForSecondsRealtime(.2f);
                    BuildingCatalog.SelectedMap=round<4?0:round%4;
                    ClassicRules.DifficultyIndex=2;
                    BuildingCatalog.SelectedWeapon=round<4?"AK47":BuildingCatalog.Weapons[round%BuildingCatalog.Weapons.Length];
                    UnityEngine.Random.InitState(18018+round);
                    GameManager.Instance.StartMatch();
                    yield return new WaitUntil(()=>BuildingMission.Instance && GameManager.Instance.IsPlaying && GameManager.Instance.Player);
                    yield return null;yield return null;
                    var game=GameManager.Instance;var mission=BuildingMission.Instance;
                    Debug.Log("CRASHCHECK18 ROUND "+round+" map="+mission.Definition.Id+" NPC="+mission.Total+" weapon="+BuildingCatalog.SelectedWeapon);
                    var player=game.Player;var gun=player.GetComponent<WeaponController>();var move=player.GetComponent<FPSPlayerController>();var look=player.GetComponent<PlayerLook>();
                    look.enabled=false;move.enabled=false;
                    float start=Time.realtimeSinceStartup;int rendered=0;
                    while(Time.realtimeSinceStartup-start<4)
                    {
                        if(game.Phase==GamePhase.Paused)game.Resume();
                        if(!game.IsPlaying)break;
                        float age=Time.realtimeSinceStartup-start;
                        player.transform.rotation=Quaternion.Euler(0,age*130,0);
                        move.Simulate(new Vector2(Mathf.Sin(age),.6f),false,false,Mathf.Min(Time.deltaTime,.05f));
                        gun.TryShoot(age>2);
                        if(age>3 && gun.Ammo!=null)gun.Ammo.StartReload(gun.Current.ReloadSeconds);
                        rendered++;
                        yield return null;
                    }
                    if(rendered<5)throw new InvalidOperationException("Crash replay did not render combat frames");
                    Debug.Log("CRASHCHECK18 COMPLETE "+round+" frames="+rendered);
                }
                File.WriteAllText(Path.Combine(folder,tag+"-passed.txt"),"16 rendered load/combat/menu cycles completed; build="+Application.buildGUID);
                Debug.Log("CRASHCHECK18 PASS");
            }
            finally
            {
                BuildingCatalog.SelectedMap=mapBefore;ClassicRules.DifficultyIndex=difficultyBefore;BuildingCatalog.SelectedWeapon=weaponBefore;
                PlayerPrefs.SetString(SaveService.ProgressKey,progressBefore);PlayerPrefs.Save();
            }
            Application.Quit(0);
        }
    }
}
