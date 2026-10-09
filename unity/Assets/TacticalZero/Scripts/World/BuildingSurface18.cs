using UnityEngine;
using UnityEngine.AI;
namespace TacticalZero
{
    public enum SurfaceUse18 { Floor,Stair,Ceiling,Roof,Prop }
    public sealed class BuildingSurface18:MonoBehaviour
    {
        public int FloorId;public string SurfaceId,FootMaterial="concrete";public SurfaceUse18 Use;
        public bool Supports(int floor)=>Use==SurfaceUse18.Floor&&floor==FloorId||Use==SurfaceUse18.Stair&&(floor==FloorId||floor==FloorId+1);
    }
    public static class BuildingSupport18
    {
        static readonly RaycastHit[] hits=new RaycastHit[16];static readonly Collider[] overlaps=new Collider[16];
        public static bool Find(Vector3 desired,int floor,out RaycastHit support)
        {
            support=default;float closest=float.MaxValue;int n=Physics.RaycastNonAlloc(desired+Vector3.up*.65f,Vector3.down,hits,1.5f,LayerMask.GetMask("TZWorld"),QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)
            {
                var s=hits[i].collider.GetComponent<BuildingSurface18>();float dy=Mathf.Abs(hits[i].point.y-desired.y);
                if(s&&s.Supports(floor)&&hits[i].normal.y>.65f&&dy<closest){support=hits[i];closest=dy;}
            }
            return closest<float.MaxValue;
        }
        public static bool Validate(Vector3 desired,int floor,float radius,float height,out Vector3 at,out string surface)
        {
            at=desired;surface="no legal support";
            if(!NavMesh.SamplePosition(desired,out var nav,.7f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-desired.y)>.6f||!Find(nav.position,floor,out var ground))return false;
            at=ground.point;surface=ground.collider.GetComponent<BuildingSurface18>().SurfaceId;
            int n=Physics.OverlapCapsuleNonAlloc(at+Vector3.up*(radius+.08f),at+Vector3.up*(height-radius),radius,overlaps,LayerMask.GetMask("TZWorld","TZInteractable"),QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(overlaps[i]!=ground.collider)return false;
            foreach(var offset in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})if(!Find(at+offset*radius,floor,out _))return false;
            return true;
        }
        public static string MaterialAt(Vector3 point)
        {return Find(point,BuildingMission.FloorAt(point),out var hit)?hit.collider.GetComponent<BuildingSurface18>().FootMaterial:"concrete";}
    }
    public sealed class BuildingActor18:MonoBehaviour
    {
        public int FloorId;public string SupportId;public bool Initialized;
        public void Place(Vector3 at,int floor,string support)
        {
            var agent=GetComponent<NavMeshAgent>();agent.enabled=false;
            foreach(var body in GetComponentsInChildren<Rigidbody>()){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
            transform.position=at;FloorId=floor;SupportId=support;Physics.SyncTransforms();agent.enabled=true;
            agent.Warp(at);agent.ResetPath();agent.velocity=Vector3.zero;Initialized=true;
        }
    }
}
