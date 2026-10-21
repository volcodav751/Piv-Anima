using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    [CreateAssetMenu(
        menuName = "Game/Enemies/Ranged Enemy Data",
        fileName = "RangedEnemyData"
    )]
    public class RangedEnemyData : EnemyData
    {
        [Header("Combat")]
        [Min(0f)]
        public float preferredDistance = 4.5f;

        [Header("Burst")]
        [Min(1)]
        public int bulletsPerBurst = 4;

        [Min(0.01f)]
        public float timeBetweenShots = 0.1f;

        [Min(0f)]
        public float burstCooldown = 1.4f;

        [Range(0f, 45f)]
        public float spreadDeg = 8f;

        [Header("Projectile")]
        public Projectile projectilePrefab;

        [Min(0.1f)]
        public float projectileSpeed = 8f;

        [Min(1)]
        public int projectileDamage = 1;

        [Min(0.1f)]
        public float projectileLifetime = 2.5f;

        private void OnValidate()
        {
            detectionRadius =
                Mathf.Max(detectionRadius, preferredDistance);
        }
    }
}