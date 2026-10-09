using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TacticalZero
{
    public sealed class AudioService:MonoBehaviour
    {
        public static AudioService Instance {get;private set;}
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly List<AudioClip> generated=new List<AudioClip>();
        readonly AudioSource[] voices=new AudioSource[12],confirmations=new AudioSource[3];
        readonly AudioLowPassFilter[] filters=new AudioLowPassFilter[12];
        readonly float[] levels=new float[12],gains=new float[12],cutoffs=new float[12];
        readonly float[] radii=new float[12];
        readonly string[] materials={"wood","tile","concrete","metal","carpet"};
        AudioSource ambient;float nextTrace;int stepVariant,scene=-2;Coroutine test;
        public int ActiveVoices {get {int n=0;foreach(var v in voices)if(v&&v.isPlaying)n++;return n;}}
        void Awake()
        {
            Instance=this;
            foreach(var clip in Resources.LoadAll<AudioClip>("Audio"))clips[clip.name]=clip;
            MakeTransient("building-door",.24f,105,3);MakeTransient("building-impact",.08f,470,1);
            for(int m=0;m<materials.Length;m++)for(int v=0;v<3;v++)MakeTransient("step-"+materials[m]+v,.16f+m*.015f,70+m*57+v*11,m);
            for(int i=0;i<voices.Length;i++)
            {
                var o=new GameObject("Pooled spatial voice "+i);o.transform.SetParent(transform,false);
                var v=voices[i]=o.AddComponent<AudioSource>();v.playOnAwake=false;v.dopplerLevel=0;v.spatialBlend=1;v.rolloffMode=AudioRolloffMode.Linear;v.minDistance=1.2f;v.maxDistance=60;
                filters[i]=o.AddComponent<AudioLowPassFilter>();filters[i].cutoffFrequency=22000;
            }
            for(int i=0;i<3;i++){var o=new GameObject("Combat confirmation "+i);o.transform.SetParent(transform,false);confirmations[i]=o.AddComponent<AudioSource>();confirmations[i].playOnAwake=false;}
            var room=new GameObject("Quiet building ambience");room.transform.SetParent(transform,false);ambient=room.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;
            const int rate=22050;var data=new float[rate*4];uint rng=18018;float smooth=0;
            for(int i=0;i<data.Length;i++){rng=rng*1664525+1013904223;smooth=Mathf.Lerp(smooth,((rng&65535)/32768f-1),.015f);data[i]=smooth*.12f+Mathf.Sin(i*2*Mathf.PI*55/rate)*.005f;}
            var hum=AudioClip.Create("Original ventilation ambience",data.Length,1,rate,false);hum.SetData(data,0);generated.Add(hum);ambient.clip=hum;
        }
        void MakeTransient(string id,float seconds,float pitch,int material)
        {
            const int rate=22050;var data=new float[Mathf.RoundToInt(rate*seconds)];uint rng=(uint)(18018+material*113+pitch);float low=0,previous=0;
            for(int i=0;i<data.Length;i++)
            {
                rng=rng*1664525+1013904223;float noise=(rng&65535)/32768f-1;low=Mathf.Lerp(low,noise,material==4?.05f:.16f);
                float t=(float)i/rate,envelope=Mathf.Exp(-t*(material==3?23:38)),attack=Mathf.Clamp01(t/.003f);
                float crunch=(noise-previous)*Mathf.Exp(-t*70)*.1f;previous=noise;
                float resonance=Mathf.Sin(t*pitch*6.28318f)*.08f+Mathf.Sin(t*pitch*1.43f*6.28318f)*.035f;
                data[i]=attack*(low*.7f+crunch+resonance)*envelope;
            }
            var clip=AudioClip.Create(id,data.Length,1,rate,false);clip.SetData(data,0);clips[id]=clip;generated.Add(clip);BuildingWebAudio18.Register(id,data,rate);
        }
        public void Step(Vector3 point,float volume)
        {
            string material=BuildingMission.Instance?BuildingSupport18.MaterialAt(point):"concrete";
            if(System.Array.IndexOf(materials,material)<0)material="concrete";
            Play("step-"+material+(stepVariant++%3),point+Vector3.up*.12f,volume,true);
        }
        public void Play(string id,Vector3 position,float volume=1,bool spatial=true)
        {
            if(id=="step"){Step(position,volume);return;}
            if(!clips.TryGetValue(id,out var clip))return;
            #if UNITY_WEBGL && !UNITY_EDITOR
            PlayClip(clip,position,volume,spatial);return;
#else
            if(id.StartsWith("confirm-")){var v=confirmations[id=="confirm-kill"?2:id=="confirm-head"?1:0];v.clip=clip;v.volume=volume*SaveService.Settings.sfxVolume;v.Play();return;}
            PlayClip(clip,position,volume,spatial);
#endif
        }
        public void PlayClip(AudioClip clip,Vector3 position,float volume=1,bool spatial=true)
        {
            if(!clip)return;
#if UNITY_WEBGL && !UNITY_EDITOR
            BuildingWebAudio18.Play(clip,position,volume,spatial);return;
#else
            int slot=-1;float lowest=float.MaxValue;var camera=Camera.main;
            float priority=volume/(1+(camera?(camera.transform.position-position).sqrMagnitude:0));
            for(int i=0;i<voices.Length;i++)
            {
                if(!voices[i].isPlaying){slot=i;break;}
                float score=levels[i]/(1+(camera?(camera.transform.position-voices[i].transform.position).sqrMagnitude:0));
                if(score<lowest){lowest=score;slot=i;}
            }
            if(voices[slot].isPlaying&&priority<lowest)return;
            var voice=voices[slot];voice.Stop();voice.transform.position=position;voice.clip=clip;voice.spatialBlend=spatial?1:0;voice.panStereo=0;voice.pitch=1;
            levels[slot]=volume;gains[slot]=1;cutoffs[slot]=22000;
            if(spatial&&BuildingMission.Instance&&camera){var trace=BuildingAcoustics.Trace(camera.transform.position,position);gains[slot]=trace.Gain;cutoffs[slot]=trace.Cutoff;}
            filters[slot].cutoffFrequency=cutoffs[slot];voice.volume=volume*gains[slot]*SaveService.Settings.sfxVolume;voice.Play();
#endif
        }
        void Update()
        {
            BuildingWebAudio18.Update();
            var camera=Camera.main;bool trace=Time.unscaledTime>=nextTrace;if(trace)nextTrace=Time.unscaledTime+.12f;
            for(int i=0;i<voices.Length;i++)if(voices[i].isPlaying)
            {
                if(trace&&voices[i].spatialBlend>0&&camera&&BuildingMission.Instance){var t=BuildingAcoustics.Trace(camera.transform.position,voices[i].transform.position);gains[i]=t.Gain;cutoffs[i]=t.Cutoff;}
                float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*25);
                voices[i].volume=Mathf.Lerp(voices[i].volume,levels[i]*gains[i]*SaveService.Settings.sfxVolume,blend);
                filters[i].cutoffFrequency=Mathf.Lerp(filters[i].cutoffFrequency,cutoffs[i],blend);
            }
            int current=BuildingMission.Instance?BuildingMission.Instance.MapIndex:-1;
            if(current!=scene){scene=current;BuildingAcoustics.Clear();BuildingWebAudio18.Clear();foreach(var v in voices)v.Stop();if(current>=0){ambient.pitch=.8f+current*.09f;ambient.Play();}else ambient.Stop();}
            ambient.volume=SaveService.Settings.ambientVolume*.22f;
        }
        public void TestHeadphones(Text label)
        {
            SaveService.Apply();if(test!=null)StopCoroutine(test);test=StartCoroutine(Test(label));
        }
        IEnumerator Test(Text label)
        {
            var view=Camera.main;if(!view)yield break;
            Vector3[] offsets={Vector3.left,Vector3.right,Vector3.forward,Vector3.back,Vector3.up,Vector3.down};
            string[] names={"左侧","右侧","前方","后方","上一层高度","下一层高度"};
            for(int i=0;i<offsets.Length;i++)
            {
                if(!label||!label.gameObject.activeInHierarchy)break;
                label.text=names[i];Play("step-metal1",view.transform.TransformPoint(offsets[i]*5),1,true);yield return new WaitForSecondsRealtime(1.2f);
            }
            if(label)label.text="耳机声场测试";test=null;
        }
        void OnDestroy(){foreach(var clip in generated)if(clip)Destroy(clip);if(Instance==this)Instance=null;}
    }
}
