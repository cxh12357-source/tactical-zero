using UnityEngine;

namespace TacticalZero
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GrassVolume : MonoBehaviour
    {
        public bool ConcealsStanding=true;
        public bool Contains(Vector3 point)=>GetComponent<BoxCollider>().bounds.Contains(point+Vector3.up*.4f);
        void Awake(){foreach(var collider in GetComponents<BoxCollider>())collider.isTrigger=true;}
        private PlayerVisibilityState occupant;
        private void OnTriggerEnter(Collider other)
        {
            var state = other.GetComponentInParent<PlayerVisibilityState>();
            if (state)
            {
                occupant = state;
                state.Enter(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var state = other.GetComponentInParent<PlayerVisibilityState>();
            if (state)
            {
                state.Exit(this);
                if (occupant == state)
                    occupant = null;
            }
        }

        private void OnDisable()
        {
            if (occupant)
                occupant.Exit(this);
            occupant = null;
        }
    }
}
