using UnityEngine;

namespace TacticalZero
{
    public sealed class NovaProjectile : MonoBehaviour
    {
        Vector3 direction; ClassicGun gun; GameObject owner; float remaining;
        public static void Launch(Vector3 origin,Vector3 direction,ClassicGun gun,GameObject owner)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="NOVA plasma grenade";
            Destroy(go.GetComponent<Collider>());go.transform.position=origin;go.transform.localScale=Vector3.one*.14f;
            go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Classic/Materials/GlowAmber");
            var p=go.AddComponent<NovaProjectile>();p.direction=direction;p.gun=gun;p.owner=owner;p.remaining=gun.range*3/64;
        }
        void Update()
        {
            if(!GameManager.Instance||!GameManager.Instance.IsPlaying)return;
            float step=Mathf.Min(remaining,gun.speed*3/64*Time.deltaTime);
            int mask=LayerMask.GetMask("TZWorld","TZEnemy","TZInteractable");
            bool blocked=Physics.SphereCast(transform.position,.07f,direction,out var hit,step,mask,QueryTriggerInteraction.Ignore);
            transform.position=blocked?hit.point-direction*.1f:transform.position+direction*step;remaining-=step;
            if(blocked||remaining<=0) Explode();
        }
        void Explode()
        {
            Vector3 at=transform.position;float radius=gun.blast*3/64;bool struck=false;
            foreach(var enemy in new System.Collections.Generic.List<EnemyHealth>(EnemyHealth.Active))
            {
                float d=Vector3.Distance(at,enemy.AimPoint);
                if(d>=radius||Physics.Linecast(at,enemy.AimPoint,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore))continue;
                enemy.Apply(new DamageInfo(gun.damage*Mathf.Max(.3f,1-d/radius*.7f),at,"玩家",gun.label));struck=true;
            }
            if(owner)
            {
                float d=Vector3.Distance(at,owner.transform.position+Vector3.up);
                if(d<radius&&!Physics.Linecast(at,owner.transform.position+Vector3.up,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore)) owner.GetComponent<PlayerHealth>().Apply(new DamageInfo(gun.damage*Mathf.Max(.3f,1-d/radius*.7f),at,"自身爆炸",gun.label));
            }
            if(struck)GameManager.Instance.Stats.hits++;
            for(int i=0;i<12;i++)EffectPool.Instance?.Burst(at+Random.insideUnitSphere*radius*.55f,new Color(1,.48f,.12f),.45f,.4f);
            AudioService.Instance?.Play("Shotgun",at,1,true);Destroy(gameObject);
        }
    }
}
