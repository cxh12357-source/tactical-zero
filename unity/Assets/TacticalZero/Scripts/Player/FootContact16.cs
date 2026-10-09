using UnityEngine;
using UnityEngine.AI;

namespace TacticalZero
{
    [DefaultExecutionOrder(160)]
    public sealed class FootContact16 : MonoBehaviour
    {
        sealed class Leg
        {
            public Transform upper,lower,foot;
            public Vector3 home,plant,from,normal=Vector3.up;
            public Quaternion footOffset;
            public float a,b,clearance;
            public bool grounded,swinging;
        }
        readonly Leg[] legs={new Leg(),new Leg()};
        OperatorRig rig;NavMeshAgent agent;CharacterController controller;
        float phase;Vector3 lastPosition;bool ready;
        public int PlantedFeet {get;private set;}
        public float MaximumPlantDrift {get;private set;}
        void Start()
        {
            rig=GetComponent<OperatorRig>();agent=GetComponentInParent<NavMeshAgent>();controller=GetComponentInParent<CharacterController>();lastPosition=transform.position;
            var all=GetComponentsInChildren<Transform>();
            for(int i=0;i<2;i++)
            {
                string s=i==0?".L":".R";var leg=legs[i];
                foreach(var t in all){if(t.name=="thigh"+s)leg.upper=t;if(t.name=="shin"+s)leg.lower=t;if(t.name=="foot"+s)leg.foot=t;}
                if(!leg.upper||!leg.lower||!leg.foot)return;
                leg.home=transform.InverseTransformPoint(leg.foot.position);leg.a=Vector3.Distance(leg.upper.position,leg.lower.position);leg.b=Vector3.Distance(leg.lower.position,leg.foot.position);
                leg.clearance=Mathf.Max(.02f,Vector3.Dot(leg.foot.position-transform.position,transform.up));
                leg.footOffset=Quaternion.Inverse(transform.rotation)*leg.foot.rotation;
            }
            ready=true;
        }
        void LateUpdate()
        {
            if(!ready||!rig||!rig.enabled||!rig.AnimationVisible)return;
            float dt=Mathf.Max(.0001f,Time.deltaTime);
            Vector3 velocity=agent&&agent.enabled?agent.velocity:controller?controller.velocity:(transform.position-lastPosition)/dt;
            velocity.y=0;float speed=velocity.magnitude;
            if((transform.position-lastPosition).sqrMagnitude>2)foreach(var l in legs)l.grounded=false;
            lastPosition=transform.position;
            if(rig.ReviewMotion){velocity=transform.forward*1.5f;speed=1.5f;}
            phase+=dt*Mathf.Clamp(speed/1.0f,0,3.3f);PlantedFeet=0;
            for(int i=0;i<2;i++)
            {
                var leg=legs[i];float cycle=Mathf.Repeat(phase+i*.5f,1);bool swing=speed>.12f&&cycle>.58f;
                Vector3 home=transform.TransformPoint(leg.home);
                if(!Ground(home,out var ground,out var normal)){leg.grounded=false;continue;}
                ground+=normal*leg.clearance;
                if(!leg.grounded){leg.plant=leg.from=ground;leg.grounded=true;leg.swinging=false;}
                if(swing&&!leg.swinging)leg.from=leg.plant;
                Vector3 target;
                if(swing)
                {
                    float t=Mathf.InverseLerp(.58f,1,cycle),smooth=t*t*(3-2*t);
                    Vector3 ahead=home+velocity*.18f;
                    if(Ground(ahead,out var future,out var futureNormal)){ground=future+futureNormal*leg.clearance;normal=futureNormal;}
                    target=Vector3.Lerp(leg.from,ground,smooth)+Vector3.up*Mathf.Sin(t*Mathf.PI)*(.075f+speed*.012f);leg.plant=ground;
                }
                else
                {
                    if(Vector3.ProjectOnPlane(leg.plant-home,Vector3.up).magnitude>.48f||speed<.12f&&Vector3.Distance(leg.plant,ground)>.19f)leg.plant=ground;
                    target=leg.plant;PlantedFeet++;
                }
                leg.swinging=swing;leg.normal=normal;
                Solve(leg.upper,leg.lower,leg.foot,target,transform.forward,leg.a,leg.b);
                leg.foot.rotation=Quaternion.FromToRotation(Vector3.up,normal)*transform.rotation*leg.footOffset;
                if(!swing)MaximumPlantDrift=Mathf.Max(MaximumPlantDrift,Vector3.Distance(leg.foot.position,target));
            }
        }
        static bool Ground(Vector3 p,out Vector3 point,out Vector3 normal)
        {
            if(BuildingMission.Instance){if(BuildingSupport18.Find(p,BuildingMission.FloorAt(p),out var support)){point=support.point;normal=support.normal;return true;}point=p;normal=Vector3.up;return false;}
            if(Physics.Raycast(p+Vector3.up*.65f,Vector3.down,out var hit,1.5f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore)){point=hit.point;normal=hit.normal;return normal.y>.5f;}
            point=p;normal=Vector3.up;return false;
        }
        internal static void Solve(Transform upper,Transform lower,Transform tip,Vector3 target,Vector3 hint,float a,float b)
        {
            Vector3 delta=target-upper.position;float distance=Mathf.Clamp(delta.magnitude,.04f,a+b-.001f);Vector3 forward=delta.normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);Vector3 bend=Vector3.ProjectOnPlane(hint,forward).normalized;
            Vector3 knee=upper.position+forward*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,knee-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(tip.position-lower.position,target-lower.position)*lower.rotation;
        }
    }
}
