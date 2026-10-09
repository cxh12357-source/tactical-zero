using UnityEngine;

namespace TacticalZero
{
    public sealed class CombatFeedback : MonoBehaviour
    {
        public static CombatFeedback Instance {get;private set;}
        public float HitAt {get;private set;}=-100;
        public float KillAt {get;private set;}=-100;
        public float HurtAt {get;private set;}=-100;
        public float DeathAt {get;private set;}=-100;
        public float Damage {get;private set;}
        public bool Headshot {get;private set;}
        public bool ShieldHit {get;private set;}
        public string KillText {get;private set;}="";
        public string DeathText {get;private set;}="";
        public Vector3 HurtOrigin {get;private set;}
        public int Streak {get;private set;}
        void Awake(){Instance=this;GameManager.Instance.Changed+=PhaseChanged;}
        void PhaseChanged(GamePhase phase)
        {
            if(phase==GamePhase.Loading){HitAt=KillAt=HurtAt=DeathAt=-100;Streak=0;KillText=DeathText="";}
            if(phase==GamePhase.Playing&&GameManager.Instance.Player&&GameManager.Instance.Player.Health>0)DeathAt=-100;
        }
        public void ConfirmHit(string victim,DamageInfo info,float actual,bool killed)
        {
            if(info.Attacker!="玩家")return;
            HitAt=Time.unscaledTime;Damage=actual;Headshot=info.Headshot;
            AudioService.Instance?.Play(killed?"confirm-kill":info.Headshot?"confirm-head":"confirm-hit",Vector3.zero,killed?.9f:.65f,false);
            if(!killed)return;
            Streak=Time.unscaledTime-KillAt<5?Streak+1:1;KillAt=Time.unscaledTime;
            KillText=(info.Headshot?"爆头击杀":"确认击杀")+"  /  "+victim+"  ·  "+info.Weapon;
        }
        public void ReceiveDamage(DamageInfo info,float healthDamage,bool dead)
        {
            HurtAt=Time.unscaledTime;HurtOrigin=info.Origin;ShieldHit=healthDamage<=0;
            if(!dead)return;
            DeathAt=Time.unscaledTime;
            DeathText=info.Attacker+"  ·  "+info.Weapon+(info.Headshot?"  ·  头部致命":"  ·  致命一击");
            Streak=0;
        }
        void OnDestroy(){if(GameManager.Instance)GameManager.Instance.Changed-=PhaseChanged;if(Instance==this)Instance=null;}
    }
}
