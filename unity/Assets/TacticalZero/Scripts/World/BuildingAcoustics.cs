using UnityEngine;
namespace TacticalZero
{
    // Gameplay propagation is independent of audio volume and the bounded playback pool.
    public static class BuildingAcoustics
    {
        public struct Transmission {public float Gain,Cutoff; public int Walls;}
        struct Entry {public Vector3 A,B;public float Until;public int Door,Map;public Transmission Value;}
        static readonly Entry[] cache=new Entry[256];static int slot;
        static readonly RaycastHit[] hits=new RaycastHit[16];
        public static int RayQueries {get;private set;}
        public static int Walls(Vector3 a,Vector3 b)
        {
            Vector3 delta=b-a;RayQueries++;
            return Physics.RaycastNonAlloc(a,delta.normalized,hits,delta.magnitude,LayerMask.GetMask("TZWorld","TZInteractable"),QueryTriggerInteraction.Ignore);
        }
        public static Transmission Trace(Vector3 a,Vector3 b)
        {
            int map=BuildingMission.Instance?BuildingMission.Instance.MapIndex:-1;
            for(int i=0;i<cache.Length;i++)
            {
                ref var e=ref cache[i];
                if(e.Until>Time.unscaledTime&&e.Map==map&&e.Door==BuildingDoor.Revision&&(e.A-a).sqrMagnitude<.36f&&(e.B-b).sqrMagnitude<.36f)return e.Value;
            }
            int walls=Walls(a,b),floors=Mathf.Abs(BuildingMission.FloorAt(a)-BuildingMission.FloorAt(b));
            float gain=Mathf.Pow(.56f,Mathf.Min(5,walls))*Mathf.Pow(.45f,floors);
            float cutoff=Mathf.Max(350,18000*Mathf.Pow(.55f,walls)*Mathf.Pow(.4f,floors));
            if(floors==1&&map>=0)
            {
                int lower=Mathf.Min(BuildingMission.FloorAt(a),BuildingMission.FloorAt(b));
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 bottom=BuildingArchitecture18.Stair(map,lower,side,true)+Vector3.up;
                    Vector3 top=BuildingArchitecture18.Stair(map,lower+1,side,false)+Vector3.up;
                    Vector3 low=BuildingMission.FloorAt(a)<BuildingMission.FloorAt(b)?a:b,high=low==a?b:a;
                    float detour=Vector3.Distance(low,bottom)+Vector3.Distance(bottom,top)+Vector3.Distance(top,high);
                    if(detour<45&&Walls(low,bottom)==0&&Walls(top,high)==0)
                    {gain=Mathf.Max(gain,.48f*Mathf.Clamp01(1-detour/70));cutoff=Mathf.Max(cutoff,3500);}
                }
            }
            var result=new Transmission{Gain=gain,Cutoff=cutoff,Walls=walls};
            cache[slot++%cache.Length]=new Entry{A=a,B=b,Until=Time.unscaledTime+.12f,Door=BuildingDoor.Revision,Map=map,Value=result};return result;
        }
        public static float Attenuation(Vector3 listener,Vector3 origin,float radius)
        {
            float gain=Mathf.Clamp01(1-Vector3.Distance(listener,origin)/Mathf.Max(1,radius));
            return gain<=0?0:gain*gain*Trace(listener+Vector3.up,origin+Vector3.up).Gain;
        }
        public static void Emit(Vector3 point,float radius,NoiseKind kind)
        {
            NoiseBus.Emit(point,radius,kind);
            if(kind==NoiseKind.Footstep||kind==NoiseKind.Stair)AudioService.Instance?.Step(point,.45f);
            else AudioService.Instance?.Play(kind==NoiseKind.Reload?"reload":kind==NoiseKind.Gunshot?"AK47":kind==NoiseKind.Door?"building-door":kind==NoiseKind.Impact?"building-impact":"alert",point+Vector3.up*.8f,.65f,true);
        }
        public static void Clear(){System.Array.Clear(cache,0,cache.Length);slot=0;RayQueries=0;}
    }
}
