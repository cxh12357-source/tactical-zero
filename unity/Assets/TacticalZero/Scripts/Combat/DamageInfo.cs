using UnityEngine;

namespace TacticalZero
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Origin;
        public readonly string Attacker, Weapon;
        public readonly bool Headshot;
        public DamageInfo(float amount, Vector3 origin, string attacker, string weapon, bool headshot = false)
        {
            Amount = amount;
            Origin = origin;
            Attacker = attacker;
            Weapon = weapon;
            Headshot = headshot;
        }
    }
}
