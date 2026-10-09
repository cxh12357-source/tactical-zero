using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TacticalZero.Tests
{
    public sealed class BlenderOperatorTests
    {
        int priorRole;bool priorAim;string progress;
        [SetUp]public void Before(){priorRole=ClassicRules.RoleIndex;priorAim=ClassicRules.AutoAim;progress=PlayerPrefs.GetString(SaveService.ProgressKey,"{}");}
        [UnityTearDown]public IEnumerator After()
        {
            ClassicRules.RoleIndex=priorRole;ClassicRules.AutoAim=priorAim;PlayerPrefs.SetString(SaveService.ProgressKey,progress);PlayerPrefs.Save();
            if(GameManager.Instance)GameManager.Instance.MainMenu();yield return null;yield return null;
        }
        [Test]public void EightRefinedModelsKeepSkinWeightsTexturesAndBlinkShapes()
        {
            foreach(var role in ClassicRules.Data.roles)
            {
                var prefab=Resources.Load<GameObject>("Classic/Operators/"+role.id);Assert.IsNotNull(prefab);
                Assert.IsTrue(prefab.GetComponent<OperatorRig>().refined,role.id+" still uses the old model");
                var instance=Object.Instantiate(prefab);
                try
                {
                    var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.AreEqual(3,skins.Length);
                    int total=0;var bounds=skins[0].bounds;
                    foreach(var skin in skins)
                    {
                        var mesh=skin.sharedMesh;Assert.GreaterOrEqual(skin.bones.Length,30);Assert.AreEqual(mesh.bindposes.Length,skin.bones.Length);
                        Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);Assert.AreEqual(mesh.vertexCount,mesh.boneWeights.Length);
                        Assert.Greater(mesh.triangles.Length,0);total+=mesh.triangles.Length/3;
                        foreach(var weight in mesh.boneWeights)Assert.That(weight.weight0+weight.weight1+weight.weight2+weight.weight3,Is.InRange(.99f,1.01f));
                        foreach(var bone in skin.bones)Assert.IsNotNull(bone);
                        foreach(var material in skin.sharedMaterials){Assert.IsNotNull(material);Assert.AreNotEqual("Hidden/InternalErrorShader",material.shader.name);}
                        Assert.IsTrue(System.Array.Exists(skin.sharedMaterials,m=>m.GetTexture("_BaseMap")!=null));
                        if(role.id!="guardian"){Assert.GreaterOrEqual(mesh.blendShapeCount,2);StringAssert.Contains("Blink",mesh.GetBlendShapeName(0));}
                        bounds.Encapsulate(skin.bounds);
                    }
                    Assert.Greater(total,10000);Assert.That(bounds.size.y,Is.InRange(1.8f,2.05f));
                    Assert.IsNotNull(instance.transform.Find("LeftLeg"));Assert.IsNotNull(instance.transform.Find("RightLeg"));
                    Assert.AreEqual(0,instance.GetComponentsInChildren<Collider>(true).Length);
                }
                finally{Object.DestroyImmediate(instance);}
            }
        }
        [UnityTest]public IEnumerator RefinedPreviewActuallyDeformsTheMesh()
        {
            var instance=Object.Instantiate(Resources.Load<GameObject>("Classic/Operators/assault"));
            var rig=instance.GetComponent<OperatorRig>();rig.ReviewMotion=true;
            yield return null;
            var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>();var before=new Mesh();var after=new Mesh();skin.BakeMesh(before);
            yield return new WaitForSeconds(.2f);skin.BakeMesh(after);
            float displacement=0;var a=before.vertices;var b=after.vertices;for(int i=0;i<a.Length;i++)displacement=Mathf.Max(displacement,(a[i]-b[i]).magnitude);
            Object.Destroy(before);Object.Destroy(after);Object.Destroy(instance);
            Assert.Greater(displacement,.005f,"Bone animation must deform the imported skin");
        }
        [UnityTest]public IEnumerator ImportedPlayerBodyRespectsFirstPersonAndDeathVisibility()
        {
            ClassicRules.RoleIndex=0;ClassicRules.AutoAim=false;
            yield return SceneManager.LoadSceneAsync("Classic_sector07");yield return null;yield return null;
            var game=GameManager.Instance;var p=game.Player;var rig=p.GetComponentInChildren<OperatorRig>();Assert.IsNotNull(rig);Assert.IsNotNull(rig.GetComponent<LODGroup>());
            foreach(var renderer in rig.GetComponentsInChildren<Renderer>())
            {
                bool leg=renderer.transform.IsChildOf(rig.transform.Find("LeftLeg"))||renderer.transform.IsChildOf(rig.transform.Find("RightLeg"));
                Assert.AreEqual(leg?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly,renderer.shadowCastingMode);
            }
            p.Apply(new DamageInfo(1000,p.transform.position+Vector3.forward*5,"测试","USP"));yield return null;yield return null;
            foreach(var renderer in rig.GetComponentsInChildren<Renderer>())Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.On,renderer.shadowCastingMode);
            Assert.IsTrue(game.Respawn());yield return null;Assert.AreEqual(GamePhase.Playing,game.Phase);
            game.MainMenu();yield return null;
        }
    }
}
