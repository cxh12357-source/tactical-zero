using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Profiling;
namespace TacticalZero
{
    public static class BuildingPerformance18
    {
        public static readonly bool Optimized=Array.IndexOf(Environment.GetCommandLineArgs(),"--baseline18")<0;
        public static double NavigationMilliseconds;public static int PathRequests;
    }
    // Explicit QA invocation only. The normal game has no benchmark overlay or synthetic input.
    public sealed class BuildingBenchmark18:MonoBehaviour
    {
        [Serializable] public class Sample {public string map;public int seed,enemies,frames,longFrames,audioPeak,pathRequests;public double meanMs,medianMs,p95Ms,p99Ms,fps,navMs,managedMB,allocatedBytes;public long drawCalls,triangles,totalMemory;public float[] frameMs;}
        [Serializable] public class Report {public bool optimized;public string unity,gpu,cpu,resolution,conditions,build;public Sample[] samples;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            if(Application.isEditor||Array.IndexOf(Environment.GetCommandLineArgs(),"--benchmark18")<0||FindAnyObjectByType<BuildingBenchmark18>())return;
            var root=new GameObject("Building18 deterministic acceptance");DontDestroyOnLoad(root);root.AddComponent<BuildingBenchmark18>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Acceptance18");Directory.CreateDirectory(folder);
            string settings=PlayerPrefs.GetString(SaveService.SettingsKey,"{}"),progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            int mapBefore=BuildingCatalog.SelectedMap,diffBefore=ClassicRules.DifficultyIndex;string weaponBefore=BuildingCatalog.SelectedWeapon;
            var report=new Report{optimized=BuildingPerformance18.Optimized,build=Application.buildGUID,unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,conditions="Seed 18018, medium difficulty (8 NPC/floor), M4A1, high quality, 1280x720, fixed outer-corridor walk/turn route; AI patrol active, attacks disabled; 3s warmup + 8s measured per scene. Optimizations toggled in the same build. Desktop native test, not mobile performance."};
            var samples=new List<Sample>();yield return new WaitForSecondsRealtime(1);
            try
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;yield return null;
                foreach(var b in FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name=="开始行动 →"){b.onClick.Invoke();break;}
                yield return null;
                foreach(var b in FindObjectsByType<Button>(FindObjectsSortMode.None))if(b.name=="选择建筑 →"){b.onClick.Invoke();break;}
                yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"map-selection.png"));yield return new WaitForSecondsRealtime(.2f);
                for(int round=0;round<5;round++)
                {
                    int index=round%4;UnityEngine.Random.InitState(18018);BuildingCatalog.SelectedMap=index;ClassicRules.DifficultyIndex=1;BuildingCatalog.SelectedWeapon="M4A1";
                    yield return SceneManager.LoadSceneAsync(BuildingCatalog.Maps[index].Scene);yield return null;yield return null;yield return null;
                    var game=GameManager.Instance;game.Settings.quality=2;game.Settings.fullscreen=false;SaveService.Apply();Screen.SetResolution(1280,720,FullScreenMode.Windowed);QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;game.Input.ForceTouch=true;game.Input.Clear();
                    var mission=BuildingMission.Instance;foreach(var npc in mission.Roster)npc.GetComponent<EnemyCombat>().enabled=false;
                    var look=game.Player.GetComponent<PlayerLook>();look.enabled=false;var movement=game.Player.GetComponent<FPSPlayerController>();movement.enabled=false;
                    BuildingMission.Warp(game.Player,new Vector3(-19.5f,mission.Definition.SpawnFloor*5,-12));game.Player.transform.rotation=Quaternion.identity;look.ResetView();
                    var times=new List<float>(20000);float start=Time.realtimeSinceStartup,last=start;int peak=0;long draws=0,tris=0,memory=0,alloc=0;int measured=0;long allocatedStart=GC.GetAllocatedBytesForCurrentThread();
                    using(var draw=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
                    using(var triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count"))
                    using(var mem=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory"))
                    using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
                    {
                        BuildingPerformance18.NavigationMilliseconds=0;BuildingPerformance18.PathRequests=0;
                        while(Time.realtimeSinceStartup-start<11)
                        {
                            if(game.Phase==GamePhase.Paused)game.Resume();
                            float age=Time.realtimeSinceStartup-start,dt=Mathf.Min(.05f,Time.realtimeSinceStartup-last);last=Time.realtimeSinceStartup;
                            game.Player.transform.rotation=Quaternion.Euler(0,Mathf.Sin(age*.55f)*70,0);
                            Vector3 target=new Vector3(-19.5f,mission.Definition.SpawnFloor*5,age<6?12:-12);Vector3 local=game.Player.transform.InverseTransformDirection(target-game.Player.transform.position);Vector2 move=new Vector2(local.x,local.z).normalized;
                            movement.Simulate(move,false,false,dt);look.View.transform.localRotation=Quaternion.Euler(2,0,0);
                            if(age>=3){if(measured==0)allocatedStart=GC.GetAllocatedBytesForCurrentThread();times.Add(Time.unscaledDeltaTime*1000);peak=Mathf.Max(peak,AudioService.Instance.ActiveVoices);draws+=draw.Valid?draw.LastValue:0;tris+=triangles.Valid?triangles.LastValue:0;memory=Math.Max(memory,mem.Valid?mem.LastValue:0);alloc+=gc.Valid?gc.LastValue:0;measured++;}
                            yield return null;
                        }
                    }
                    float[] raw=times.ToArray();times.Sort();double sum=0;int longs=0;foreach(var t in times){sum+=t;if(t>50)longs++;}
                    var sample=new Sample{map=mission.Definition.Id+(round==4?"-repeat":""),seed=18018,enemies=mission.Total,frames=times.Count,longFrames=longs,audioPeak=peak,meanMs=sum/times.Count,medianMs=times[times.Count/2],p95Ms=times[(int)(times.Count*.95)],p99Ms=times[(int)(times.Count*.99)],fps=times.Count*1000/sum,managedMB=GC.GetTotalMemory(false)/1048576.0,navMs=BuildingPerformance18.NavigationMilliseconds,pathRequests=BuildingPerformance18.PathRequests,drawCalls=draws/Math.Max(1,measured),triangles=tris/Math.Max(1,measured),totalMemory=memory,allocatedBytes=(GC.GetAllocatedBytesForCurrentThread()-allocatedStart)/(double)Math.Max(1,measured),frameMs=raw};if(sample.triangles<=0)throw new InvalidOperationException("No rendered frames; benchmark requires a visible game window.");samples.Add(sample);
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,sample.map+"-runtime.png"));yield return new WaitForSecondsRealtime(.2f);
                    Debug.Log("BENCHMARK18 "+sample.map+" median="+sample.medianMs+" p99="+sample.p99Ms+" NPC="+sample.enemies);
                }
                report.resolution=Screen.width+"x"+Screen.height;report.samples=samples.ToArray();File.WriteAllText(Path.Combine(folder,BuildingPerformance18.Optimized?"optimized.json":"baseline.json"),JsonUtility.ToJson(report,true));
            }
            finally
            {
                BuildingCatalog.SelectedMap=mapBefore;BuildingCatalog.SelectedWeapon=weaponBefore;ClassicRules.DifficultyIndex=diffBefore;PlayerPrefs.SetString(SaveService.SettingsKey,settings);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();
            }
            Application.Quit();
        }
    }
}
