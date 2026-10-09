using UnityEngine;

namespace TacticalZero
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FPSPlayerController : MonoBehaviour
    {
        [SerializeField]
        private float walkSpeed = 4.2f, sprintSpeed = 6.4f, crouchSpeed = 2.1f, acceleration = 16, gravity = -22;
        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private int obstacleMask;
        private readonly Collider[] headroom = new Collider[4];
        public bool IsCrouched { get; private set; }
        public bool IsSprinting { get; private set; }
        public float Speed => new Vector2(controller.velocity.x, controller.velocity.z).magnitude;
        public bool Grounded => controller.isGrounded;
        public bool IsAirborne => !Grounded;
        public float VerticalSpeed => verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            obstacleMask = LayerMask.GetMask("TZWorld");
        }

        private void Update()
        {
            var game = GameManager.Instance;
            if (!game || !game.IsPlaying || (BuildingMission.Instance && BuildingMission.Instance.Transitioning))
                return;
            Simulate(game.Input.Move, game.Input.Sprint && !game.Input.Aiming, game.Input.Crouched, game.Input.JumpPressed, Time.deltaTime);
        }

        public void Simulate(Vector2 move, bool sprint, bool crouch, float dt)
            => Simulate(move, sprint, crouch, false, dt);

        public void ResetMotion()
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = -2;
            IsCrouched = IsSprinting = false;
            controller.height = 1.8f;
            controller.center = Vector3.up * .9f;
        }

        public void Simulate(Vector2 move, bool sprint, bool crouch, bool jump, float dt)
        {
            if (dt <= 0)
                return;
            bool blocked = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 1.65f, .29f, headroom, obstacleMask, QueryTriggerInteraction.Ignore) > 0;
            IsCrouched = crouch || (IsCrouched && blocked);
            IsSprinting = sprint && !IsCrouched && move.y > .2f;
            float height = IsCrouched ? 1.15f : 1.8f;
            controller.height = height;
            controller.center = Vector3.up * height * .5f;
            float speed = IsCrouched ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            speed*=MapArea.SpeedAt(transform.position);
            if(BuildingMission.Instance){var gun=GetComponent<WeaponController>().Current;if(gun)speed*=BuildingCatalog.Mobility(gun.Id);}
            var desired = transform.TransformDirection(new Vector3(move.x, 0, move.y));
            desired = Vector3.ClampMagnitude(desired, 1) * speed;
            // Immediate release and reversal; a short start ramp avoids a digital-input lurch.
            if(desired.sqrMagnitude<.0001f||Vector3.Dot(planarVelocity,desired)<0)planarVelocity=desired;
            else planarVelocity=Vector3.MoveTowards(planarVelocity,desired,Mathf.Max(80,acceleration)*dt);
            if (controller.isGrounded && verticalVelocity < 0)
                verticalVelocity = -2;
            if (jump && controller.isGrounded && !IsCrouched && !blocked)
                verticalVelocity = Mathf.Sqrt(-2 * gravity * 1.2f);
            verticalVelocity = Mathf.Max(-30, verticalVelocity + gravity * dt);
            var collision = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            if ((collision & CollisionFlags.Above) != 0 && verticalVelocity > 0) verticalVelocity = 0;
        }
    }
}
