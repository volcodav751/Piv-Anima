using UnityEngine;

namespace Game.Enemies.Thrower
{
    [CreateAssetMenu(
        fileName = "Thrower_Data",
        menuName = "Game/Enemies/Thrower Enemy Data"
    )]
    public class ThrowerEnemyData : ScriptableObject
    {
        [Header("Health")]
        [Min(1)]
        public int maxHealth = 4;

        [Header("Movement")]
        [Min(0f)]
        public float moveSpeed = 2f;

        [Min(0f)]
        public float detectionRadius = 8f;

        [Header("Combat Distance")]
        [Min(0f)]
        public float retreatDistance = 2.5f;

        [Min(0f)]
        public float preferredDistance = 4.5f;

        [Min(0f)]
        public float throwRange = 6f;

        [Header("Throw")]
        [Min(0.1f)]
        public float throwCooldown = 2f;

        public MolotovProjectile molotovPrefab;

        private void OnValidate()
        {
            preferredDistance =
                Mathf.Max(
                    preferredDistance,
                    retreatDistance
                );

            throwRange =
                Mathf.Max(
                    throwRange,
                    preferredDistance
                );

            detectionRadius =
                Mathf.Max(
                    detectionRadius,
                    throwRange
                );
        }
    }
}