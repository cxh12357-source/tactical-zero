using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace TacticalZero.Tests
{
    public sealed class FreeformMapTests
    {
        int priorMap,priorDifficulty;bool priorAim;string progress;
        [SetUp]public void Before(){priorMap=ClassicRules.MapIndex;priorDifficulty=ClassicRules.DifficultyIndex;priorAim=ClassicRules.AutoAim;progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");ClassicRules.DifficultyIndex=0;ClassicRules.AutoAim=false;}
        [UnityTearDown]public IEnumerator After(){ClassicRules.MapIndex=priorMap;ClassicRules.DifficultyIndex=priorDifficulty;ClassicRules.AutoAim=priorAim;PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();if(GameManager.Instance)GameManager.Instance.MainMenu();yield return null;yield return null;}
        [UnityTest]public IEnumerator FreeformMapsKeepLootRespawnsAndExtractionOnConnectedTerrain()
        {
            for(int m=0;m<4;m++)
            {
                ClassicRules.MapIndex=m;yield return SceneManager.LoadSceneAsync(ClassicRules.Map.Scene);yield return null;yield return null;
                foreach(var enemy in EnemyHealth.Active){enemy.GetComponent<EnemyBrain>().enabled=false;enemy.GetComponent<NavMeshAgent>().enabled=false;}
                var mission=ClassicEncounter.Instance;var game=GameManager.Instance;var geometry=GameObject.Find("Static architecture");Assert.IsNotNull(geometry.transform.Find("Freeform layout 12"));
                int oblique=0,rooms=0;foreach(var collider in geometry.GetComponentsInChildren<BoxCollider>())if(collider.name=="Perimeter"&&Mathf.Abs(Mathf.Sin(collider.transform.eulerAngles.y*Mathf.Deg2Rad*2))>.15f)oblique++;
                Assert.Greater(oblique,6,"Map boundary must be authored beyond a rectangle");foreach(var room in MapArea.Active)if(room.Interior)rooms++;Assert.GreaterOrEqual(rooms,4);
                foreach(var point in Object.FindObjectsByType<MapRoutePoint>(FindObjectsSortMode.None))CheckPath(game.Player.transform.position,point.transform.position,point.name);
                foreach(var supply in mission.Supplies)if(supply&&supply.gameObject.activeSelf)CheckPath(game.Player.transform.position,supply.GroundPosition,supply.name);
                var previous=game.Player.transform.position;game.Player.Apply(new DamageInfo(1000,previous+Vector3.forward*5,"测试","USP"));yield return null;Assert.IsTrue(game.Respawn());yield return null;
                Assert.Greater(Vector3.Distance(previous,game.Player.transform.position),8);CheckPath(game.Player.transform.position,mission.Extraction,"Respawn to extraction");
                var hud=Object.FindFirstObjectByType<ClassicCombatUI>();var field=typeof(ClassicCombatUI).GetField("mapTexture",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);var map=(Texture2D)field.GetValue(hud);Assert.AreEqual(168,map.width);Assert.AreEqual(120,map.height);
            }
        }
        static void CheckPath(Vector3 from,Vector3 to,string label)
        {Assert.IsTrue(NavMesh.SamplePosition(to,out var hit,.8f,NavMesh.AllAreas),label);var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(from,hit.position,NavMesh.AllAreas,path),label);Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,label);}
    }
}
