using UnityEngine;
namespace TacticalZero
{
    public static class BuildingWebAudio18
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TZ18AudioPCM(string id,float[] samples,int count,int rate);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int TZ18AudioPlay(string id,float x,float y,float z,float level,int spatial,float gain,float cutoff);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TZ18AudioAdjust(int slot,float gain,float cutoff);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TZ18AudioListener(float x,float y,float z,float fx,float fy,float fz,float ux,float uy,float uz,float volume);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TZ18AudioClear();
        struct Voice {public Vector3 At;public float Until;public bool Spatial;}
        static readonly Voice[] voices=new Voice[16];static float next;
        public static void Register(string id,float[] samples,int rate)=>TZ18AudioPCM(id,samples,samples.Length,rate);
        public static void Play(AudioClip clip,Vector3 point,float volume,bool spatial)
        {
            var camera=Camera.main;var trace=spatial&&camera&&BuildingMission.Instance?BuildingAcoustics.Trace(camera.transform.position,point):new BuildingAcoustics.Transmission{Gain=1,Cutoff=22000};
            int slot=TZ18AudioPlay(clip.name,point.x,point.y,point.z,volume,spatial?1:0,trace.Gain,trace.Cutoff);
            if(slot>=0)voices[slot]=new Voice{At=point,Until=Time.unscaledTime+clip.length,Spatial=spatial};
        }
        public static void Update()
        {
            var camera=Camera.main;if(!camera)return;var t=camera.transform;var p=t.position;var f=t.forward;var u=t.up;
            TZ18AudioListener(p.x,p.y,p.z,f.x,f.y,f.z,u.x,u.y,u.z,SaveService.Settings.masterVolume*SaveService.Settings.sfxVolume);
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+.12f;
            for(int i=0;i<16;i++)if(voices[i].Until>Time.unscaledTime&&voices[i].Spatial){var trace=BuildingAcoustics.Trace(p,voices[i].At);TZ18AudioAdjust(i,trace.Gain,trace.Cutoff);}
        }
        public static void Clear(){TZ18AudioClear();System.Array.Clear(voices,0,voices.Length);}
#else
        public static void Register(string id,float[] samples,int rate){}
        public static void Update(){}
        public static void Clear(){}
#endif
    }
}
