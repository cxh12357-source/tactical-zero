using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class ExpandedMapTests
    {
        int role,map,difficulty;bool aim;string progress;
        [UnitySetUp]public IEnumerator Setup()
        {
            role=ClassicRules.RoleIndex;map=ClassicRules.MapIndex;difficulty=ClassicRules.DifficultyIndex;aim=ClassicRules.AutoAim;progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");
            ClassicRules.RoleIndex=ClassicRules.MapIndex=ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;Freeze();
        }
        static void Freeze(){foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}}
        [UnityTearDown]public IEnumerator Cleanup()
        {
            GameManager.Instance.Input.ForceTouch=false;GameManager.Instance.Input.Mobile.Clear();ClassicRules.RoleIndex=role;ClassicRules.MapIndex=map;ClassicRules.DifficultyIndex=difficulty;ClassicRules.AutoAim=aim;
            PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();GameManager.Instance.MainMenu();yield return null;yield return null;
        }
        static void Warp(Vector3 at)
        {
            var p=GameManager.Instance.Player;var cc=p.GetComponent<CharacterController>();cc.enabled=false;p.transform.position=at+Vector3.up*.08f;cc.enabled=true;
            p.GetComponent<FPSPlayerController>().ResetMotion();p.GetComponent<PlayerVisibilityState>().ResetForRespawn();p.transform.rotation=Quaternion.identity;p.GetComponent<PlayerLook>().ResetView();Physics.SyncTransforms();
        }
        [UnityTest]public IEnumerator EveryRoomAndLookoutIsReachableAndPlayerCanClimbEachRamp()
        {
            for(int m=0;m<4;m++)
            {
                ClassicRules.MapIndex=m;yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;Freeze();
                var game=GameManager.Instance;int rooms=0;foreach(var area in MapArea.Active)if(area.Interior)rooms++;
                Assert.GreaterOrEqual(rooms,3,ClassicRules.Map.id+" room count");
                MapRoutePoint entrance=null,lookout=null;
                var points=Object.FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None);Assert.GreaterOrEqual(points.Length,12,"Authored navigation markers must survive scene serialization");
                foreach(var point in points)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(point.transform.position,out var target,.8f,NavMesh.AllAreas),ClassicRules.Map.id+" "+point.name+" must lie on navigation");
                    var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(game.Player.transform.position,target.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,ClassicRules.Map.id+" "+point.transform.parent.name+" / "+point.name);
                    if(point.name=="Ramp entry")entrance=point;if(point.name=="Lookout route")lookout=point;
                }
                Assert.IsNotNull(entrance);Assert.IsNotNull(lookout);Warp(entrance.transform.position);yield return new WaitForSeconds(.2f);
                game.Input.ForceTouch=true;game.Input.Mobile.Move=Vector2.up;yield return new WaitForSeconds(3f);game.Input.Mobile.Move=Vector2.zero;
                Assert.Greater(game.Player.transform.position.y,1.8f,ClassicRules.Map.id+" character must climb the actual ramp");
                ClassicCampaignTests.Capture("map07-lookout-"+ClassicRules.Map.id);game.Input.ForceTouch=false;
                MapArea room=null;foreach(var area in MapArea.Active)if(area.Interior){room=area;break;}
                Warp(room.transform.position+Vector3.back*room.Size.z*.24f);yield return new WaitForSeconds(.2f);
                Assert.AreEqual(room.Label,MapArea.NameAt(game.Player.transform.position));ClassicCampaignTests.Capture("map07-room-"+ClassicRules.Map.id);
            }
        }
        [UnityTest]public IEnumerator LowBrushRequiresCrouchingAndShotsAndSprintRevealPlayer()
        {
            var game=GameManager.Instance;GrassVolume low=null,tall=null;
            foreach(var grass in Object.FindObjectsByType<GrassVolume>(FindObjectsSortMode.None)){foreach(var c in grass.GetComponents<Collider>())Assert.IsTrue(c.isTrigger,"Grass must never be solid");if(grass.ConcealsStanding)tall=grass;else low=grass;}
            Assert.IsNotNull(low);Assert.IsNotNull(tall);var state=game.Player.GetComponent<PlayerVisibilityState>();
            // Isolate authored brush behavior from overlapping decorative volumes in the later art pass.
            ClassicCampaignTests.IsolatedRange();game.Input.ForceTouch=true;game.Input.Clear();
            var lowBox=new GameObject("Isolated low brush");lowBox.transform.position=new Vector3(980,1,980);lowBox.AddComponent<BoxCollider>().size=new Vector3(8,2,8);low=lowBox.AddComponent<GrassVolume>();low.ConcealsStanding=false;
            var tallBox=new GameObject("Isolated tall brush");tallBox.transform.position=new Vector3(1000,1,980);tallBox.AddComponent<BoxCollider>().size=new Vector3(8,2,8);tall=tallBox.AddComponent<GrassVolume>();tall.ConcealsStanding=true;
            Warp(new Vector3(980,0,980));yield return new WaitForSeconds(.25f);Assert.IsTrue(state.HiddenInGrass);Assert.IsFalse(state.ConcealedByGrass);
            game.Input.ForceTouch=true;game.Input.Mobile.Crouch=true;yield return new WaitForSeconds(.2f);Assert.IsTrue(state.ConcealedByGrass);
            state.OnShot();Assert.IsTrue(state.Revealed);yield return new WaitForSeconds(2.6f);Assert.IsFalse(state.Revealed);
            game.Input.Mobile.Crouch=false;Warp(new Vector3(1000,0,980));yield return new WaitForSeconds(.3f);Assert.IsTrue(state.ConcealedByGrass);
            game.Input.Mobile.Sprint=true;game.Input.Mobile.Move=Vector2.up;yield return new WaitForSeconds(.15f);Assert.IsTrue(state.Revealed);game.Input.Mobile.Clear();
            ClassicCampaignTests.Capture("map07-grass-concealment");
            Object.Destroy(lowBox);Object.Destroy(tallBox);
        }
        [UnityTest]public IEnumerator ShallowsAndMudSlowMovementOnlyInsideTheirBounds()
        {
            ClassicRules.MapIndex=1;yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;Freeze();
            MapArea wet=null;foreach(var area in MapArea.Active)if(area.Movement<1){wet=area;break;}
            Assert.IsNotNull(wet);Assert.AreEqual(.72f,MapArea.SpeedAt(wet.transform.position),.01f);Assert.AreEqual(1,MapArea.SpeedAt(wet.transform.position+Vector3.up*3));
            Warp(wet.transform.position+Vector3.back*2);yield return new WaitForSeconds(.2f);var game=GameManager.Instance;game.Input.ForceTouch=true;game.Input.Mobile.Move=Vector2.up;yield return new WaitForSeconds(.7f);
            Assert.Less(game.Player.GetComponent<FPSPlayerController>().Speed,3.3f);game.Input.Mobile.Clear();
        }
    }
}
