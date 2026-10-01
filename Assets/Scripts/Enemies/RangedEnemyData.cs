using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Distances (from the player):
    ///   retreatDistance  &lt; preferredDistance  &lt; attackRange  (&lt;= detectionRadius usually)
    ///   - farther than preferredDistance: walks toward the player
    ///   - reached preferredDistance with line of sight: stops and shoots
    ///   - keeps shooting while the player stays between retreatDistance and attackRange
    ///   - player closer than retreatDistance: backs off
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Enemies/Ranged Enemy Data", fileName = "RangedEnemyData")]
    public class RangedEnemyData : EnemyData
    {
        [Header("Positioning")]
        [Min(0f)] public float retreatDistance = 2.5f;
        [Min(0f)] public float preferredDistance = 4.5f;
        [Min(0f)] public float attackRange = 6.5f;
        [Range(0.1f, 1.5f)] public float retreatSpeedMultiplier = 0.7f;

        [Header("Aiming")]
        [Tooltip("Gun rotation speed, degrees per second.")]
        [Min(1f)] public float turnSpeed = 540f;
        [Tooltip("Starts a burst only when the gun points at the player within this angle.")]
        [Range(1f, 90f)] public float aimToleranceDeg = 12f;

        [Header("Bursts")]
        [Min(1)] public int bulletsPerBurst = 4;
        [Min(0.01f)] public float timeBetweenShots = 0.1f;
        [Tooltip("Pause between bursts.")]
        [Min(0f)] public float burstCooldown = 1.4f;
        [Tooltip("Delay after entering Attack before the first burst (telegraph).")]
        [Min(0f)] public float firstBurstDelay = 0.35f;
        [Tooltip("Total spread cone in degrees.")]
        [Range(0f, 45f)] public float spreadDeg = 8f;

        [Header("Projectile")]
        public Projectile projectilePrefab;
        [Min(0.1f)] public float projectileSpeed = 8f;
        [Min(1)] public int projectileDamage = 1;
        [Min(0.1f)] public float projectileLifetime = 2.5f;

        protected override void OnValidate()
        {
            base.OnValidate();
            preferredDistance = Mathf.Max(preferredDistance, retreatDistance + 0.5f);
            attackRange = Mathf.Max(attackRange, preferredDistance + 0.5f);
        }
    }
}
