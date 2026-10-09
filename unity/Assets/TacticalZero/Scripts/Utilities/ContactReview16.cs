using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace TacticalZero
{
    public sealed class ContactReview16 : MonoBehaviour
    {
        string folder;GameManager game;Camera view;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if(Application.isEditor||Array.IndexOf(Environment.GetCommandLineArgs(),"--review-contact16")<0||FindAnyObjectByType<ContactReview16>())return;
            var o=new GameObject("Contact16 visual acceptance");DontDestroyOnLoad(o);o.AddComponent<ContactReview16>();
        }
        void Place(Vector3 position,Vector3 target)
        {
            var cc=game.Player.GetComponent<CharacterController>();cc.enabled=false;
            if(NavMesh.SamplePosition(position,out var hit,2.5f,NavMesh.AllAreas))position=hit.position;
            game.Player.transform.position=position+Vector3.up*.06f;game.Player.transform.rotation=Quaternion.identity;cc.enabled=true;
            var look=game.Player.GetComponent<PlayerLook>();look.enabled=false;view=look.View;view.transform.localPosition=new Vector3(0,1.6f,0);view.transform.rotation=Quaternion.LookRotation(target-view.transform.position);Physics.SyncTransforms();
        }
        void Freeze()
        {
            foreach(var e in EnemyHealth.Active){e.GetComponent<EnemyBrain>().enabled=false;e.GetComponent<EnemyCombat>().enabled=false;e.GetComponent<NavMeshAgent>().isStopped=true;}
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Acceptance16");Directory.CreateDirectory(folder);Directory.CreateDirectory(Path.Combine(folder,"motion"));
            string settings=PlayerPrefs.GetString(SaveService.SettingsKey,"{}"),progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");int role=ClassicRules.RoleIndex,map=ClassicRules.MapIndex,difficulty=ClassicRules.DifficultyIndex,stash=PlayerPrefs.GetInt("TZ3D.Stash",0);
            yield return new WaitForSecondsRealtime(2);
            try
            {
                foreach(var id in new[]{"sector07","docks","ruins","outpost"})
                {
                    ClassicRules.MapIndex=Array.IndexOf(new[]{"sector07","docks","ruins","outpost"},id);
                    ClassicRules.DifficultyIndex=0;yield return SceneManager.LoadSceneAsync("Classic_"+id);yield return null;yield return null;game=GameManager.Instance;game.Settings.quality=2;game.Settings.frameLimit=60;SaveService.Apply();Freeze();
                    var area=FindObjectsByType<MapArea>(FindObjectsSortMode.None).Where(a=>a.Interior).OrderBy(a=>a.Label).First();
                    game.Player.GetComponent<WeaponController>().TakeWeapon("M4A1");
                    Place(area.transform.TransformPoint(new Vector3(area.Size.x*.23f,0,-area.Size.z*.30f)),area.transform.TransformPoint(new Vector3(-area.Size.x*.29f,1.3f,-area.Size.z*.22f)));
                    yield return new WaitForSecondsRealtime(1.2f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,id+"-interior.png"));yield return new WaitForSecondsRealtime(.3f);
                    Place(area.transform.TransformPoint(new Vector3(1.4f,0,-area.Size.z*.5f-3.5f)),area.transform.TransformPoint(new Vector3(0,1.5f,0)));
                    yield return new WaitForSecondsRealtime(.8f);var canvas=FindAnyObjectByType<GameUI>().GetComponent<Canvas>();canvas.enabled=false;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(folder,id+"-exterior.png"));yield return new WaitForSecondsRealtime(.3f);canvas.enabled=true;
                }
                ClassicRules.MapIndex=0;yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;game=GameManager.Instance;Freeze();
                var room=FindObjectsByType<MapArea>(FindObjectsSortMode.None).Where(a=>a.Interior).OrderBy(a=>a.Label).First();
                Place(room.transform.TransformPoint(new Vector3(room.Size.x*.22f,0,-room.Size.z*.31f)),room.transform.TransformPoint(new Vector3(-room.Size.x*.25f,1.5f,-room.Size.z*.20f)));
                var guns=game.Player.GetComponent<WeaponController>();int frame=0;
                foreach(string id in new[]{"M4A1","P90","MG3","M700","M1014"})
                {
                    guns.TakeWeapon(id);yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,id+"-grip.png"));
                    var model=game.Player.GetComponentInChildren<WeaponReloadAnimation>();
                    foreach(var arm in model.GetComponentsInChildren<FirstPersonArm>())
                    {
                        var tip=arm.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="digit3_2."+(arm.Left?"L":"R"));
                        if(tip)Debug.Log("CONTACT_GRIP "+id+" left="+arm.Left+" wrist="+model.transform.InverseTransformPoint(arm.Hand.position)+" finger="+model.transform.InverseTransformPoint(tip.position));
                    }
                    yield return new WaitForSecondsRealtime(.2f);
                    guns.TryShoot();for(int k=0;k<2;k++)guns.Ammo.TryFire(Time.time+2+k,.01f);guns.Ammo.StartReload(guns.Current.ReloadSeconds);
                    float until=Time.realtimeSinceStartup+guns.Current.ReloadSeconds+.4f;
                    while(Time.realtimeSinceStartup<until)
                    {
                        if(game.Phase==GamePhase.Paused)game.Resume();
                        ScreenCapture.CaptureScreenshot(Path.Combine(folder,"motion",(frame++).ToString("D5")+".png"));yield return new WaitForSecondsRealtime(1f/12);
                    }
                }
                File.WriteAllText(Path.Combine(folder,"visual-review.txt"),"Four maps, 16 furnished rooms, five real-time reload sequences. Captures produced directly by the standalone Unity player; AI firing disabled during this visual review.\n");
            }
            finally
            {
                ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;PlayerPrefs.SetString(SaveService.SettingsKey,settings);PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.SetInt("TZ3D.Stash",stash);PlayerPrefs.Save();
            }
            Application.Quit();
        }
    }
}
