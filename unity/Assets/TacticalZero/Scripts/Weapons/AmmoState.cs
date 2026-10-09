using System;

namespace TacticalZero
{
    public sealed class AmmoState
    {
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public int Capacity { get; private set; }
        public int MaximumReserve { get; private set; }
        public float ReloadRemaining { get; private set; }
        public float ReadyAt { get; private set; }

        public AmmoState(int capacity, int reserve)
        {
            Capacity = Math.Max(1, capacity);
            Loaded = Capacity;
            MaximumReserve = Reserve = Math.Max(0, reserve);
        }

        public bool TryFire(float now, float interval)
        {
            if (ReloadRemaining > 0 || Loaded <= 0 || now < ReadyAt)
                return false;
            Loaded--;
            ReadyAt = now + Math.Max(.01f, interval);
            return true;
        }

        public bool StartReload(float duration)
        {
            if (ReloadRemaining > 0 || Loaded == Capacity || Reserve == 0)
                return false;
            ReloadRemaining = Math.Max(.01f, duration);
            return true;
        }

        public void CancelReload()
        {
            ReloadRemaining = 0;
        }

        public void Tick(float dt)
        {
            if (ReloadRemaining <= 0 || dt <= 0)
                return;
            ReloadRemaining = Math.Max(0, ReloadRemaining - dt);
            if (ReloadRemaining == 0)
            {
                int amount = Math.Min(Capacity - Loaded, Reserve);
                Loaded += amount;
                Reserve -= amount;
            }
        }

        public bool Refill(int amount)
        {
            int next = Math.Min(MaximumReserve, Reserve + Math.Max(0, amount));
            bool changed = next != Reserve;
            Reserve = next;
            return changed;
        }
    }
}
