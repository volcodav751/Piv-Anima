using System;

namespace Game.Combat
{
    public interface IHealth
    {
        int Current { get; }
        int Max { get; }
        bool IsAlive { get; }

        event Action<int, int> Changed;
        event Action Died;
    }
}