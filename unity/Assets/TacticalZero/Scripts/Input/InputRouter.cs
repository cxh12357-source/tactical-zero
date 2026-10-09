using UnityEngine;
using UnityEngine.InputSystem;

namespace TacticalZero
{
    [DefaultExecutionOrder(-200)]
    public sealed class InputRouter : MonoBehaviour
    {
        private InputActionMap actions;
        private InputAction move, look, fire, ads, reload, sprint, crouch, interact, pause, jump;
        private InputAction[] slots;
        private bool aimLatched, aimBlockedUntilRelease;
        public MobileInputBridge Mobile { get; private set; } = new MobileInputBridge();
        public bool TouchMode => Application.isMobilePlatform || ForceTouch;
        public bool ForceTouch { get; set; }
        public Vector2 Move => TouchMode ? Mobile.Move : move.ReadValue<Vector2>();
        public Vector2 Look => TouchMode ? Mobile.ConsumeLook() : look.ReadValue<Vector2>();
        public bool FireHeld => TouchMode ? Mobile.Fire : fire.IsPressed();
        public bool FirePressed => TouchMode ? Mobile.Take(MobileAction.Fire) : fire.WasPressedThisFrame();
        public bool Aiming => GameManager.Instance && GameManager.Instance.IsPlaying &&
            (TouchMode ? Mobile.Aim : !aimBlockedUntilRelease && (SaveService.Settings.toggleAim ? aimLatched : ads.IsPressed()));
        public bool Sprint => TouchMode ? Mobile.Sprint : sprint.IsPressed();
        public bool Crouched => TouchMode ? Mobile.Crouch : crouch.IsPressed();
        public bool ReloadPressed => TouchMode ? Mobile.Take(MobileAction.Reload) : reload.WasPressedThisFrame();
        public bool InteractPressed => TouchMode ? Mobile.Take(MobileAction.Interact) : interact.WasPressedThisFrame();

        public bool JumpPressed => TouchMode ? Mobile.Take(MobileAction.Jump) : jump.WasPressedThisFrame();

        public int WeaponSlot()
        {
            if (TouchMode)
                return Mobile.Take(MobileAction.Switch) ? -2 : -1;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].WasPressedThisFrame())
                    return i;
            return -1;
        }

        private void Awake()
        {
            actions = new InputActionMap("Tactical Player");
            move = actions.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = actions.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            fire = Button("Fire", "<Mouse>/leftButton");
            ads = Button("ADS", "<Mouse>/rightButton");
            reload = Button("Reload", "<Keyboard>/r");
            sprint = Button("Sprint", "<Keyboard>/leftShift");
            crouch = Button("Crouch", "<Keyboard>/leftCtrl");
            crouch.AddBinding("<Keyboard>/c");
            interact = Button("Interact", "<Keyboard>/e");
            pause = Button("Pause", "<Keyboard>/escape");
            jump = Button("Jump", "<Keyboard>/space");
            slots = new[]
            {
                Button("Slot1", "<Keyboard>/1"),
                Button("Slot2", "<Keyboard>/2"),
                Button("Slot3", "<Keyboard>/3")
            };
            actions.Enable();
        }

        private InputAction Button(string name, string binding) => actions.AddAction(name, InputActionType.Button, binding);
        private void Update()
        {
            if(!Application.isMobilePlatform&&Keyboard.current!=null&&Keyboard.current.f11Key.wasPressedThisFrame)DisplayMode.Toggle();
            if (pause.WasPressedThisFrame() || Mobile.Take(MobileAction.Pause))
                GameManager.Instance?.TogglePause();
            if(!ads.IsPressed())aimBlockedUntilRelease=false;
            var game=GameManager.Instance;
            if(!game||!game.IsPlaying||TouchMode)
            {
                aimLatched=false;
                if(ads.IsPressed())aimBlockedUntilRelease=true;
                return;
            }
            // Resolve the press once per frame; camera, movement and weapons read one state.
            if(!aimBlockedUntilRelease&&SaveService.Settings.toggleAim&&ads.WasPressedThisFrame())aimLatched=!aimLatched;
        }

        public void Clear()
        {
            TouchControl.ReleaseAll();
            Mobile.Clear();
            aimLatched=false;
            aimBlockedUntilRelease=ads!=null&&ads.IsPressed();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                Clear();
                GameManager.Instance?.Pause(false);
            }
        }

        private void OnDestroy()
        {
            actions?.Disable();
            actions?.Dispose();
        }
    }
}
