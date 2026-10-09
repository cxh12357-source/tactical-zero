using UnityEngine;

namespace TacticalZero
{
    public sealed class WeaponReloadAnimation : MonoBehaviour
    {
        public Transform Magazine,LeftHand,RightHand,Bolt,Cover,Belt,Shell;
        public string WeaponId;
        public bool ContactGrip;
        public Vector3 MagazineGrip,BoltGrip,BeltGrip,CoverGrip,ShellGrip;
        public float GripRelease {get;private set;}
        Vector3 magazineRest,leftRest,rightRest,boltRest,beltRest,shellRest;Quaternion coverRest,leftRotation,rightRotation,magazineRotation;bool initialized;
        float shotAt=-100,priorProgress=-1;WeaponController owner;
        void Awake(){Initialize();owner=GetComponentInParent<WeaponController>();}
        void Initialize()
        {
            if(initialized)return;initialized=true;
            if(Magazine)magazineRest=Magazine.localPosition;if(LeftHand)leftRest=LeftHand.localPosition;if(RightHand)rightRest=RightHand.localPosition;
            if(Bolt)boltRest=Bolt.localPosition;if(Cover)coverRest=Cover.localRotation;if(Belt)beltRest=Belt.localPosition;if(Shell)shellRest=Shell.localPosition;
            if(LeftHand)leftRotation=LeftHand.localRotation;if(RightHand)rightRotation=RightHand.localRotation;if(Magazine)magazineRotation=Magazine.localRotation;
        }
        public void Shot(){shotAt=Time.time;}
        void LateUpdate()
        {
            if(!owner)owner=GetComponentInParent<WeaponController>();
            if(!owner||!owner.Current||owner.Ammo==null)return;
            float progress=owner.Ammo.ReloadRemaining>0?1-owner.Ammo.ReloadRemaining/owner.Current.ReloadSeconds:-1;
            Pose(progress);
            if(progress>=0)
            {
                if(WeaponId=="M1014"||WeaponId=="Shotgun")
                {
                    int count=Mathf.Clamp(Mathf.Min(owner.Ammo.Capacity-owner.Ammo.Loaded,owner.Ammo.Reserve),1,8);
                    for(int i=0;i<count;i++)Cue(.12f+.76f*(i+.65f)/count,"field-shell-insert",progress);
                }
                else{Cue(.15f,"field-mag-release",progress);Cue(.68f,"field-mag-insert",progress);Cue(.87f,"field-bolt-cycle",progress);}
            }
            priorProgress=progress;
            if(owner.Ammo.ReloadRemaining<=0&&Bolt)
            {
                float age=Time.time-shotAt;float length=WeaponId=="M700"||WeaponId=="AWP"?.95f:.12f;
                float pull=age>=0&&age<length?Mathf.Sin(age/length*Mathf.PI):0;
                Bolt.localPosition=boltRest+Vector3.back*pull*(length>.5f?.095f:.04f);
                if(length>.5f&&RightHand)RightHand.localPosition=rightRest+new Vector3(.045f,.06f,-.08f)*pull;
            }
        }
        public void Pose(float progress)
        {
            Initialize();float p=Mathf.Clamp01(progress);bool active=progress>=0&&progress<1;
            GripRelease=0;
            if(Magazine)Magazine.localPosition=magazineRest;if(LeftHand)LeftHand.localPosition=leftRest;if(RightHand)RightHand.localPosition=rightRest;
            if(Bolt)Bolt.localPosition=boltRest;if(Cover)Cover.localRotation=coverRest;if(Belt)Belt.localPosition=beltRest;
            if(LeftHand)LeftHand.localRotation=leftRotation;if(RightHand)RightHand.localRotation=rightRotation;if(Magazine)Magazine.localRotation=magazineRotation;
            if(Shell){Shell.gameObject.SetActive(active);Shell.localPosition=shellRest;}
            if(!active)return;
            float reach=Ramp(p,0,.16f)*(1-Ramp(p,.88f,1));
            float remove=Ramp(p,.16f,.34f)*(1-Ramp(p,.48f,.70f));
            float chamber=Ramp(p,.76f,.84f)*(1-Ramp(p,.89f,.96f));
            Vector3 shift=WeaponId=="P90"?new Vector3(-.10f,.22f,-.06f):new Vector3(-.06f,-.30f,-.055f);
            if(Magazine&&WeaponId!="MG3")Magazine.localPosition=magazineRest+shift*remove;
            Vector3 grip=WeaponId=="P90"?new Vector3(-.035f,.145f,-.035f):WeaponId=="MG3"?new Vector3(-.11f,-.01f,-.03f):new Vector3(-.035f,-.10f,.055f);
            if(LeftHand){LeftHand.localPosition=Vector3.Lerp(leftRest,grip,reach)+shift*remove+new Vector3(.075f,.16f,-.06f)*chamber;LeftHand.localRotation=leftRotation*Quaternion.Euler(-15*reach,0,WeaponId=="P90"?-55*reach:18*reach);}
            if(Magazine&&WeaponId!="MG3")Magazine.localRotation=magazineRotation*Quaternion.Euler(-12*remove,4*remove,-7*remove);
            if(Bolt)Bolt.localPosition=boltRest+Vector3.back*.075f*chamber;
            if(WeaponId=="MG3")
            {
                float open=Ramp(p,.08f,.25f)*(1-Ramp(p,.73f,.85f));
                if(Cover)Cover.localRotation=coverRest*Quaternion.Euler(78*open,0,0);
                if(Belt)Belt.localPosition=beltRest+new Vector3(-.17f,.09f,0)*remove;
                if(LeftHand)LeftHand.localPosition+=new Vector3(-.10f,.15f,-.06f)*open;
            }
            if(WeaponId=="M1014"||WeaponId=="Shotgun")
            {
                int count=owner&&owner.Ammo!=null?Mathf.Clamp(Mathf.Min(owner.Ammo.Capacity-owner.Ammo.Loaded,owner.Ammo.Reserve),1,8):6;
                float cycle=Mathf.Repeat(Mathf.InverseLerp(.12f,.88f,p)*count,1),insert=Mathf.Sin(cycle*Mathf.PI);
                float blend=Ramp(p,0,.12f)*(1-Ramp(p,.90f,1));
                if(LeftHand){LeftHand.localPosition=Vector3.Lerp(leftRest,new Vector3(.035f,-.19f,.045f)+new Vector3(0,.12f,.025f)*insert,blend);LeftHand.localRotation=leftRotation*Quaternion.Euler(0,0,-65*blend);}
                if(Shell){Shell.gameObject.SetActive(p>.12f&&p<.88f&&cycle<.7f);Shell.localPosition=shellRest+new Vector3(0,.12f,.025f)*insert;}
            }
            if(ContactGrip&&LeftHand)
            {
                float release=Ramp(p,.015f,.08f)*(1-Ramp(p,.12f,.19f));
                GripRelease=release*.7f+Ramp(p,.72f,.78f)*(1-Ramp(p,.86f,.96f))*.5f;
                if(Magazine&&WeaponId!="MG3"&&WeaponId!="Shotgun"&&WeaponId!="M1014")
                {
                    float contact=Ramp(p,.035f,.16f)*(1-Ramp(p,.71f,.79f));
                    Vector3 wrist=MagazineGrip+shift*remove;
                    LeftHand.localPosition=Vector3.Lerp(leftRest,wrist,contact);
                    LeftHand.localRotation=Quaternion.Slerp(leftRotation,Quaternion.Euler(WeaponId=="P90"?0:-90,0,WeaponId=="P90"?0:12),contact);
                    LeftHand.localPosition=Vector3.Lerp(LeftHand.localPosition,BoltGrip+Vector3.back*.075f*chamber,chamber);
                }
                if(WeaponId=="MG3")
                {
                    float coverContact=Ramp(p,.015f,.08f)*(1-Ramp(p,.23f,.30f))+Ramp(p,.68f,.74f)*(1-Ramp(p,.86f,.94f));
                    float beltContact=Ramp(p,.25f,.34f)*(1-Ramp(p,.64f,.72f));
                    Vector3 coverWrist=Cover?transform.InverseTransformPoint(Cover.TransformPoint(CoverGrip)):leftRest;
                    LeftHand.localPosition=Vector3.Lerp(leftRest,coverWrist,coverContact);
                    LeftHand.localPosition=Vector3.Lerp(LeftHand.localPosition,BeltGrip+new Vector3(-.17f,.09f,0)*remove,beltContact);
                    LeftHand.localRotation=leftRotation;
                    GripRelease=(1-Mathf.Max(coverContact,beltContact))*reach*.7f;
                }
                if((WeaponId=="M1014"||WeaponId=="Shotgun")&&Shell)
                {
                    float contact=Ramp(p,.015f,.12f)*(1-Ramp(p,.90f,1));
                    LeftHand.localPosition=Vector3.Lerp(leftRest,ShellGrip+Shell.localPosition-shellRest,contact);
                    LeftHand.localRotation=leftRotation;
                }
            }
        }
        static float Ramp(float x,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        void Cue(float at,string sound,float progress){if(priorProgress<at&&progress>=at)AudioService.Instance?.Play(sound,transform.position,.6f,false);}
        void OnDisable(){Pose(-1);priorProgress=-1;}
    }
}
