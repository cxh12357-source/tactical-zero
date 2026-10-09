using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalZero
{
    // Opt-in standalone acceptance runner. Normal launches do not instantiate it.
    public sealed class FieldBenchmark15 : MonoBehaviour
    {
        [Serializable]public class Sample{public string map;public int frames;public double meanMs,p95Ms,meanGpuMs;public float approximateFps;}
        [Serializable]public class Report{public string gpu,unity,resolution;public Sample[] samples;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Application.isEditor||Array.IndexOf(Environment.GetCommandLineArgs(),"--benchmark-field15")<0||FindAnyObjectByType<FieldBenchmark15>())return;
            var runner=new GameObject("Standalone acceptance runner");DontDestroyOnLoad(runner);runner.AddComponent<FieldBenchmark15>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Acceptance");Directory.CreateDirectory(folder);
            string settings=PlayerPrefs.GetString(SaveService.SettingsKey,"{}"),progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");int oldRole=ClassicRules.RoleIndex,oldMap=ClassicRules.MapIndex,oldDifficulty=ClassicRules.DifficultyIndex,stash=PlayerPrefs.GetInt("TZ3D.Stash",0);
            var samples=new List<Sample>();var timing=new FrameTiming[1];
            yield return new WaitForSecondsRealtime(2);
            try
            {
                string[] ids={"sector07","docks","ruins","outpost"};
                for(int i=0;i<ids.Length;i++)
                {
                    ClassicRules.MapIndex=i;ClassicRules.DifficultyIndex=0;
                    yield return SceneManager.LoadSceneAsync("Classic_"+ids[i]);yield return null;yield return null;
                    var game=GameManager.Instance;game.Settings.quality=2;SaveService.Apply();QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;game.Input.ForceTouch=false;
                    foreach(var enemy in EnemyHealth.Active)enemy.GetComponent<EnemyCombat>().enabled=false;
                    var guns=game.Player.GetComponent<WeaponController>();guns.TakeWeapon(i%2==0?"M4A1":"P90");
                    var points=FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None);var cc=game.Player.GetComponent<CharacterController>();var look=game.Player.GetComponent<PlayerLook>();look.enabled=false;var view=look.View;
                    var times=new List<double>();double gpu=0;int gpuFrames=0;float start=Time.realtimeSinceStartup;bool captured=false;int previousPoint=-1;
                    while(Time.realtimeSinceStartup-start<9)
                    {
                        if(game.Phase==GamePhase.Paused)game.Resume();
                        float age=Time.realtimeSinceStartup-start;int point=(int)(age/2)%Mathf.Max(1,points.Length);
                        if(point!=previousPoint&&points.Length>0){cc.enabled=false;game.Player.transform.position=points[point].transform.position+Vector3.up*.07f;cc.enabled=true;previousPoint=point;}
                        view.transform.localRotation=Quaternion.Euler(0,age*22,0);
                        FrameTimingManager.CaptureFrameTimings();
                        if(age>2)
                        {
                            times.Add(Time.unscaledDeltaTime*1000);
                            if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0){gpu+=timing[0].gpuFrameTime;gpuFrames++;}
                        }
                        if(age>5&&!captured){ScreenCapture.CaptureScreenshot(Path.Combine(folder,ids[i]+"-standalone.png"));captured=true;}
                        yield return null;
                    }
                    times.Sort();double sum=0;foreach(double value in times)sum+=value;double mean=times.Count>0?sum/times.Count:0;
                    samples.Add(new Sample{map=ids[i],frames=times.Count,meanMs=mean,p95Ms=times.Count>0?times[Mathf.Min(times.Count-1,(int)(times.Count*.95f))]:0,meanGpuMs=gpuFrames>0?gpu/gpuFrames:0,approximateFps=mean>0?(float)(1000/mean):0});
                }
                File.WriteAllText(Path.Combine(folder,"standalone-performance.json"),JsonUtility.ToJson(new Report{gpu=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,resolution=Screen.width+"x"+Screen.height,samples=samples.ToArray()},true));
            }
            finally
            {
                ClassicRules.RoleIndex=oldRole;ClassicRules.MapIndex=oldMap;ClassicRules.DifficultyIndex=oldDifficulty;PlayerPrefs.SetString(SaveService.SettingsKey,settings);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.SetInt("TZ3D.Stash",stash);PlayerPrefs.Save();
            }
            Application.Quit();
        }
    }
}
