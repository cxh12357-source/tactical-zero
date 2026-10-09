using UnityEngine;

namespace TacticalZero
{
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField]
        private Camera view;
        private FPSPlayerController movement;
        private AimAssistSystem assist;
        private float pitch, kick, shake, bob, roll, recovery = 12;
        public Camera View => view;

        public void Configure(Camera camera)
        {
            view = camera;
        }

        private void Awake()
        {
            movement = GetComponent<FPSPlayerController>();
            assist = GetComponent<AimAssistSystem>();
            if (!view)
                view = GetComponentInChildren<Camera>();
        }

        private void LateUpdate()
        {
            var game = GameManager.Instance;
            if (!game || !game.IsPlaying || !view)
                return;
            var input = game.Input;
            var settings = game.Settings;
            Vector2 delta = input.Look;
            float sensitivity = input.TouchMode ? settings.touchSensitivity * .065f : settings.mouseSensitivity * .07f;
            sensitivity *= input.Aiming ? settings.adsSensitivity : 1;
            if (input.TouchMode && assist)
                sensitivity *= assist.Friction(delta.magnitude);
            transform.Rotate(0, delta.x * sensitivity, 0);
            pitch = Mathf.Clamp(pitch + delta.y * sensitivity * (settings.invertY ? 1 : -1), -84, 84);
            kick = Mathf.MoveTowards(kick, 0, Time.deltaTime * recovery);
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 3);
            bob += movement.Speed * Time.deltaTime * 2.2f;
            float y = movement.IsCrouched ? .99f : 1.6f;
            float strength=settings.cameraShake?(input.Aiming?.12f:1):0;
            float moving=Mathf.Clamp01(movement.Speed/4.2f);
            float motion=(Mathf.Sin(bob*2)*.017f-Mathf.Abs(Mathf.Sin(bob))*.009f)*moving*strength;
            float targetRoll=(-input.Move.x*1.4f+Mathf.Sin(bob)*.7f*moving-Mathf.Clamp(delta.x*sensitivity,-2,2)*.2f)*strength;
            roll=Mathf.Lerp(roll,targetRoll,1-Mathf.Exp(-Time.deltaTime*14));
            view.transform.localPosition = Vector3.Lerp(view.transform.localPosition, new Vector3(Mathf.Sin(bob)*.009f*moving*strength, y + motion, 0), Time.deltaTime * 14);
            view.transform.localRotation = Quaternion.Euler(pitch - kick, 0, roll+(settings.cameraShake ? Mathf.Sin(Time.time * 48) * shake : 0));
        }

        public void ResetView()
        {
            pitch=kick=shake=bob=roll=0;
            view.transform.localPosition=new Vector3(0,1.6f,0);
            view.transform.localRotation=Quaternion.identity;
            view.fieldOfView=75;
        }

        public void Recoil(float amount, float recoverySpeed)
        {
            kick = Mathf.Min(9, kick + amount);
            recovery = Mathf.Max(1, recoverySpeed);
        }

        public void Hurt()
        {
            if (GameManager.Instance.Settings.cameraShake)
                shake = .65f;
        }
    }
}
