using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TacticalZero.Editor
{
    public static class VisualPolish10
    {
        const string Root=ClassicArt.Root;
        public static void Apply()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/TacticalZero/Settings/TacticalURP.asset");
            pipeline.supportsHDR=true;pipeline.shadowCascadeCount=2;
            var pipelineData=new SerializedObject(pipeline);pipelineData.FindProperty("m_SoftShadowsSupported").boolValue=true;pipelineData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/TacticalZero/Settings/ForwardRenderer.asset");
            var rendererData=new SerializedObject(renderer);
            rendererData.FindProperty("postProcessData").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");rendererData.ApplyModifiedPropertiesWithoutUndo();
            ScreenSpaceAmbientOcclusion ao=null;foreach(var feature in renderer.rendererFeatures)if(feature is ScreenSpaceAmbientOcclusion found)ao=found;
            if(!ao){ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Contact shading";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);}
            var settings=new SerializedObject(ao);var options=settings.FindProperty("m_Settings");
            options.FindPropertyRelative("Downsample").boolValue=true;options.FindPropertyRelative("Intensity").floatValue=.7f;options.FindPropertyRelative("Radius").floatValue=.22f;
            options.FindPropertyRelative("DirectLightingStrength").floatValue=.12f;options.FindPropertyRelative("Source").intValue=0;options.FindPropertyRelative("Samples").intValue=2;options.FindPropertyRelative("BlurQuality").intValue=1;
            settings.ApplyModifiedPropertiesWithoutUndo();ao.SetActive(true);renderer.SetDirty();EditorUtility.SetDirty(renderer);
            foreach(var map in ClassicRules.Data.maps)
            {
                var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene("Assets/TacticalZero/Scenes/"+map.Scene+".unity",OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
                try
                {
                    bool warehouse=map.id=="sector07",harbor=map.id=="docks",ruins=map.id=="ruins";
                    var trim=Resources.Load<Material>("Classic/Materials/"+map.id+"_Trim");
                    if(trim){var tint=warehouse?new Color(.28f,.39f,.42f):harbor?new Color(.40f,.49f,.52f):ruins?new Color(.46f,.34f,.27f):new Color(.31f,.37f,.26f);trim.SetColor("_BaseColor",tint);trim.SetColor("_Color",tint);EditorUtility.SetDirty(trim);}
                    Color sky=warehouse?new Color(.11f,.23f,.34f):harbor?new Color(.15f,.36f,.52f):ruins?new Color(.28f,.32f,.37f):new Color(.12f,.32f,.49f);
                    Color horizon=warehouse?new Color(.49f,.60f,.65f):harbor?new Color(.78f,.69f,.54f):ruins?new Color(.66f,.53f,.39f):new Color(.58f,.72f,.74f);
                    Color sunlight=warehouse?new Color(1,.90f,.76f):harbor?new Color(1,.78f,.52f):ruins?new Color(1,.83f,.62f):new Color(1,.94f,.80f);
                    var sun=RenderSettings.sun;if(sun){sun.color=sunlight;sun.intensity=warehouse?1.5f:1.7f;sun.transform.rotation=Quaternion.Euler(harbor?24:ruins?34:46,harbor?-68:ruins?26:-38,0);sun.shadowBias=.03f;sun.shadowNormalBias=.18f;}
                    RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Color.Lerp(sky,Color.white,.42f);RenderSettings.ambientEquatorColor=Color.Lerp(sky,horizon,.42f)*.68f;RenderSettings.ambientGroundColor=new Color(.14f,.16f,.17f);
                    var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.27f,.30f,.33f));ambient.AddDirectionalLight(Vector3.up,Color.Lerp(sky,Color.white,.55f),.35f);RenderSettings.ambientProbe=ambient;
                    foreach(var root in scene.GetRootGameObjects())if(root.name=="Sky fill"&&root.TryGetComponent<Light>(out var fill)){fill.intensity=.65f;fill.color=new Color(.73f,.84f,1);}
                    RenderSettings.fogColor=Color.Lerp(sky,horizon,.66f);RenderSettings.fogStartDistance=warehouse?40:48;RenderSettings.fogEndDistance=ruins?110:135;
                    string skyPath=Root+"/Materials/"+map.id+"_Sky10.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
                    if(!material){material=new Material(Shader.Find("TacticalZero/Atmosphere Sky"));AssetDatabase.CreateAsset(material,skyPath);}
                    material.SetColor("_Zenith",sky);material.SetColor("_Horizon",horizon);material.SetColor("_Ground",RenderSettings.ambientGroundColor);material.SetColor("_SunColor",sunlight);material.SetVector("_SunDirection",sun?-sun.transform.forward:Vector3.up);material.SetFloat("_CloudAmount",ruins?.65f:.38f);EditorUtility.SetDirty(material);RenderSettings.skybox=material;
                    RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=Reflection(map.id,sky,horizon,sun?-sun.transform.forward:Vector3.up,sunlight);RenderSettings.reflectionIntensity=.65f;
                    string profilePath=Root+"/"+map.id+"_Grading10.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                    if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
                    var tone=Component<Tonemapping>(profile);tone.mode.Override(TonemappingMode.ACES);
                    var color=Component<ColorAdjustments>(profile);color.postExposure.Override(warehouse?.15f:.05f);color.contrast.Override(8);color.saturation.Override(ruins?-9:-4);
                    var bloom=Component<Bloom>(profile);bloom.threshold.Override(1.25f);bloom.intensity.Override(.13f);bloom.scatter.Override(.45f);
                    EditorUtility.SetDirty(profile);
                    SceneVisualStyle style=null;foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent<SceneVisualStyle>(out var found))style=found;
                    if(!style)style=new GameObject("Atmosphere and quality").AddComponent<SceneVisualStyle>();
                    var volume=style.GetComponent<Volume>();if(!volume)volume=style.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;volume.sharedProfile=profile;style.Grading=volume;style.ContactShadows=ao;
                    foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>()){camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
                    EditorSceneManager.SaveScene(scene);ClassicProjectSetup.RefreshMapPreview(map);
                }
                finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        static T Component<T>(VolumeProfile profile) where T:VolumeComponent
        {if(!profile.TryGet<T>(out var component)){component=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(component,profile);}EditorUtility.SetDirty(component);return component;}
        static Cubemap Reflection(string id,Color sky,Color horizon,Vector3 sun,Color light)
        {
            string path=Root+"/"+id+"_Reflection10.asset";var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if(!cube){cube=new Cubemap(32,TextureFormat.RGBAHalf,true);cube.name=id+" soft environment reflection";AssetDatabase.CreateAsset(cube,path);}
            var pixels=new Color[32*32];
            for(int face=0;face<6;face++)
            {
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    float u=(x+.5f)/16-1,v=(y+.5f)/16-1;
                    Vector3 d=(face==0?new Vector3(1,-v,-u):face==1?new Vector3(-1,-v,u):face==2?new Vector3(u,1,v):face==3?new Vector3(u,-1,-v):face==4?new Vector3(u,-v,1):new Vector3(-u,-v,-1)).normalized;
                    Color color=d.y>=0?Color.Lerp(horizon,sky,Mathf.Pow(d.y,.55f)):Color.Lerp(horizon,new Color(.2f,.22f,.24f),Mathf.Min(1,-d.y*3));
                    pixels[x+y*32]=color+light*(Mathf.Pow(Mathf.Max(0,Vector3.Dot(d,sun)),24)*.35f);
                }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply();EditorUtility.SetDirty(cube);return cube;
        }
    }
}
