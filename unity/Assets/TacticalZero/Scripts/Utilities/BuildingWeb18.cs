using UnityEngine;
namespace TacticalZero
{
    public static class BuildingWeb18
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void TZ18MenuState(int ready);
#endif
        public static void Phase(GamePhase phase)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            TZ18MenuState(phase==GamePhase.MainMenu?1:0);
#endif
        }
    }
}
