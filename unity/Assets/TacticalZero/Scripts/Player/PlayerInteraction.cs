using UnityEngine;

namespace TacticalZero
{
    // Resolve the current rendered view after PlayerLook.LateUpdate.
    [DefaultExecutionOrder(100)]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField]
        private Camera view;
        [SerializeField]
        private float distance = 2.8f;
        private PlayerHealth health;
        private int mask;
        public string Prompt { get; private set; } = "";
        public IInteractable Target { get; private set; }

        public void Configure(Camera camera)
        {
            view = camera;
        }

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            mask = LayerMask.GetMask("TZWorld", "TZInteractable");
        }

        private void LateUpdate()
        {
            if (!GameManager.Instance || !GameManager.Instance.IsPlaying || !view)
                return;
            Scan();

            if (GameManager.Instance.Input.InteractPressed)
            {
                if (Target != null && Target.Interact(health))
                {
                    AudioService.Instance?.Play("pickup", transform.position, .7f, false);
                    // Consumed supplies disable their collider immediately; clear stale targets now.
                    Scan();
                }
            }
        }

        private void Scan()
        {
            Target = null;
            Prompt = "";
            var ray = new Ray(view.transform.position, view.transform.forward);
            float nearest = distance;
            if (Physics.Raycast(ray, out var hit, distance, mask, QueryTriggerInteraction.Ignore))
            {
                nearest = hit.distance;
                Target = hit.collider.GetComponentInParent<IInteractable>();
            }
            // Holographic supplies have no physics collider: select their visual space only.
            var mission = ClassicEncounter.Instance;
            if (mission) foreach (var loot in mission.Supplies)
            {
                if (!loot || !loot.gameObject.activeInHierarchy) continue;
                var bounds = new Bounds(loot.GroundPosition + Vector3.up * .8f, new Vector3(.95f, 1.6f, .95f));
                if (bounds.IntersectRay(ray, out float reach) && reach < nearest)
                {
                    nearest = reach;
                    Target = loot;
                }
            }
            if (Target == null)
                return;
            bool allowed = Target.CanInteract(health, out string reason);
            Prompt = Target.Label + " · " + (allowed ? "互动 [E]" : reason);
        }
    }
}
