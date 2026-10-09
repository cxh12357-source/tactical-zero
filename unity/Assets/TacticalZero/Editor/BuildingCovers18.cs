using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
namespace TacticalZero.Editor
{
    public static class BuildingCovers18
    {
        public static void Render()
        {
            Directory.CreateDirectory("Assets/TacticalZero/Art/Building18/Concepts");Directory.CreateDirectory("Assets/TacticalZero/Resources/Maps/Previews");
            Vector3[] positions={new Vector3(3,2,-3),new Vector3(-12,1.8f,-9),new Vector3(-10.2f,11.65f,-12.7f),new Vector3(-10,2.7f,-5)};
            Vector3[] targets={new Vector3(-15,4,7),new Vector3(13,1.6f,7),new Vector3(-14,11,-8.5f),new Vector3(13,5.2f,4)};
            for(int i=0;i<4;i++)
            {
                var map=BuildingCatalog.Maps[i];EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity");
                foreach(var player in Object.FindObjectsByType<FPSPlayerController>(FindObjectsSortMode.None))player.gameObject.SetActive(false);
                foreach(var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))cam.enabled=false;
                foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)){label.font=UIFactory.Font;label.font.RequestCharactersInTexture(label.text,label.fontSize);label.GetComponent<MeshRenderer>().sharedMaterial=BuildingSign.WorldMaterial;}
                if(i==2)foreach(var door in Object.FindObjectsByType<BuildingDoor>(FindObjectsSortMode.None))if(door.Floor==2){door.SendMessage("Awake");door.SetOpen(true);}
                var camera=new GameObject("Offline cover camera").AddComponent<Camera>();camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);camera.fieldOfView=i==0?78:72;camera.nearClipPlane=.08f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.31f,.36f,.4f);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
                Capture(camera,"Assets/TacticalZero/Art/Building18/Concepts/"+map.Id+".png",1280,720);
                Capture(camera,"Assets/TacticalZero/Resources/Maps/Previews/"+map.Id+".png",640,360);
                Debug.Log("COVER18_RENDERED "+map.Id+" actualScene="+map.Scene+" position="+positions[i]+" target="+targets[i]);
            }
            AssetDatabase.Refresh();
            foreach(var map in BuildingCatalog.Maps)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/TacticalZero/Resources/Maps/Previews/"+map.Id+".png");importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
            }
            EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/MainMenu.unity");AssetDatabase.SaveAssets();
        }
        static void Capture(Camera camera,string path,int w,int h)
        {
            var rt=new RenderTexture(w,h,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(w,h,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=old;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }
    }
}
