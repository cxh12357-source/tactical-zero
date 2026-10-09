using System;

namespace TacticalZero
{
    public static class DamageModel
    {
        public static float Calculate(float damage, bool headshot, float multiplier, float distance, float range, bool falloff)
        {
            float factor = falloff ? Math.Max(.25f, 1 - distance / Math.Max(1, range) * .75f) : 1;
            return Math.Max(0, damage) * (headshot ? Math.Max(1, multiplier) : 1) * factor;
        }

        public static float Absorb(float damage, ref float armor, float ratio = .6f)
        {
            damage = Math.Max(0, damage);
            float used = Math.Min(Math.Max(0, armor), damage * Math.Max(0, Math.Min(1, ratio)));
            armor = Math.Max(0, armor - used);
            return damage - used;
        }
    }
}
