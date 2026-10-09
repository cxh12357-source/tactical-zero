using UnityEngine;
using UnityEngine.AI;

namespace TacticalZero
{
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField]
        private EnemyDefinition definition;
        [SerializeField]
        private Vector3[] patrolPoints;
        private NavMeshAgent agent;
        private EnemyPerception perception;
        private EnemyCombat combat;
        private EnemyHealth health;
        private PlayerHealth player;
        private PlayerVisibilityState visibility;
        private Vector3 home, lastKnown;
        private int patrolIndex;
        private int movementVariant;
        private float nextThink, lastThink, nextReposition;
        public EnemyStateMachine Machine { get; private set; } = new EnemyStateMachine();
        public Vector3 LastKnown => lastKnown;

        public void Configure(EnemyDefinition data)
        {
            definition = data;
        }

        public void SetPatrol(Vector3[] points)
        {
            patrolPoints = points;
        }

        private void Awake()
        {
            movementVariant = Random.Range(0, 40);
            agent = GetComponent<NavMeshAgent>();
            perception = GetComponent<EnemyPerception>();
            combat = GetComponent<EnemyCombat>();
            health = GetComponent<EnemyHealth>();
            health.Damaged += OnDamage;
            health.Died += OnDeath;
        }

        private void OnEnable()
        {
            NoiseBus.Raised += Hear;
        }

        private void OnDisable()
        {
            NoiseBus.Raised -= Hear;
        }

        private void Start()
        {
            home = transform.position;
            lastKnown = home;
            lastThink = Time.time;
            nextThink = Time.time + Random.value * .2f;
            var game = GameManager.Ensure();
            player = game.Player;
            if (player)
                visibility = player.GetComponent<PlayerVisibilityState>();
            if (!definition)
            {
                Debug.LogError("EnemyDefinition missing on " + name + ". Rerun setup or assign a definition.", this);
                enabled = false;
                return;
            }

            Machine.SearchSeconds = definition.SearchSeconds;
            if (NavMesh.SamplePosition(home, out var hit, 2, NavMesh.AllAreas))
                agent.Warp(hit.position);
            agent.avoidancePriority = 30 + movementVariant;
        }

        private void Update()
        {
            var game = GameManager.Instance;
            if (!game || !game.IsPlaying || !health.Alive || Time.time < nextThink)
                return;
            float dt = Time.time - lastThink;
            lastThink = Time.time;
            nextThink = Time.time + (game.Settings.quality == 0 ? .25f : .12f) * (player && Vector3.Distance(player.transform.position, transform.position) > 32 ? 2 : 1);
            if (!player)
            {
                player = game.Player;
                if (player)
                    visibility = player.GetComponent<PlayerVisibilityState>();
            }

            if (!player || !definition)
                return;
            bool sees = perception.Sees(player, visibility, definition);
            if (sees)
                lastKnown = player.transform.position;
            bool reached = agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 1;
            var prior = Machine.State;
            Machine.Tick(sees, reached, dt);
            if (Machine.State == EnemyState.Combat)
            {
                if (prior != EnemyState.Combat)
                    AudioService.Instance?.Play("alert", transform.position, .6f, true);
                Vector3 direction = lastKnown - transform.position;
                direction.y = 0;
                if (direction.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), .55f);
                if (Time.time >= nextReposition)
                {
                    nextReposition = Time.time + 1.0f;
                    Vector3 offset = Vector3.Cross(direction.normalized, Vector3.up) * (movementVariant % 2 == 0 ? 2 : -2);
                    Destination(lastKnown - direction.normalized * definition.PreferredRange + offset);
                }

                if (sees)
                    combat.Tick(player, definition, Machine.TimeInState);
            }
            else if (Machine.State == EnemyState.Investigate || Machine.State == EnemyState.Search)
            {
                Destination(lastKnown);
                if (reached && Machine.State == EnemyState.Search)
                    transform.Rotate(0, 35 * dt, 0);
            }
            else if (Machine.State == EnemyState.Return)
                Destination(home);
            else if (Machine.State == EnemyState.Patrol)
            {
                if (patrolPoints == null || patrolPoints.Length == 0)
                {
                    Destination(home);
                    return;
                }

                if (reached)
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                Destination(patrolPoints[patrolIndex]);
            }
        }

        private void Destination(Vector3 point)
        {
            if (!agent.isOnNavMesh)
                return;
            agent.isStopped = false;
            agent.updateRotation = Machine.State != EnemyState.Combat;
            agent.speed = definition.Speed * (Machine.State == EnemyState.Patrol ? .65f : 1);
            if (NavMesh.SamplePosition(point, out var hit, 3, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        private void Hear(Vector3 origin, float radius, NoiseKind kind)
        {
            if (!health || !health.Alive || Machine.State == EnemyState.Combat || !perception.Hears(origin, radius))
                return;
            // A noise gives a snapshot location, never a reference that tracks a concealed player.
            lastKnown = origin;
            Machine.Hear();
        }

        private void OnDamage(DamageInfo info)
        {
            lastKnown = info.Origin;
            Machine.Hit();
        }

        private void OnDeath()
        {
            Machine.Kill();
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }
    }
}
