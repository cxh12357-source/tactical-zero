using System;

namespace TacticalZero
{
    public sealed class ObjectiveProgress
    {
        public bool HasIntel { get; private set; }
        public float ExtractProgress { get; private set; }
        public float Duration { get; private set; } = 5;
        public bool Completed { get; private set; }

        public bool Acquire()
        {
            if (HasIntel)
                return false;
            HasIntel = true;
            return true;
        }

        public bool Tick(bool inside, float dt)
        {
            if (Completed)
                return true;
            ExtractProgress = HasIntel && inside ? Math.Min(Duration, ExtractProgress + Math.Max(0, dt)) : 0;
            Completed = HasIntel && ExtractProgress >= Duration;
            return Completed;
        }
    }
}
