using UnityEngine;

namespace TacticalZero
{
    public enum MobileAction
    {
        Move,
        Look,
        Fire,
        Aim,
        Reload,
        Interact,
        Switch,
        Sprint,
        Crouch,
        Pause,
        Jump = 11
    }

    public sealed class MobileInputBridge
    {
        public Vector2 Move { get; set; }
        public Vector2 LookDelta { get; set; }
        public bool Fire { get; set; }
        public bool Aim { get; set; }
        public bool Sprint { get; set; }
        public bool Crouch { get; set; }

        private readonly bool[] pressed = new bool[12];
        public void Press(MobileAction action)
        {
            pressed[(int)action] = true;
        }

        public bool Take(MobileAction action)
        {
            bool value = pressed[(int)action];
            pressed[(int)action] = false;
            return value;
        }

        public Vector2 ConsumeLook()
        {
            var result = LookDelta;
            LookDelta = Vector2.zero;
            return result;
        }

        public void Clear()
        {
            Move = LookDelta = Vector2.zero;
            Fire = Aim = Sprint = Crouch = false;
            System.Array.Clear(pressed, 0, pressed.Length);
        }
    }
}
