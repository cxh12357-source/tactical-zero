using UnityEngine;

namespace TacticalZero
{
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        private FPSPlayerController movement;
        private float distance; private Vector3 previous;
        private void Awake()
        {
            movement = GetComponent<FPSPlayerController>(); previous=transform.position;
        }

        private void Update()
        {
            float moved=Vector3.Distance(transform.position,previous);previous=transform.position;
            if(moved>3){distance=0;return;}
            if (!GameManager.Instance || !GameManager.Instance.IsPlaying || !movement.Grounded)
                return;
            distance += moved;
            if (distance < (movement.IsSprinting ? 2.1f : 1.7f))
                return;
            distance = 0;
            float radius = movement.IsCrouched ? 3 : movement.IsSprinting ? 17 : 8;
            if(TryGetComponent<PlayerVisibilityState>(out var visibility)&&visibility.HiddenInGrass)radius*=movement.IsSprinting?1.1f:movement.IsCrouched?.45f:.65f;
            NoiseBus.Emit(transform.position, radius, NoiseKind.Footstep);
            AudioService.Instance?.Step(transform.position, movement.IsSprinting?.4f:.3f);
        }
    }
}
