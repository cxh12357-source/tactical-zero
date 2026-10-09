using System;
using UnityEngine;

namespace TacticalZero
{
    public enum NoiseKind
    {
        Gunshot,
        Footstep,
        Hurt,
        Terminal,
        Reload, Door, Voice, Impact, Stair
    }

    public static class NoiseBus
    {
        public static event Action<Vector3, float, NoiseKind> Raised;
        public static void Emit(Vector3 position, float radius, NoiseKind kind)
        {
            Raised?.Invoke(position, radius, kind);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Raised = null;
        }
    }
}
