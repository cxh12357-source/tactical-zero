using UnityEngine;

namespace TacticalZero
{
    public sealed class EnemyCombat : MonoBehaviour
    {
        private float nextShot;
        private int burst, mask, obstacles;
        public float ShotAt { get; private set; } = -100;
        public ClassicGun ClassicWeapon { get; set; }
        private void Awake()
        {
            mask = LayerMask.GetMask("TZWorld", "TZPlayer", "TZEnemy", "TZInteractable");
            obstacles = LayerMask.GetMask("TZWorld", "TZInteractable");
        }

        public void Tick(PlayerHealth player, EnemyDefinition definition, float reactionTime)
        {
            if(!isActiveAndEnabled || (player && !BuildingMission.SameFloor(transform.position,player.transform.position)))return;
            var difficulty=ClassicWeapon!=null?ClassicRules.Difficulty:null;
            if (!player || player.Health <= 0 || Time.time < nextShot || reactionTime < (difficulty!=null?difficulty.reaction[0]:definition.ReactionSeconds))
                return;
            Vector3 eye = transform.position + Vector3.up * 1.45f;
            Vector3 aim = player.transform.position + Vector3.up * .9f;
            float distance = Vector3.Distance(eye, aim);
            if (distance > definition.SightRange)
                return;
            if (Physics.Linecast(eye, aim, obstacles, QueryTriggerInteraction.Ignore))
                return;
            Vector3 muzzle = eye + transform.forward * .6f;
            if (Physics.Linecast(eye, muzzle, obstacles, QueryTriggerInteraction.Ignore))
                return;
            ShotAt=Time.time;
            if(BuildingMission.Instance)NoiseBus.Emit(transform.position,50,NoiseKind.Gunshot);
            nextShot = Time.time + (ClassicWeapon!=null?ClassicWeapon.interval:definition.ShotInterval);
            if (++burst >= definition.BurstCount)
            {
                burst = 0;
                nextShot = Time.time + (difficulty!=null?Random.Range(difficulty.rest[0],difficulty.rest[1]):definition.BurstRest);
            }

            bool shotgun = ClassicWeapon!=null?ClassicWeapon.id=="Shotgun":definition.PreferredRange < 8;
            int pellets = ClassicWeapon!=null?ClassicWeapon.pellets:shotgun ? 4 : 1;
            for (int i = 0; i < pellets; i++)
            {
                Vector3 direction = (aim - muzzle).normalized;
                float error=difficulty!=null?difficulty.error*Mathf.Rad2Deg:2.2f;
                direction = Quaternion.Euler(Random.Range(-error,error), Random.Range(-error,error), 0) * direction;
                if (Physics.Raycast(muzzle, direction, out var hit, definition.SightRange, mask, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<PlayerHealth>() == player)
                    player.Apply(new DamageInfo(ClassicWeapon!=null?ClassicWeapon.damage*difficulty.damage:definition.Damage, muzzle, definition.DisplayName, ClassicWeapon!=null?ClassicWeapon.label:shotgun ? "霰弹枪" : "突击步枪"));
            }

            EffectPool.Instance?.Burst(muzzle, new Color(1, .65f, .2f), .1f, .06f);
            AudioService.Instance?.Play(shotgun ? "Shotgun" : "AK47", muzzle, .55f, true);
        }
    }
}
