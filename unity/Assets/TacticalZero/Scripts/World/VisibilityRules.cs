namespace TacticalZero
{
    public static class VisibilityRules
    {
        public static bool CanSee(float distance, float angle, bool blocked, float range, float halfAngle, bool grass, bool crouch, bool moving, bool sprint, bool revealed, float near = 2.8f)
        {
            if (blocked || distance > range)
                return false;
            if (distance <= near)
                return true;
            if (angle > halfAngle)
                return false;
            float visibility = crouch ? .72f : 1;
            if (grass && !revealed)
            {
                if (sprint)
                    visibility *= .55f;
                else if (moving)
                    visibility *= .25f;
                else
                    return false;
            }

            return distance <= range * visibility;
        }
    }
}
