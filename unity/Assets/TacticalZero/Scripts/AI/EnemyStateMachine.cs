namespace TacticalZero
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Suspicious,
        Investigate,
        Combat,
        Search,
        Return,
        Dead
    }

    public sealed class EnemyStateMachine
    {
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public float TimeInState { get; private set; }
        public float SearchSeconds { get; set; } = 8;

        public void Change(EnemyState state)
        {
            if (State == EnemyState.Dead || State == state)
                return;
            State = state;
            TimeInState = 0;
        }

        public void Hear()
        {
            if (State == EnemyState.Patrol || State == EnemyState.Idle || State == EnemyState.Return)
                Change(EnemyState.Suspicious);
            else if (State == EnemyState.Search)
                Change(EnemyState.Investigate);
        }

        public void Hit()
        {
            if (State != EnemyState.Combat)
                Change(EnemyState.Investigate);
        }

        public void Kill()
        {
            Change(EnemyState.Dead);
        }

        public void Tick(bool sees, bool reached, float dt)
        {
            if (State == EnemyState.Dead)
                return;
            TimeInState += dt;
            if (sees)
            {
                Change(EnemyState.Combat);
                return;
            }

            switch (State)
            {
                case EnemyState.Idle:
                    if (TimeInState >= .3f)
                        Change(EnemyState.Patrol);
                    break;
                case EnemyState.Combat:
                    Change(EnemyState.Search);
                    break;
                case EnemyState.Suspicious:
                    if (TimeInState >= .45f)
                        Change(EnemyState.Investigate);
                    break;
                case EnemyState.Investigate:
                    if (reached || TimeInState > 12)
                        Change(EnemyState.Search);
                    break;
                case EnemyState.Search:
                    if (TimeInState >= SearchSeconds)
                        Change(EnemyState.Return);
                    break;
                case EnemyState.Return:
                    if (reached)
                        Change(EnemyState.Patrol);
                    break;
            }
        }
    }
}
