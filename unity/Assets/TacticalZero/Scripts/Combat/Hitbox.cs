using UnityEngine;

namespace TacticalZero
{
    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField]
        private bool head;
        private EnemyHealth health;
        public bool Head => head;

        public EnemyHealth Health
        {
            get
            {
                if (!health)
                    health = GetComponentInParent<EnemyHealth>();
                return health;
            }
        }

        public void Configure(bool isHead)
        {
            head = isHead;
        }
    }
}
