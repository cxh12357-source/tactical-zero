using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalZero
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        private static readonly List<EnemyHealth> active = new List<EnemyHealth>(12);
        public static IReadOnlyList<EnemyHealth> Active => active;

        [SerializeField]
        private EnemyDefinition definition;
        private Collider[] colliders;
        public float Health { get; private set; }
        public bool Alive => Health > 0;
        public Vector3 AimPoint => transform.position + Vector3.up * 1.0f;

        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public float HurtAt { get; private set; } = -100;

        public void Configure(EnemyDefinition data)
        {
            definition = data;
            if(BuildingCatalog.InScene)Health=data.Health;
        }

        private void Awake()
        {
            Health = definition ? definition.Health : 100;
            colliders = GetComponentsInChildren<Collider>();
        }

        private void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntimeState()
        {
            active.Clear();
        }

        public void Apply(DamageInfo info)
        {
            if (!Alive || info.Amount <= 0)
                return;
            float actual=Mathf.Min(Health,info.Amount);
            Health = Mathf.Max(0, Health - info.Amount);
            CombatFeedback.Instance?.ConfirmHit(name,info,actual,!Alive);
            HurtAt = Time.time;
            Damaged?.Invoke(info);
            AudioService.Instance?.Play("enemy-hurt", transform.position, .5f, true);
            NoiseBus.Emit(transform.position, 10, NoiseKind.Hurt);
            if (Alive)
                return;
            foreach (var collider in colliders)
                collider.enabled = false;
            active.Remove(this);
            Died?.Invoke();
            if (GameManager.Instance)
                GameManager.Instance.Stats.kills++;
        }
    }
}
