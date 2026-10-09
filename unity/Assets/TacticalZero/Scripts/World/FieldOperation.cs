using System.Collections.Generic;
using UnityEngine;
namespace TacticalZero
{
    public sealed class FieldOperation : MonoBehaviour
    {
        public const float RoundLimit=600, EarliestExtraction=300, CaptureSeconds=35, DecodeSeconds=45;
        ClassicEncounter mission;Vector3[] relays;GameObject[] markers;
        public int Stage {get;private set;}
        public float Capture {get;private set;}
        public float Decode {get;private set;}
        public bool Uploading=>Capture>=CaptureSeconds;
        public bool ReadyForExtraction=>Stage>=3&&mission.Elapsed>=EarliestExtraction;
        public Vector3 Target=>Stage<3?relays[Stage]:mission.Extraction;
        public string Status=>Stage>=3?(ReadyForExtraction?"通信目标完成 · 清场并撤离":"守住撤离区 · 运输机 "+Mathf.CeilToInt(EarliestExtraction-mission.Elapsed)+" 秒后抵达"):
            "通信站 "+(Stage+1)+" / 3 · "+(Uploading?"解密传输 "+Mathf.CeilToInt(DecodeSeconds-Decode)+"s · 可离站作战":"进入标记范围 · 接管 "+Mathf.CeilToInt(Capture)+" / "+CaptureSeconds+"s");
        public void Configure(ClassicEncounter encounter,IReadOnlyList<Vector3> floor,Vector3 spawn)
        {
            mission=encounter;relays=new Vector3[3];markers=new GameObject[3];var chosen=new List<Vector3>{spawn};
            for(int i=0;i<3;i++)
            {
                float best=-1;Vector3 selected=spawn;
                foreach(var p in floor){float nearest=1000;foreach(var prior in chosen)nearest=Mathf.Min(nearest,Vector3.Distance(prior,p));float score=nearest;if(score>best){best=score;selected=p;}}
                relays[i]=selected;chosen.Add(selected);
                var root=new GameObject("通信站 "+(i+1));root.transform.position=selected;markers[i]=root;
                var terminal=GameObject.CreatePrimitive(PrimitiveType.Cube);terminal.name="Relay terminal";terminal.transform.SetParent(root.transform,false);terminal.transform.localPosition=new Vector3(0,.8f,0);terminal.transform.localScale=new Vector3(.7f,1.6f,.55f);Destroy(terminal.GetComponent<Collider>());terminal.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Classic/Materials/Gunmetal");
                var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.transform.SetParent(root.transform,false);ring.transform.localPosition=Vector3.up*.03f;ring.transform.localScale=new Vector3(7,.025f,7);Destroy(ring.GetComponent<Collider>());ring.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Classic/Materials/GlowTeal");
                root.SetActive(i==0);
            }
        }
        void Update()
        {
            var game=GameManager.Instance;if(!mission||!game||!game.IsPlaying)return;
            if(mission.Elapsed>=RoundLimit){mission.Notice="行动超时 · 撤离窗口关闭";game.EndMatch(false);return;}
            if(Stage>=3)return;
            bool inside=Vector3.Distance(game.Player.transform.position,Target)<3.7f;
            bool contested=false;foreach(var enemy in EnemyHealth.Active)if(Vector3.Distance(enemy.transform.position,Target)<7){contested=true;break;}
            Advance(Time.deltaTime,inside,contested);
        }
        public void Advance(float dt,bool inside,bool contested)
        {
            if(Stage>=3||dt<=0)return;
            if(!Uploading){if(inside&&!contested)Capture=Mathf.Min(CaptureSeconds,Capture+dt);return;}
            Decode+=dt;
            if(Decode<DecodeSeconds)return;
            markers[Stage].SetActive(false);Stage++;Capture=Decode=0;
            mission.Notice=Stage<3?"通信链路已接通 · 转移至下一座通信站":"三座通信站已接通 · 敌方增援抵达撤离区";
            mission.SpawnReinforcements(Stage==3?3:2);
            if(Stage<3)markers[Stage].SetActive(true);
            AudioService.Instance?.Play("pickup",Target,.7f,false);
        }
        void OnDestroy(){if(markers!=null)foreach(var marker in markers)if(marker)Destroy(marker);}
    }
}
