using UnityEngine;

namespace TacticalZero
{
    public static class HitscanSolver
    {
        private static int worldMask;
        // Camera chooses the sight point; a second trace from the muzzle prevents corner shooting.
        public static bool Trace(Vector3 eye, Vector3 muzzle, Vector3 direction, float range, int mask, out RaycastHit hit)
        {
            if (worldMask == 0) worldMask = LayerMask.GetMask("TZWorld", "TZInteractable");
            if (Physics.Linecast(eye, muzzle, out hit, worldMask, QueryTriggerInteraction.Ignore))
                return true;
            Vector3 end = eye + direction * range;
            if (Physics.Raycast(eye, direction, out var aimed, range, mask, QueryTriggerInteraction.Ignore))
                end = aimed.point;
            Vector3 line = end - muzzle;
            return Physics.Raycast(muzzle, line.normalized, out hit, line.magnitude + .03f, mask, QueryTriggerInteraction.Ignore);
        }
    }
}
