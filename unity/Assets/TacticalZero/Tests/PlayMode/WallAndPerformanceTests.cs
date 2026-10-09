using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class WallAndPerformanceTests
    {
        static readonly HashSet<string> StructureNames=new HashSet<string>{"Perimeter","Concrete partition","Broken courtyard masonry","Rock ridge","Support pier","Coping","Wall footing","Fractured rock crown","Weathered ridge face","Doorway wall","Window sill wall","Window header wall","Window pier","Interior partition","Room roof","Door lintel","Interior door header","Shipping container","Cargo stack","Supply shelter","Vehicle barricade"};
        int role,map,difficulty;bool aim;string progress;
        [UnitySetUp]public IEnumerator Setup()
        {
            role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;aim=ClassicRules.AutoAim;progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return null;
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;ClassicRules.AutoAim=aim;
            PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();if(GameManager.Instance)GameManager.Instance.MainMenu();yield return null;yield return null;
        }
        static void Freeze(){foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}}
        [UnityTest]public IEnumerator VisibleStructuralSurfacesStopMovementAndShots()
        {
            var misses=new List<string>();int checkedSurfaces=0;
            for(int m=0;m<4;m++)
            {
                ClassicRules.MapIndex=m;yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;Freeze();Physics.SyncTransforms();
                var geometry=GameObject.Find("Static architecture");
                int refinements=0;foreach(var node in geometry.GetComponentsInChildren<Transform>())if(node.name=="Art refinement 02")refinements++;
                Assert.AreEqual(1,refinements,"Repeated setup must not duplicate static decorations");
                foreach(var r in geometry.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!StructureNames.Contains(r.name)&&r.name!="Roof panel")continue;
                    var b=r.bounds;var direction=b.size.x<b.size.z?Vector3.right:Vector3.forward;float extent=b.size.x<b.size.z?b.extents.x:b.extents.z;
                    foreach(int side in new[]{-1,1})
                    {
                        Vector3 origin=b.center+direction*side*(extent+.15f),end=b.center-direction*side*(extent+.15f);var ray=(end-origin).normalized;
                        bool bullet=HitscanSolver.Trace(origin,origin,ray,Vector3.Distance(origin,end),LayerMask.GetMask("TZWorld","TZInteractable"),out _);
                        bool body=Physics.SphereCast(origin,.10f,ray,out _,Vector3.Distance(origin,end),LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore);
                        checkedSurfaces++;if(!bullet||!body)misses.Add(ClassicRules.Map.id+" / "+r.name+" at "+b.center+" side="+side+" bullet="+bullet+" body="+body);
                    }
                }
                foreach(var area in MapArea.Active)if(area.Interior)foreach(int side in new[]{-1,1})
                {
                    var opening=area.transform.TransformPoint(new Vector3(side*area.Size.x*.5f,1.7f,area.Size.z*.19f));
                    bool blocked=Physics.Linecast(opening-area.transform.right*.6f,opening+area.transform.right*.6f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore);
                    if(blocked)misses.Add(ClassicRules.Map.id+" / filled window opening in "+area.Label);
                }
                foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if(r.name=="Boarded window recess"||r.name=="Recessed access panel")
                        if(!Physics.CheckSphere(r.bounds.center,.28f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore))misses.Add(ClassicRules.Map.id+" / floating wall attachment "+r.name+" at "+r.bounds.center);
            }
            System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllLines("TestResults/wall08-audit.txt",misses);
            Assert.Greater(checkedSurfaces,100);Assert.IsEmpty(misses,string.Join("\n",misses.GetRange(0,Mathf.Min(30,misses.Count)))+"\nTotal failures: "+misses.Count+" / "+checkedSurfaces);
        }
        [UnityTest]public IEnumerator MeasureMapRenderingAndHudRefresh()
        {
            var report=new List<string>();
            for(int m=0;m<4;m++)
            {
                ClassicRules.MapIndex=m;yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;Freeze();
                var hud=Object.FindFirstObjectByType<ClassicCombatUI>();var update=typeof(ClassicCombatUI).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);var next=typeof(ClassicCombatUI).GetField("next",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.IsTrue(GameManager.Instance.IsPlaying,"Benchmark must run the combat HUD");
                for(int i=0;i<10;i++){next.SetValue(hud,0f);update.Invoke(hud,null);}
                var timer=System.Diagnostics.Stopwatch.StartNew();long memory=System.GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<100;i++){next.SetValue(hud,0f);update.Invoke(hud,null);}
                long allocated=System.GC.GetAllocatedBytesForCurrentThread()-memory;timer.Stop();int grassRenderers=0;foreach(var grass in Object.FindObjectsByType<GrassVolume>(FindObjectsSortMode.None))grassRenderers+=grass.GetComponentsInChildren<Renderer>().Length;
                report.Add(ClassicRules.Map.id+": grass renderers="+grassRenderers+", all renderers="+Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length+", HUD100 ms="+timer.Elapsed.TotalMilliseconds.ToString("F3")+", allocated="+allocated);
            }
            System.IO.Directory.CreateDirectory("TestResults");System.IO.File.WriteAllLines("TestResults/wall08-performance.txt",report);foreach(var line in report)Debug.Log(line);
        }
        [UnityTest]public IEnumerator ActualCharacterAndOffsetMuzzleCannotCrossFormerGhostPier()
        {
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;Freeze();
            MeshRenderer source=null;foreach(var r in GameObject.Find("Static architecture").GetComponentsInChildren<MeshRenderer>())if(r.name=="Support pier"){source=r;break;}
            Assert.IsNotNull(source);var authored=source.GetComponent<BoxCollider>();Assert.IsNotNull(authored);
            // Copy authored collision data; a statically batched renderer cannot be moved as a clone.
            var wall=new GameObject("Isolated authored pier");wall.layer=source.gameObject.layer;wall.transform.SetPositionAndRotation(new Vector3(1000,1.85f,1000),Quaternion.identity);wall.transform.localScale=source.transform.lossyScale;
            var copied=wall.AddComponent<BoxCollider>();copied.size=authored.size;copied.center=authored.center;copied.isTrigger=authored.isTrigger;copied.enabled=authored.enabled;Physics.SyncTransforms();
            var p=GameManager.Instance.Player;var cc=p.GetComponent<CharacterController>();var previous=p.transform.position;
            try
            {
                var b=copied.bounds;var direction=b.size.x<b.size.z?Vector3.right:Vector3.forward;float extent=b.size.x<b.size.z?b.extents.x:b.extents.z;
                var start=b.center-direction*(extent+1);start.y=.05f;cc.enabled=false;p.transform.position=start;cc.enabled=true;Physics.SyncTransforms();
                for(int i=0;i<60;i++)cc.Move(direction*.1f);
                Assert.Less(Vector3.Dot(p.transform.position-b.center,direction),-extent,"Real CharacterController must stop on the near side");
                var eye=start+Vector3.up*1.4f;var muzzle=eye+direction*(extent*2+2);
                Assert.IsTrue(HitscanSolver.Trace(eye,muzzle,direction,30,LayerMask.GetMask("TZWorld","TZEnemy","TZInteractable"),out var hit));
                Assert.AreEqual(wall,hit.collider.gameObject,"Muzzle extending through a wall must still strike that wall");
            }
            finally{cc.enabled=false;p.transform.position=previous;cc.enabled=true;Object.Destroy(wall);}
        }
    }
}
