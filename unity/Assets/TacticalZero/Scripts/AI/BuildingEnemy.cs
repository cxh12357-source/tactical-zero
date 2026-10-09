using UnityEngine;
using UnityEngine.AI;
namespace TacticalZero
{
    public enum SweepEnemyState { IDLE,PATROL,HEARD_SOUND,INVESTIGATE,SEARCH,ALERT,TAKE_COVER,ATTACK,CHASE,CHANGE_FLOOR,DEAD }
    public sealed class BuildingEnemy:MonoBehaviour
    {
        public SweepEnemyState State {get;private set;}
        public Vector3 LastKnown {get;private set;}
        EnemyHealth health;EnemyCombat combat;EnemyPerception perception;EnemyDefinition data;NavMeshAgent agent;BuildingMission mission;
        Vector3 home,patrol,previous;float walked;float nextThink,nextPath,lastSeen=-100,heardAt=-100,attackAt,stepAt,nextCover;int variant;bool alerted;
        public void Configure(BuildingMission owner,EnemyDefinition definition,Vector3 route)
        {
            mission=owner;data=definition;patrol=route;home=transform.position;previous=home;LastKnown=home;
            agent=GetComponent<NavMeshAgent>();health=GetComponent<EnemyHealth>();combat=GetComponent<EnemyCombat>();perception=GetComponent<EnemyPerception>();
            health.Damaged+=Hurt;health.Died+=Dead;variant=Random.Range(0,11);agent.speed=data.Speed;agent.avoidancePriority=30+variant;agent.stoppingDistance=.35f;agent.autoTraverseOffMeshLink=true;
            if(isActiveAndEnabled)NoiseBus.Raised+=Hear;nextThink=Time.time+variant*.02f;
        }
        void OnEnable(){if(health)NoiseBus.Raised+=Hear;}
        void OnDisable(){NoiseBus.Raised-=Hear;}
        void StateTo(SweepEnemyState state){if(state==State)return;State=state;if(state==SweepEnemyState.ALERT)attackAt=Time.time;}
        void Update()
        {
            if(!health||!health.Alive||!GameManager.Instance.IsPlaying||Time.time<nextThink)return;
            var player=GameManager.Instance.Player;if(!player)return;
            bool same=BuildingMission.SameFloor(transform.position,player.transform.position);
            nextThink=Time.time+(same?.14f:.45f);
            if(!agent.isOnNavMesh)return;
            if(agent.isOnOffMeshLink){StateTo(SweepEnemyState.CHANGE_FLOOR);return;}
            float moved=Vector3.Distance(previous,transform.position);previous=transform.position;if(moved<3)walked+=moved;else walked=0;
            if(walked>1.65f){walked=0;BuildingAcoustics.Emit(transform.position,13,NoiseKind.Footstep);}
            if(TryGetComponent<BuildingActor18>(out var actor))actor.FloorId=BuildingMission.FloorAt(transform.position);
            bool sees=perception.Sees(player,player.GetComponent<PlayerVisibilityState>(),data);
            if(sees)
            {
                LastKnown=player.transform.position;lastSeen=Time.time;
                if(!alerted){alerted=true;StateTo(SweepEnemyState.ALERT);BuildingAcoustics.Emit(transform.position,20,NoiseKind.Voice);}
                float reaction=ClassicRules.Difficulty.reaction[0];if(Time.time-attackAt<reaction)return;
                StateTo(SweepEnemyState.ATTACK);
                Vector3 toward=LastKnown-transform.position;toward.y=0;if(toward.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(toward),.65f);
                agent.updateRotation=false;
                combat.Tick(player,data,Time.time-attackAt);
                if(Time.time>nextCover)
                {
                    nextCover=Time.time+2.5f+variant*.1f;
                    bool seek=Random.value<.2f+ClassicRules.DifficultyIndex*.18f;
                    if(seek&&TryCover(player.transform.position,out var cover)){StateTo(SweepEnemyState.TAKE_COVER);Go(cover);}
                    else if(toward.magnitude>16)Go(LastKnown-toward.normalized*10);
                    else if(agent.hasPath&&agent.remainingDistance<1)agent.ResetPath();
                }
                return;
            }
            agent.updateRotation=true;
            if(alerted&&Time.time-lastSeen<8+ClassicRules.DifficultyIndex*3){StateTo(same?SweepEnemyState.CHASE:SweepEnemyState.CHANGE_FLOOR);Go(LastKnown);}
            else if(Time.time-heardAt<12)
            {
                StateTo(Time.time-heardAt<.4f?SweepEnemyState.HEARD_SOUND:SweepEnemyState.INVESTIGATE);
                if(State==SweepEnemyState.INVESTIGATE)Go(LastKnown);
            }
            else if(alerted||State==SweepEnemyState.INVESTIGATE||State==SweepEnemyState.SEARCH)
            {
                StateTo(SweepEnemyState.SEARCH);transform.Rotate(0,20,0);alerted=false;
                if(Time.time-Mathf.Max(lastSeen,heardAt)>20)StateTo(SweepEnemyState.PATROL);
            }
            else
            {
                StateTo(SweepEnemyState.PATROL);Go((transform.position-patrol).sqrMagnitude<2?home:patrol);
                foreach(var body in mission.Roster)if(body&&!body.Alive&&(body.transform.position-transform.position).sqrMagnitude<36&&!Physics.Linecast(transform.position+Vector3.up,body.transform.position+Vector3.up*.2f,LayerMask.GetMask("TZWorld","TZInteractable")))
                {LastKnown=body.transform.position;heardAt=Time.time;StateTo(SweepEnemyState.INVESTIGATE);break;}
            }
        }
        bool TryCover(Vector3 threat,out Vector3 point)
        {
            for(int i=0;i<5;i++)
            {
                var test=transform.position+Quaternion.Euler(0,i*72+variant*17,0)*Vector3.forward*3;
                if(NavMesh.SamplePosition(test,out var hit,1,NavMesh.AllAreas)&&BuildingMission.SameFloor(hit.position,transform.position)&&Physics.Linecast(hit.position+Vector3.up,threat+Vector3.up,LayerMask.GetMask("TZWorld","TZInteractable")))
                {point=hit.position;return true;}
            }
            point=transform.position;return false;
        }
        void Go(Vector3 point)
        {
            if(Time.time<nextPath)return;nextPath=Time.time+.8f+variant*.04f;
            long start=System.Diagnostics.Stopwatch.GetTimestamp();
            if(NavMesh.SamplePosition(point,out var hit,1.2f,NavMesh.AllAreas)){agent.isStopped=false;agent.SetDestination(hit.position);BuildingPerformance18.PathRequests++;}
            BuildingPerformance18.NavigationMilliseconds+=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        void Hear(Vector3 origin,float radius,NoiseKind kind)
        {
            if(!isActiveAndEnabled||!health||!health.Alive||(origin-transform.position).sqrMagnitude<.7f||State==SweepEnemyState.ATTACK||State==SweepEnemyState.ALERT)return;
            float hearing=.8f+ClassicRules.DifficultyIndex*.15f;
            if(BuildingAcoustics.Attenuation(transform.position,origin,radius*hearing)<.025f)return;
            // A sound contains a position at emission, never the player's current transform.
            LastKnown=origin;
            // Repeated steps/gunfire update the snapshot without restarting the reaction delay.
            if(State!=SweepEnemyState.HEARD_SOUND&&State!=SweepEnemyState.INVESTIGATE){heardAt=Time.time;StateTo(SweepEnemyState.HEARD_SOUND);}
        }
        void Hurt(DamageInfo hit){LastKnown=hit.Origin;lastSeen=Time.time;alerted=true;attackAt=Time.time;StateTo(SweepEnemyState.CHASE);}
        void Dead(){StateTo(SweepEnemyState.DEAD);if(agent.isOnNavMesh)agent.ResetPath();agent.enabled=false;}
        void OnDestroy(){NoiseBus.Raised-=Hear;if(health){health.Damaged-=Hurt;health.Died-=Dead;}}
    }
}
