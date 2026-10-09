using System;
using UnityEngine;

namespace TacticalZero.Tests
{
    // Drive the camera in its real LateUpdate phase; observe the HUD before rendering.
    [DefaultExecutionOrder(50)]
    public sealed class SupplyFrameDriver : MonoBehaviour
    {
        public Action Drive;
        void LateUpdate() => Drive?.Invoke();
    }

    [DefaultExecutionOrder(1000)]
    public sealed class SupplyFrameObserver : MonoBehaviour
    {
        public Action Observe;
        void LateUpdate() => Observe?.Invoke();
    }
}
