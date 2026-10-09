using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TacticalZero.Tests
{
    public sealed class Building18Regression
    {
        [UnityTearDown] public IEnumerator Clean(){Time.captureDeltaTime=0;Time.timeScale=1;if(GameManager.Instance){GameManager.Instance.Input.Clear();GameManager.Instance.Input.ForceTouch=false;}yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;}
        [UnityTest] public IEnumerator HarborSpawns()=>CheckMap(0);
        [UnityTest] public IEnumerator OfficeSpawns()=>CheckMap(1);
        [UnityTest] public IEnumerator HospitalSpawns()=>CheckMap(2);
        [UnityTest] public IEnumerator FactorySpawns()=>CheckMap(3);
        IEnumerator CheckMap(int map)
        {
            for(int once=0;once<1;once++)
            {
                Random.InitState(18018);yield return SceneManager.LoadSceneAsync(BuildingCatalog.Maps[map].Scene);yield return null;yield return null;yield return null;
                var m=BuildingMission.Instance;Assert.Greater(m.Total,0,m.Definition.Id);
                foreach(var npc in m.Roster){npc.GetComponent<BuildingEnemy>().enabled=false;npc.GetComponent<EnemyCombat>().enabled=false;}
                Assert.IsEmpty(m.ValidateNavigation(),m.Definition.Id);
                for(int f=0;f<m.FloorRoots.Length;f++)
                {
                    int count=m.FloorCount(f);Assert.GreaterOrEqual(count,map==0&&f==3?4:8);
                    for(int i=0;i<50;i++)
                    {
                        int room=m.FloorStart(f)+i%count;var point=m.FindSpawn(room,Random.Range(0,81),.3f,1.8f,out var support);
                        Assert.IsTrue(BuildingSupport18.Validate(point,f,.3f,1.8f,out _,out _),m.Definition.Id+" floor="+f+" room="+room+" "+point);
                        Assert.IsNotEmpty(support);Assert.AreEqual(f,BuildingMission.FloorAt(point));
                    }
                    Debug.Log("SPAWN18_CHECK map="+m.Definition.Id+" floor="+f+" samples=50 rooms="+count);
                }
            }
        }
        [UnityTest] public IEnumerator RemovedCapabilitiesAndVisualAudioCannotReturnFromOldSettings()
        {
            PlayerPrefs.SetInt("TZ3D.Building.SoundVisual",1);PlayerPrefs.SetInt("TZ3D.Role",7);
            yield return SceneManager.LoadSceneAsync("Classic_Sweep_harbor");yield return null;yield return null;yield return null;
            Assert.AreEqual(0,ClassicRules.RoleIndex);Assert.IsFalse(System.Enum.IsDefined(typeof(MobileAction),"Skill"));
            Assert.IsNull(typeof(BuildingMission).Assembly.GetType("TacticalZero.OperatorSkill"));
            foreach(var text in Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
            {
                Assert.AreNotEqual("Sound cue",text.name);Assert.AreNotEqual("Operator skill",text.name);
                if(text.gameObject.activeInHierarchy){Assert.IsFalse(text.text.Contains("Q 技能"));Assert.IsFalse(text.text.Contains("听声提示强度"));}
            }
            foreach(var control in Object.FindObjectsByType<TouchControl>(FindObjectsSortMode.None))Assert.IsFalse(control.name.Contains("技能"));
            Assert.IsNull(GameManager.Instance.Player.GetComponent<SpecialEquipment>());
        }
        [UnityTest] public IEnumerator NativeAudioCameraDirectionDoorFilteringAndMuteIndependentHearing()
        {
            yield return SceneManager.LoadSceneAsync("Classic_Sweep_office");yield return null;yield return null;yield return null;
            var m=BuildingMission.Instance;foreach(var npc in m.Roster){npc.GetComponent<BuildingEnemy>().enabled=false;npc.GetComponent<EnemyCombat>().enabled=false;}
            var door=Object.FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None)[0];door.SetOpen(false);Physics.SyncTransforms();
            Vector3 normal=door.SlideDirection.x>.5f?Vector3.forward:Vector3.right;Vector3 a=door.transform.position-normal*1.5f,b=door.transform.position+normal*1.5f;
            var shut=BuildingAcoustics.Trace(a,b);door.SetOpen(true);Physics.SyncTransforms();var open=BuildingAcoustics.Trace(a,b);
            Assert.Greater(open.Gain,shut.Gain);Assert.Greater(open.Cutoff,shut.Cutoff);
            var camera=GameManager.Instance.Player.GetComponent<PlayerLook>().View;Assert.IsNotNull(camera.GetComponent<AudioListener>());
            var clip=AudioClip.Create("spatial test",44100,1,44100,false);AudioService.Instance.PlayClip(clip,camera.transform.position-camera.transform.right*4);
            AudioSource source=null;foreach(var v in AudioService.Instance.GetComponentsInChildren<AudioSource>())if(v.clip==clip)source=v;
            Assert.IsNotNull(source);Assert.AreEqual(1,source.spatialBlend);Assert.AreEqual(0,source.panStereo);
            Assert.Less(camera.transform.InverseTransformPoint(source.transform.position).x,0);camera.transform.Rotate(0,180,0);Assert.Greater(camera.transform.InverseTransformPoint(source.transform.position).x,0);
            var brain=m.Roster[0].GetComponent<BuildingEnemy>();var agent=brain.GetComponent<NavMeshAgent>();agent.Warp(new Vector3(-19.5f,0,-10));brain.enabled=true;
            var old=SaveService.Settings.sfxVolume;SaveService.Settings.sfxVolume=0;
            var heard=new Vector3(-19.5f,0,-5);NoiseBus.Emit(heard,30,NoiseKind.Gunshot);Assert.AreEqual(heard,brain.LastKnown);SaveService.Settings.sfxVolume=old;
            source.Stop();Object.Destroy(clip);yield return null;
        }
        [UnityTest] public IEnumerator PlacementReusePauseAndRestartPreserveFixedRosterAndSupport()
        {
            int count=-1;
            for(int round=0;round<3;round++)
            {
                Random.InitState(18018);yield return SceneManager.LoadSceneAsync("Classic_Sweep_factory");yield return null;yield return null;yield return null;
                var m=BuildingMission.Instance;foreach(var npc in m.Roster){npc.GetComponent<BuildingEnemy>().enabled=false;npc.GetComponent<EnemyCombat>().enabled=false;}
                if(count<0)count=m.Total;Assert.AreEqual(count,m.Total);Assert.AreEqual(count,EnemyHealth.Active.Count);
                var e=m.Roster[0];var actor=e.GetComponent<BuildingActor18>();var agent=e.GetComponent<NavMeshAgent>();
                for(int i=0;i<12;i++){int room=i%m.SpawnZones.Length;var at=m.FindSpawn(room,i,.3f,1.8f,out var support);actor.Place(at,m.Layout.Rooms[room].Floor,support);Assert.Less(agent.velocity.sqrMagnitude,.001f);Assert.IsFalse(agent.hasPath);Assert.IsTrue(BuildingSupport18.Find(e.transform.position,actor.FloorId,out _));}
                var before=e.transform.position;GameManager.Instance.Pause();yield return new WaitForSecondsRealtime(.15f);GameManager.Instance.Resume();yield return new WaitForSeconds(.1f);Assert.Less(Vector3.Distance(before,e.transform.position),.1f);
                Assert.LessOrEqual(AudioService.Instance.GetComponentsInChildren<AudioSource>().Length,16);
            }
        }
        [UnityTest] public IEnumerator LowFrameRateStairPursuit(){Time.captureDeltaTime=1f/15;yield return StairPursuitNeverCarriesLiveNpcThroughSolidCeiling();Time.captureDeltaTime=0;}
        [UnityTest] public IEnumerator StairPursuitNeverCarriesLiveNpcThroughSolidCeiling()
        {
            Random.InitState(18018);yield return SceneManager.LoadSceneAsync("Classic_Sweep_harbor");yield return null;yield return null;yield return null;
            var m=BuildingMission.Instance;var e=m.Roster[0];foreach(var npc in m.Roster){npc.GetComponent<BuildingEnemy>().enabled=false;npc.GetComponent<EnemyCombat>().enabled=false;}
            var agent=e.GetComponent<NavMeshAgent>();agent.Warp(BuildingMission.Stair(0,-1,true));agent.speed=3;agent.SetDestination(BuildingMission.Stair(1,-1,false));
            float until=Time.time+18;int ceilingHits=0;float worst=0;
            while(Time.time<until&&Vector3.Distance(e.transform.position,BuildingMission.Stair(1,-1,false))>.5f)
            {
                var p=e.transform.position;var hits=Physics.OverlapCapsule(p+Vector3.up*.38f,p+Vector3.up*1.4f,.25f,LayerMask.GetMask("TZWorld"));
                foreach(var h in hits)if(h.name=="吊顶"){ceilingHits++;worst=p.y;}
                yield return null;
            }
            Debug.Log("NPC18_REPRO solidCeilingIntersections="+ceilingHits+" lastHeight="+worst+" final="+e.transform.position);
            Assert.AreEqual(0,ceilingHits,"Live NavMesh NPC crossed solid ceiling during the real stair pursuit");
            Assert.Less(Vector3.Distance(e.transform.position,BuildingMission.Stair(1,-1,false)),.6f);
        }
    }
}
