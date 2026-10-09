using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace TacticalZero.Editor
{
    public static class LayoutReview12
    {
        public static void ReviewAndBuild(){Render();ArtReview06.Render();TacticalZeroBuild.Windows();}
        public static void Generate(){MapLayout12.Apply(true);VisualPolish10.Apply();Render();}
        public static void Render()
        {
            Directory.CreateDirectory("TestResults");ShaderUtil.allowAsyncCompilation=false;
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.43f,.46f,.49f);
                    var camera=new GameObject("Layout review camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,75,-22);camera.transform.LookAt(Vector3.zero);camera.orthographic=true;camera.orthographicSize=33;camera.farClipPlane=250;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.05f,.07f);camera.cullingMask=~LayerMask.GetMask("TZPlayer","TZViewModel");
                    Capture(camera,"TestResults/layout12-"+map.id+"-overview.png",1440,1000);
                    camera.clearFlags=CameraClearFlags.Skybox;camera.orthographic=false;camera.fieldOfView=74;camera.transform.position=map.id=="outpost"?new Vector3(-15,1.65f,-5):map.id=="docks"?new Vector3(-21,1.65f,-6):map.id=="ruins"?new Vector3(-8,1.65f,-14):new Vector3(-13,1.65f,-10);camera.transform.LookAt(new Vector3(2,2,6));
                    Capture(camera,"TestResults/layout12-"+map.id+"-street.png",1440,900);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
        }
        static void Capture(Camera camera,string path,int width,int height)
        {
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;camera.Render();camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
        }
    }
}
