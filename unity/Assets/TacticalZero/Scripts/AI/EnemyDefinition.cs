using UnityEngine;

namespace TacticalZero
{
    [CreateAssetMenu(menuName = "Tactical Zero/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField]
        private string displayName = "步枪兵";
        [SerializeField]
        private float health = 100, speed = 2.8f, sightRange = 22, sightAngle = 95, preferredRange = 13, damage = 7, shotInterval = .18f;
        [SerializeField]
        private int burstCount = 3;
        [SerializeField]
        private float burstRest = 1.1f, searchSeconds = 8, reactionSeconds = .45f, nearRevealDistance = 2.8f, lightVisibility = 1;
        public string DisplayName => displayName;
        public float Health => health;
        public float Speed => speed;
        public float SightRange => sightRange;
        public float SightAngle => sightAngle;
        public float PreferredRange => preferredRange;
        public float Damage => damage;
        public float ShotInterval => shotInterval;
        public int BurstCount => burstCount;
        public float BurstRest => burstRest;
        public float SearchSeconds => searchSeconds;
        public float ReactionSeconds => reactionSeconds;
        public float NearRevealDistance => nearRevealDistance;
        public float LightVisibility => lightVisibility;

        public void Configure(string label, float hp, float pace, float sight, float angle, float distance, float hit, int burst, float interval)
        {
            displayName = label;
            health = hp;
            speed = pace;
            sightRange = sight;
            sightAngle = angle;
            preferredRange = distance;
            damage = hit;
            burstCount = burst;
            shotInterval = interval;
        }
    }
}
