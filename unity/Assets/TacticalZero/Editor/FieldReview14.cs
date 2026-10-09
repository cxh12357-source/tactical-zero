using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace TacticalZero.Editor
{
    public static class FieldReview14
    {
        public static void Render()
        {
            foreach(var role in ClassicRules.Data.roles)
            {
                string path=ClassicArt.Root+"/Operators/"+role.id+".prefab";var model=PrefabUtility.LoadPrefabContents(path);
                try{foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=false;PrefabUtility.SaveAsPrefabAsset(model,path);}finally{PrefabUtility.UnloadPrefabContents(model);}
            }
            ShaderUtil.allowAsyncCompilation=false;Directory.CreateDirectory("TestResults");var prior=SceneManager.GetActiveScene();if(prior.IsValid()&&string.IsNullOrEmpty(prior.path))EditorSceneManager.SaveScene(prior,AssetDatabase.GenerateUniqueAssetPath("Assets/TacticalZero/Scenes/PreservedFieldReview.unity"));var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var stage=new GameObject("Weapon photography");stage.transform.position=new Vector3(2000,2000,2000);
            try
            {
                RenderSettings.ambientLight=Color.gray;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.defaultReflectionMode=UnityEngine.Rendering.DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=Resources.Load<Cubemap>("Classic/Textures/FieldReflection14");RenderSettings.reflectionIntensity=.85f;
                var camera=new GameObject("Weapon camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.orthographic=true;camera.orthographicSize=.43f;camera.transform.localPosition=new Vector3(1.4f,.75f,.85f);camera.transform.LookAt(stage.transform.position+Vector3.forward*.04f);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.08f,.095f);camera.farClipPlane=10;
                var light=new GameObject("Key light").AddComponent<Light>();light.transform.SetParent(stage.transform,false);light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(40,-30,0);light.intensity=2;
                var sheet=new Texture2D(1600,1000,TextureFormat.RGB24,false);int i=0;
                foreach(string id in new[]{"P90","MG3","M700","M1014"})
                {
                    var model=Object.Instantiate(Resources.Load<GameObject>("Classic/Models/"+id),stage.transform);
                    foreach(var t in model.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Reload left hand")||t.name.StartsWith("Reload right hand"))t.gameObject.SetActive(false);
                    var frame=Capture(camera,800,500);sheet.SetPixels(i%2*800,(1-i/2)*500,800,500,frame.GetPixels());File.WriteAllBytes("TestResults/field14-"+id+".png",frame.EncodeToPNG());Object.DestroyImmediate(frame);Object.DestroyImmediate(model);i++;
                }
                sheet.Apply();File.WriteAllBytes("TestResults/field14-weapons.png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
            }
            finally{Object.DestroyImmediate(stage);EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
        public static Texture2D Capture(Camera camera,int width,int height)
        {
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.Render();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);return texture;
        }
    }
}

