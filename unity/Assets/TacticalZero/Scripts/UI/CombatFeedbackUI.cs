using UnityEngine;
using UnityEngine.UI;

namespace TacticalZero
{
    public sealed class CombatFeedbackUI : MonoBehaviour
    {
        RectTransform root,killPanel,arrow;Image[] edges;Text kill,damage,death;Image wash;Image[] ticks;
        public void Build(Transform parent)
        {
            root=UIFactory.Fill("Combat feedback",parent);
            var overlay=UIFactory.Fill("Damage wash",root);wash=UIFactory.Panel(overlay,Color.clear);
            edges=new Image[4];
            for(int i=0;i<4;i++)
            {
                var edge=UIFactory.Rect("Damage edge",root,i==0?new Vector2(.5f,1):i==1?new Vector2(1,.5f):i==2?new Vector2(.5f,0):new Vector2(0,.5f),Vector2.zero,i%2==0?new Vector2(4000,30):new Vector2(30,2400));
                edges[i]=UIFactory.Panel(edge,Color.clear);
            }
            arrow=UIFactory.Rect("Incoming direction",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(16,16));
            var notch=UIFactory.Rect("Direction tick",arrow,new Vector2(.5f,.5f),Vector2.up*110,new Vector2(44,7));UIFactory.Panel(notch,new Color(1,.24f,.17f));
            ticks=new Image[4];
            for(int i=0;i<4;i++)
            {
                var tick=UIFactory.Rect("Hit confirmation",root,new Vector2(.5f,.5f),Quaternion.Euler(0,0,45+i*90)*Vector3.right*16,new Vector2(11,3));tick.localRotation=Quaternion.Euler(0,0,45+i*90);ticks[i]=UIFactory.Panel(tick,Color.clear);
            }
            damage=UIFactory.Text("Impact number",root,"",new Vector2(.5f,.5f),new Vector2(0,43),new Vector2(220,35),24,TextAnchor.MiddleCenter);
            killPanel=UIFactory.Rect("Kill confirmation",root,new Vector2(.5f,.5f),new Vector2(0,-105),new Vector2(470,52));UIFactory.Panel(killPanel,new Color(.035f,.075f,.085f,.94f));
            kill=UIFactory.Text("Kill text",killPanel,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(450,50),18,TextAnchor.MiddleCenter);kill.color=new Color(1,.78f,.38f);
            death=UIFactory.Text("Death confirmation",root,"",new Vector2(.5f,1),new Vector2(0,-84),new Vector2(1000,130),32,TextAnchor.MiddleCenter);death.color=new Color(1,.45f,.36f);
        }
        void Update()
        {
            var feedback=CombatFeedback.Instance;var game=GameManager.Instance;if(!root||!feedback||!game)return;
            bool dead=game.Phase==GamePhase.Defeat,playing=game.IsPlaying;
            root.gameObject.SetActive(playing||dead);if(!playing&&!dead)return;
            float now=Time.unscaledTime,impact=Mathf.Clamp01(1-(now-feedback.HitAt)/.4f),hurt=Mathf.Clamp01(1-(now-feedback.HurtAt)/.8f);
            bool low=playing&&game.Player&&game.Player.Health<=30;
            var red=feedback.ShieldHit?new Color(.3f,.7f,1):new Color(.85f,.08f,.04f);
            float intensity=dead?.42f:Mathf.Max(hurt*.7f,low?.12f+.07f*Mathf.Sin(now*5):0);
            foreach(var edge in edges)edge.color=new Color(red.r,red.g,red.b,intensity);
            wash.color=new Color(red.r,red.g,red.b,dead?.08f:hurt*.09f);
            arrow.gameObject.SetActive(playing&&hurt>0);
            if(playing&&game.Player){var incoming=game.Player.transform.InverseTransformDirection(feedback.HurtOrigin-game.Player.transform.position);arrow.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(incoming.x,incoming.z)*Mathf.Rad2Deg);}
            var color=feedback.Headshot?new Color(1,.7f,.25f):Color.white;
            foreach(var tick in ticks)tick.color=new Color(color.r,color.g,color.b,playing?impact:0);
            damage.text=playing&&impact>0?(feedback.Headshot?"爆头 ":"")+Mathf.RoundToInt(feedback.Damage):"";damage.color=new Color(color.r,color.g,color.b,impact);
            bool killed=playing&&now-feedback.KillAt<2.7f;killPanel.gameObject.SetActive(killed);
            if(killed){float t=now-feedback.KillAt;killPanel.localScale=Vector3.one*(1+Mathf.Exp(-t*12)*.12f);kill.text=feedback.KillText+(feedback.Streak>1?"\n"+feedback.Streak+" 连杀":"");}
            death.text=dead&&!game.PauseMenuRequested?"你已阵亡\n<size=23>"+feedback.DeathText+"</size>":"";
        }
    }
}
