using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace TacticalZero.Editor
{
    public static class BuildingSweepBuild
    {
        const string Root="Assets/TacticalZero";
        public static void EnsureScenes()
        {
            foreach(var map in BuildingCatalog.Maps)if(!File.Exists(Root+"/Scenes/"+map.Scene+".unity")){Setup();return;}
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach(var map in BuildingCatalog.Maps){string path=Root+"/Scenes/"+map.Scene+".unity";if(!scenes.Exists(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));}
            EditorBuildSettings.scenes=scenes.ToArray();
        }
        [MenuItem("Tactical Zero/Building Sweep/Generate Buildings")]
        public static void Setup()
        {
            var catalog=Resources.Load<PrototypeCatalog>("PrototypeCatalog");
            if(!catalog||!catalog.PlayerPrefab)throw new InvalidOperationException("Original Unity player catalog missing");
            Directory.CreateDirectory(Root+"/Art/Building17");AssetDatabase.Refresh();
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for(int index=0;index<BuildingCatalog.Maps.Length;index++)
            {
                var map=BuildingCatalog.Maps[index];var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=new GameObject("Building Sweep "+map.Name);var mission=root.AddComponent<BuildingMission>();mission.MapIndex=index;
                mission.FloorMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Bodycam15/Concrete floor.mat");
                mission.WallMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Bodycam15/Aged concrete.mat");
                mission.MetalMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Bodycam15/Worn painted steel.mat");
                mission.WoodMaterial=Wood();mission.GlassMaterial=Glass();
                mission.AccentMaterial=Resources.Load<Material>("Classic/Materials/GlowTeal");
                if(!mission.FloorMaterial||!mission.WallMaterial||!mission.MetalMaterial)throw new InvalidOperationException("Original Bodycam materials missing");
                if(index==0){mission.FloorMaterial=Interior("Apartment corridor tile",new Color(.48f,.46f,.40f),1);mission.WallMaterial=Interior("Apartment plaster",new Color(.57f,.53f,.45f),0);}
                if(index==1){mission.FloorMaterial=Interior("Office carpet",new Color(.19f,.24f,.28f),2);mission.WallMaterial=Interior("Office painted wall",new Color(.48f,.53f,.55f),0);}
                if(index==2){mission.FloorMaterial=Interior("Hospital tile",new Color(.69f,.75f,.72f),1);mission.WallMaterial=Interior("Hospital washable wall",new Color(.70f,.78f,.74f),0);}
                mission.GenerateGeometry();
                var exterior=new GameObject("Surrounding district").transform;
                BuildingMission.Part("院落道路",exterior,new Vector3(0,-.35f,0),new Vector3(110,.25f,95),mission.FloorMaterial);
                for(int i=0;i<10;i++)
                {
                    float x=i<5?-39:39,z=-32+(i%5)*16,height=10+(i%3)*5;
                    BuildingMission.Part("相邻建筑",exterior,new Vector3(x,height*.5f,z),new Vector3(13,height,11),mission.WallMaterial);
                    for(int level=0;level<height/4;level++)for(int window=0;window<3;window++)
                        BuildingMission.Part("邻楼窗",exterior,new Vector3(x+(i<5?6.55f:-6.55f),2+level*4,z-3+window*3),new Vector3(.035f,1.5f,1.5f),mission.GlassMaterial,false);
                }
                var surface=root.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=LayerMask.GetMask("TZWorld","TZInteractable");surface.overrideVoxelSize=true;surface.voxelSize=.15f;
                surface.BuildNavMesh();
                string navPath=Root+"/Scenes/"+map.Scene+"_Navigation.asset";
                var prior=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
                if(prior){EditorUtility.CopySerialized(surface.navMeshData,prior);surface.RemoveData();surface.navMeshData=prior;surface.AddData();}
                else AssetDatabase.CreateAsset(surface.navMeshData,navPath);
                var player=(GameObject)PrefabUtility.InstantiatePrefab(catalog.PlayerPrefab);player.transform.position=map.Spawn+Vector3.up*.06f;
                
                var ui=new GameObject("Game UI");ui.AddComponent<GameUI>();new GameObject("Scene entry").AddComponent<SceneEntry>();
                var sun=new GameObject("Overcast sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(55,-25,0);sun.intensity=1.2f;sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.53f,.56f,.58f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.006f;RenderSettings.fogColor=new Color(.35f,.4f,.44f);
                var volume=new GameObject("Original Bodycam post processing").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Art/Bodycam15/sector07 Body camera.asset");volume.gameObject.AddComponent<BodyCameraStyle>();
                var style=root.AddComponent<SceneVisualStyle>();style.BodyCamera=true;style.Grading=volume;
                // Limited unshadowed fill lights retain readable interiors without a light per prop.
                for(int f=0;f<map.Floors.Length;f++)for(int i=0;i<3;i++)
                {var light=new GameObject("Interior fill").AddComponent<Light>();light.transform.SetParent(mission.FloorRoots[f]);light.transform.position=new Vector3(-14+i*14,f*5+2.8f,0);light.type=LightType.Point;light.range=18;light.intensity=2;light.shadows=LightShadows.None;light.color=index==2?new Color(.7f,.85f,.88f):new Color(1,.88f,.73f);}
                string path=Root+"/Scenes/"+map.Scene+".unity";EditorSceneManager.SaveScene(scene,path);
                if(!scenes.Exists(s=>s.path==path))scenes.Add(new EditorBuildSettingsScene(path,true));
                Debug.Log("BUILDING_SCENE_CREATED "+map.Id+" floors="+map.Floors.Length+" rooms="+mission.SpawnZones.Length);
            }
            EditorBuildSettings.scenes=scenes.ToArray();PlayerSettings.bundleVersion="0.18.0";AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root+"/Scenes/MainMenu.unity");
        }
        static Material Interior(string name,Color baseColor,int pattern)
        {
            string path=Root+"/Art/Building17/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,true){name=name,wrapMode=TextureWrapMode.Repeat};var colors=new Color[16384];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float grain=(Mathf.PerlinNoise(x*.36f,y*.36f)-.5f)*.035f;
                float detail=pattern==1&&(x%64<2||y%64<2)?-.14f:pattern==2?(x%2==y%2?.025f:-.025f):0;
                colors[x+y*128]=new Color(baseColor.r+grain+detail,baseColor.g+grain+detail,baseColor.b+grain+detail);
            }
            texture.SetPixels(colors);texture.Apply();AssetDatabase.CreateAsset(texture,Root+"/Art/Building17/"+name+".asset");
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};material.SetTexture("_BaseMap",texture);material.SetFloat("_Smoothness",pattern==1?.38f:pattern==2?.05f:.18f);material.mainTextureScale=pattern==1?new Vector2(16,12):pattern==2?new Vector2(24,16):new Vector2(4,4);AssetDatabase.CreateAsset(material,path);return material;
        }
        static Material Wood()
        {
            string path=Root+"/Art/Building17/Interior wood.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
            var grain=new Texture2D(128,128,TextureFormat.RGBA32,true){name="Wood grain",wrapMode=TextureWrapMode.Repeat};
            var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {float stripe=Mathf.Sin(y*.8f+Mathf.PerlinNoise(x*.02f,y*.05f)*6)*.08f+Mathf.PerlinNoise(x*.12f,y*.8f)*.1f;float seam=y%32==0?.5f:1;pixels[x+y*128]=new Color((.28f+stripe)*seam,(.18f+stripe*.7f)*seam,(.095f+stripe*.4f)*seam);}
            grain.SetPixels(pixels);grain.Apply();AssetDatabase.CreateAsset(grain,Root+"/Art/Building17/Wood grain.asset");
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Interior wood",enableInstancing=true};material.SetTexture("_BaseMap",grain);material.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(material,path);return material;
        }
        static Material Glass()
        {
            string path=Root+"/Art/Building17/Building glass.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Building glass",enableInstancing=true,renderQueue=3000};
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.SetFloat("_Smoothness",.75f);material.SetColor("_BaseColor",new Color(.29f,.46f,.49f,.22f));material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");AssetDatabase.CreateAsset(material,path);return material;
        }
        public static void RegenerateRelease18(){Setup();Release18();}
        public static void Release18(){BuildingCovers18.Render();Windows();}
        public static void Web18()
        {
            EnsureScenes();var scenes=new List<string>();
            foreach(var scene in EditorBuildSettings.scenes)if(scene.enabled)scenes.Add(scene.path);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
            PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.initialMemorySize=256;
            string path="Builds/BuildingSweep-0.18.0-WebGL";Directory.CreateDirectory(path);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes.ToArray(),locationPathName=path,target=BuildTarget.WebGL,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Unity WebGL build failed: "+report.summary.result);
            Debug.Log("BUILDING_WEBGL_READY "+Path.GetFullPath(path));
        }
        [MenuItem("Tactical Zero/Building Sweep/Build Windows")]
        public static void Windows()
        {
            EnsureScenes();
            var scenes=new List<string>();foreach(var scene in EditorBuildSettings.scenes)if(scene.enabled)scenes.Add(scene.path);
            string path="Builds/BuildingSweep-0.18.0/TacticalZero.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes.ToArray(),locationPathName=path,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Building Sweep Windows build: "+report.summary.result);
            Debug.Log("BUILDING_WINDOWS_READY "+Path.GetFullPath(path));
        }
    }
}
