using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace TacticalZero.Editor
{
    public static class ArtReview06
    {
        public static void Render()
        {
            ShaderUtil.allowAsyncCompilation=false;
            Directory.CreateDirectory("TestResults");var prior=SceneManager.GetActiveScene();
            if(prior.IsValid()&&string.IsNullOrEmpty(prior.path))EditorSceneManager.SaveScene(prior,AssetDatabase.GenerateUniqueAssetPath("Assets/TacticalZero/Scenes/PreservedArtWorkspace.unity"));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var stage=new GameObject("Art review stage");stage.transform.position=new Vector3(2000,2000,2000);
            try
            {
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.46f,.52f);
                var camera=new GameObject("Review camera").AddComponent<Camera>();camera.transform.SetParent(stage.transform,false);camera.transform.localPosition=new Vector3(.9f,1.25f,3.65f);camera.transform.LookAt(stage.transform.position+Vector3.up*1.02f);camera.fieldOfView=34;camera.nearClipPlane=.05f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.06f,.078f);camera.cullingMask=1<<LayerMask.NameToLayer("TZViewModel");
                for(int i=0;i<3;i++){var l=new GameObject("Studio light").AddComponent<Light>();l.transform.SetParent(stage.transform,false);l.type=LightType.Directional;l.transform.rotation=Quaternion.Euler(i==0?35:20,i==0?-35:i==1?130:210,0);l.intensity=i==0?1.5f:.65f;l.color=i==0?new Color(1,.87f,.72f):new Color(.53f,.73f,1);l.cullingMask=camera.cullingMask;}
                var atlas=new Texture2D(1600,1200,TextureFormat.RGB24,false);int n=0;
                foreach(var role in ClassicRules.Data.roles)
                {
                    var model=Object.Instantiate(Resources.Load<GameObject>("Classic/Operators/"+role.id),stage.transform);foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=LayerMask.NameToLayer("TZViewModel");
                    if(model.TryGetComponent<LODGroup>(out var lod))lod.ForceLOD(0);
                    var image=Capture(camera,400,600);File.WriteAllBytes("TestResults/art06-"+role.id+".png",image.EncodeToPNG());atlas.SetPixels((n%4)*400,(1-n/4)*600,400,600,image.GetPixels());n++;Object.DestroyImmediate(image);Object.DestroyImmediate(model);
                }
                atlas.Apply();File.WriteAllBytes("TestResults/art06-operators.png",atlas.EncodeToPNG());Object.DestroyImmediate(atlas);
                var fieldGuns=System.Array.FindAll(ClassicRules.Data.weapons,g=>g.tier!="airdrop");int gunRows=Mathf.CeilToInt(fieldGuns.Length/4f);var gunAtlas=new Texture2D(1600,gunRows*300,TextureFormat.RGB24,false);n=0;
                camera.transform.localPosition=new Vector3(1,.5f,1.05f);camera.transform.LookAt(stage.transform.position+Vector3.forward*.12f);camera.orthographic=true;camera.orthographicSize=.35f;
                foreach(var gun in fieldGuns)
                {
                    var model=Object.Instantiate(Resources.Load<GameObject>("Classic/Models/"+gun.id),stage.transform);
                    foreach(var t in model.GetComponentsInChildren<Transform>()){t.gameObject.layer=LayerMask.NameToLayer("TZViewModel");string name=t.name.ToLowerInvariant();if(name.Contains("sleeve")||name.Contains("glove")||name.Contains("hand")||name.Contains("finger"))t.gameObject.SetActive(false);}
                    var image=Capture(camera,400,300);File.WriteAllBytes("TestResults/bodycam15-gun-"+gun.id+".png",image.EncodeToPNG());gunAtlas.SetPixels((n%4)*400,(gunRows-1-n/4)*300,400,300,image.GetPixels());n++;Object.DestroyImmediate(image);Object.DestroyImmediate(model);
                }
                gunAtlas.Apply();File.WriteAllBytes("TestResults/art06-weapons.png",gunAtlas.EncodeToPNG());Object.DestroyImmediate(gunAtlas);
            }
            finally{Object.DestroyImmediate(stage);EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
        static Texture2D Capture(Camera camera,int width,int height)
        {
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.Render();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);return texture;
        }
    }
}
