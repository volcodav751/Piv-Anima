using System;

namespace Game.Combat
{
    public interface IHealth
    {
        int Current { get; }
        int Max { get; }

        bool IsAlive { get; }
        float Normalized { get; }

        // Аргументи: поточне та максимальне здоров’я.
        event Action<int, int> Changed;

        event Action Died;
    }
}