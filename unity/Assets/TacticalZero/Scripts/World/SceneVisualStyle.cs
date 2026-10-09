using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TacticalZero
{
    public sealed class SceneVisualStyle : MonoBehaviour
    {
        public bool BodyCamera;
        public Volume Grading;public ScriptableRendererFeature ContactShadows;
        void OnEnable(){SaveService.Applied+=Apply;}
        void Start(){Apply();}
        void OnDisable(){SaveService.Applied-=Apply;if(ContactShadows)ContactShadows.SetActive(false);}
        void Apply()
        {
            if(!BodyCamera)
            {
            // Legacy scenes retain their authored ambient lighting.
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.55f,.58f,.61f);
            var ambient=new SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(.32f,.35f,.38f));
            var sky=RenderSettings.skybox;
            ambient.AddDirectionalLight(Vector3.up,sky&&sky.HasProperty("_Zenith")?Color.Lerp(sky.GetColor("_Zenith"),Color.white,.55f):Color.white,.35f);
            RenderSettings.ambientProbe=ambient;
            var reflections=Resources.Load<Cubemap>("Classic/Textures/FieldReflection14");
            if(reflections){RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=reflections;RenderSettings.reflectionIntensity=.85f;}
            }
            else
            {
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.53f,.56f,.58f);
                var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.26f,.29f,.32f));ambient.AddDirectionalLight(Vector3.up,new Color(.85f,.91f,1),.25f);RenderSettings.ambientProbe=ambient;
            }
            int quality=SaveService.Settings.quality;
            if(Grading)Grading.enabled=quality>0;
            if(ContactShadows)ContactShadows.SetActive(quality==2);
            var camera=Camera.main;if(!camera)return;
            camera.clearFlags=CameraClearFlags.Skybox;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=quality>0;
            data.antialiasing=quality==2?AntialiasingMode.SubpixelMorphologicalAntiAliasing:AntialiasingMode.None;
            data.antialiasingQuality=AntialiasingQuality.Medium;
            if(RenderSettings.sun)RenderSettings.sun.shadows=quality==0?LightShadows.None:quality==1?LightShadows.Hard:LightShadows.Soft;
        }
    }
}
