using UnityEngine;

namespace TacticalZero
{
    // Bind after the detailed character has been installed, not to the hidden prototype.
    public sealed class EnemyCombatPresentation : MonoBehaviour
    {
        EnemyHealth health;Renderer[] surfaces;Color[] colors;MaterialPropertyBlock block;
        Vector3 rest;float side=1;Quaternion fall;bool ragdoll;
        public float ImpactIntensity=>health?Mathf.Clamp01(1-(Time.time-health.HurtAt)/.23f):0;
        void Start()
        {
            health=GetComponentInParent<EnemyHealth>();surfaces=GetComponentsInChildren<Renderer>();colors=new Color[surfaces.Length];
            for(int i=0;i<colors.Length;i++)colors[i]=surfaces[i].sharedMaterial.color;
            block=new MaterialPropertyBlock();rest=transform.localPosition;health.Damaged+=OnDamage;
        }
        void OnDamage(DamageInfo info)
        {
            side=Vector3.Dot(transform.right,transform.position-info.Origin)>0?1:-1;
            if(health.Alive)return;
            var rig=GetComponent<OperatorRig>();if(rig)rig.enabled=false;
            if(rig&&rig.refined){gameObject.AddComponent<OperatorRagdoll>().Fall(info.Origin);ragdoll=true;}
            fall=Quaternion.Euler(12,side*15,side*88);
        }
        void Update()
        {
            if(!health||!GameManager.Instance||!GameManager.Instance.IsPlaying)return;
            float hit=ImpactIntensity;
            for(int i=0;i<surfaces.Length;i++)
            {
                if(!surfaces[i])continue;
                block.Clear();block.SetColor("_BaseColor",Color.Lerp(colors[i],new Color(.34f,.16f,.13f),hit*.16f));
                surfaces[i].SetPropertyBlock(block);
            }
            if(health.Alive)
            {
                transform.localPosition=rest+Vector3.back*(hit*.055f);
                transform.localRotation=Quaternion.Euler(-hit*7,0,side*hit*5);
            }
            else if(!ragdoll)
            {
                transform.localRotation=Quaternion.Slerp(transform.localRotation,fall,Time.deltaTime*9);
                transform.localPosition=Vector3.Lerp(transform.localPosition,rest+Vector3.up*.2f,Time.deltaTime*8);
            }
        }
        void OnDestroy(){if(health)health.Damaged-=OnDamage;}
    }
}
