namespace Game.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(DamageInfo damage);
    }
}