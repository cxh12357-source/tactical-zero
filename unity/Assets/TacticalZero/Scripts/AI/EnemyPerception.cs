using UnityEngine;

namespace TacticalZero
{
    public sealed class EnemyPerception : MonoBehaviour
    {
        private int obstacles;
        private void Awake()
        {
            obstacles = LayerMask.GetMask("TZWorld", "TZInteractable");
        }

        public bool Sees(PlayerHealth player, PlayerVisibilityState visibility, EnemyDefinition data)
        {
            if (!player || player.Health <= 0 || !visibility || !data || !BuildingMission.SameFloor(transform.position,player.transform.position))
                return false;
            Vector3 eye = transform.position + Vector3.up * 1.55f, target = player.transform.position + Vector3.up * (visibility.Crouched ? .8f : 1.2f), line = target - eye;
            bool blocked = Physics.Linecast(eye, target, obstacles, QueryTriggerInteraction.Ignore);
            return VisibilityRules.CanSee(line.magnitude, Vector3.Angle(transform.forward, line), blocked, data.SightRange * data.LightVisibility, data.SightAngle * .5f, visibility.ConcealedByGrass, visibility.Crouched, visibility.Moving, visibility.Sprinting, visibility.Revealed, data.NearRevealDistance);
        }

        public bool Hears(Vector3 origin, float radius)
        {
            bool blocked = Physics.Linecast(transform.position + Vector3.up, origin + Vector3.up, obstacles, QueryTriggerInteraction.Ignore);
            return Vector3.Distance(transform.position, origin) < radius * (blocked ? .5f : 1);
        }
    }
}
