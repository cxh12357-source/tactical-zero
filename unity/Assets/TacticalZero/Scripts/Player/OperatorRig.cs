using UnityEngine;
using UnityEngine.AI;

namespace TacticalZero
{
    public sealed class OperatorRig : MonoBehaviour
    {
        public bool refined;
        readonly float[] angles=new float[9];
        Transform[] joints;Quaternion[] rest;Vector3[] axes;SkinnedMeshRenderer[] skins;CharacterController controller;
        Transform left,right,leftArm,rightArm;NavMeshAgent agent;
        Transform handL,handR,head,pelvis;Quaternion headRest,pelvisRest;WeaponReloadAnimation gun;
        float gaitPhase;Vector3 pelvisPosition;int[][] blinkIndices;
        EnemyBrain brain;BuildingEnemy sweepBrain;EnemyCombat combat;float readiness;
        void Start()
        {
            left=transform.Find("LeftLeg");right=transform.Find("RightLeg");leftArm=transform.Find("LeftArm");rightArm=transform.Find("RightArm");agent=GetComponentInParent<NavMeshAgent>();controller=GetComponentInParent<CharacterController>();
            if(!refined)return;
            string[] names={"thigh.L","thigh.R","shin.L","shin.R","upper_arm.L","upper_arm.R","forearm.L","forearm.R","chest"};
            joints=new Transform[names.Length];rest=new Quaternion[names.Length];axes=new Vector3[names.Length];
            var all=GetComponentsInChildren<Transform>();
            for(int i=0;i<names.Length;i++)foreach(var t in all)if(t.name==names[i]){joints[i]=t;rest[i]=t.localRotation;axes[i]=t.parent.InverseTransformDirection(transform.right);break;}
            skins=GetComponentsInChildren<SkinnedMeshRenderer>();blinkIndices=new int[skins.Length][];
            for(int k=0;k<skins.Length;k++){var indices=new System.Collections.Generic.List<int>();for(int i=0;i<skins[k].sharedMesh.blendShapeCount;i++)if(skins[k].sharedMesh.GetBlendShapeName(i).Contains("Blink."))indices.Add(i);blinkIndices[k]=indices.ToArray();}
            foreach(var t in all){if(t.name=="hand.L")handL=t;if(t.name=="hand.R")handR=t;if(t.name=="head")head=t;if(t.name=="pelvis")pelvis=t;}
            if(head)headRest=head.localRotation;if(pelvis){pelvisRest=pelvis.localRotation;pelvisPosition=pelvis.localPosition;}
            gun=GetComponentInChildren<WeaponReloadAnimation>();
            brain=GetComponentInParent<EnemyBrain>();sweepBrain=GetComponentInParent<BuildingEnemy>();combat=GetComponentInParent<EnemyCombat>();
        }
        public bool AnimationVisible {get;private set;}=true;
        void Update()
        {
            AnimationVisible=true;
            if(BuildingPerformance18.Optimized&&refined&&agent&&!ReviewMotion&&skins!=null)
            {
                bool visible=false;foreach(var skin in skins)if(skin&&skin.isVisible){visible=true;break;}
                var player=GameManager.Instance?GameManager.Instance.Player:null;
                if(!visible&&(!player||(player.transform.position-transform.position).sqrMagnitude>64)){AnimationVisible=false;return;}
            }
            float pace=agent?Mathf.Min(1,agent.velocity.magnitude/2):0;
            if(refined)
            {
                if(!agent&&controller)pace=Mathf.Clamp01(new Vector2(controller.velocity.x,controller.velocity.z).magnitude/2);
                if(ReviewMotion)pace=1;
                float speed=ReviewMotion?2:agent?agent.velocity.magnitude:controller?new Vector2(controller.velocity.x,controller.velocity.z).magnitude:0;
                gaitPhase+=Time.deltaTime*speed*4.2f;
                float phase=gaitPhase,step=Mathf.Sin(phase)*pace;
                float direction=agent&&Vector3.Dot(agent.velocity,transform.forward)<-.1f?-1:1;
                angles[0]=step*27*direction;angles[1]=-step*27*direction;angles[2]=Mathf.Max(0,-Mathf.Sin(phase))*pace*43;angles[3]=Mathf.Max(0,Mathf.Sin(phase))*pace*43;angles[4]=-step*10;angles[5]=step*10;angles[6]=angles[7]=-9-Mathf.Abs(step)*8;angles[8]=Mathf.Sin(Time.time*1.8f)*.65f+pace*3;
                for(int i=0;i<joints.Length;i++)if(joints[i])joints[i].localRotation=Quaternion.AngleAxis(angles[i],axes[i])*rest[i];
                if(pelvis){pelvis.localPosition=pelvisPosition+Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.012f*pace);pelvis.localRotation=Quaternion.AngleAxis(step*2,Vector3.forward)*pelvisRest;}
                if(head)head.localRotation=Quaternion.AngleAxis(-step*.7f,Vector3.forward)*headRest;
                if(agent&&gun)
                {
                    var player=GameManager.Instance?GameManager.Instance.Player:null;
                    Vector3 aimAt=sweepBrain?sweepBrain.LastKnown:player?player.transform.position:transform.position+transform.forward*5;
                    float pitch=Mathf.Clamp(-Mathf.Atan2(aimAt.y+1.4f-gun.transform.position.y,Vector3.Distance(aimAt,transform.position))*Mathf.Rad2Deg,-22,25);
                    bool alert=sweepBrain?sweepBrain.State!=SweepEnemyState.IDLE&&sweepBrain.State!=SweepEnemyState.PATROL&&sweepBrain.State!=SweepEnemyState.DEAD:!brain||brain.Machine.State==EnemyState.Combat||brain.Machine.State==EnemyState.Search||brain.Machine.State==EnemyState.Investigate;
                    readiness=Mathf.MoveTowards(readiness,alert?1:0,Time.deltaTime*3.5f);
                    float recoil=combat?Mathf.Clamp01(1-(Time.time-combat.ShotAt)/.17f):0;
                    Vector3 lateral=transform.InverseTransformDirection(agent.velocity);
                    gun.transform.localPosition=new Vector3(.07f,1.39f+readiness*.10f+Mathf.Sin(Time.time*1.8f)*.003f,.22f-recoil*.026f);
                    gun.transform.localRotation=Quaternion.Euler(Mathf.Lerp(24,pitch,readiness)-recoil*3.5f,Mathf.Clamp(lateral.x*-1.3f,-3,3),step*.3f);
                    Arm(joints[4],joints[6],handL,gun.LeftHand?gun.LeftHand.position:gun.transform.TransformPoint(new Vector3(-.024f,-.065f,.20f)),transform.TransformDirection(new Vector3(-1,-1,0)));
                    Arm(joints[5],joints[7],handR,gun.RightHand?gun.RightHand.position:gun.transform.TransformPoint(new Vector3(.034f,-.09f,-.075f)),transform.TransformDirection(new Vector3(1,-1,0)));
                }
                float blink=Mathf.Clamp01(1-Mathf.Abs(Mathf.Repeat(Time.unscaledTime+transform.position.x*.17f+transform.position.z*.23f,4.3f)-.13f)/.13f)*100;
                if(BuildingPerformance18.Optimized){for(int k=0;k<skins.Length;k++)foreach(int i in blinkIndices[k])skins[k].SetBlendShapeWeight(i,blink);}
                else foreach(var skin in skins)for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)if(skin.sharedMesh.GetBlendShapeName(i).Contains("Blink."))skin.SetBlendShapeWeight(i,blink);
                return;
            }
            float gait=Mathf.Sin(Time.time*9)*pace*23;
            if(left)left.localRotation=Quaternion.Euler(gait,0,0);if(right)right.localRotation=Quaternion.Euler(-gait,0,0);
            if(leftArm)leftArm.localRotation=Quaternion.Euler(-gait*.16f,0,0);if(rightArm)rightArm.localRotation=Quaternion.Euler(gait*.16f,0,0);
        }
        public bool ReviewMotion {get;set;}
        static void Arm(Transform upper,Transform lower,Transform wrist,Vector3 target,Vector3 hint)
        {
            if(!upper||!lower||!wrist)return;
            float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,wrist.position);
            var delta=target-upper.position;float distance=Mathf.Clamp(delta.magnitude,.04f,a+b-.005f);var direction=delta.normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);var elbow=upper.position+direction*along+Vector3.ProjectOnPlane(hint,direction).normalized*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(wrist.position-lower.position,target-lower.position)*lower.rotation;
        }
    }
}

