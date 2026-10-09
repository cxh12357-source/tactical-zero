using System;
using UnityEngine;

namespace TacticalZero
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField]
        private float maximumHealth = 100, maximumArmor = 100;
        private PlayerLook look;
        public float Health { get; private set; }
        public float Armor { get; private set; }
        public DamageInfo LastDamage { get; private set; }
        public float LastHurtAt { get; private set; } = -100;

        public float SpawnProtectionSeconds => Mathf.Max(0, protectedUntil-Time.time);
        float protectedUntil;
        public void Revive()
        {
            Health=maximumHealth; Armor=0; LastDamage=default; LastHurtAt=-100;
            protectedUntil=0;
        }
        public void CancelSpawnProtection() { protectedUntil=0; }
        public event Action<DamageInfo> Damaged;
        private void Awake()
        {
            Health = maximumHealth;
            look = GetComponent<PlayerLook>();
        }

        public void Apply(DamageInfo info)
        {
            if (Health <= 0 || SpawnProtectionSeconds > 0 || !GameManager.Instance || !GameManager.Instance.IsPlaying)
                return;
            float armor = Armor;
            float incoming = info.Amount;
            float damage = DamageModel.Absorb(incoming, ref armor);
            Armor = armor;
            float actual = Mathf.Min(Health, damage);
            Health = Mathf.Max(0, Health - damage);
            CombatFeedback.Instance?.ReceiveDamage(info,actual,Health<=0);
            LastDamage = info;
            LastHurtAt = Time.time;
            GameManager.Instance.Stats.damageTaken += actual;
            look?.Hurt();
            Damaged?.Invoke(info);
            AudioService.Instance?.Play("hurt", transform.position, .7f, false);
            if (Health <= 0)
                GameManager.Instance.EndMatch(false);
        }

        public bool Heal(float amount)
        {
            if (Health <= 0 || Health >= maximumHealth)
                return false;
            Health = Mathf.Min(maximumHealth, Health + amount);
            return true;
        }

        public bool AddArmor(float amount)
        {
            if (Health <= 0 || Armor >= maximumArmor)
                return false;
            Armor = Mathf.Min(maximumArmor, Armor + amount);
            return true;
        }
    }
}
