using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Anything that can be hit: player, enemies, destructible props.
    /// All attacks in the game should go through this interface.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damage);
    }
}
