using UnityEngine;

namespace Game.Enemies
{
    [CreateAssetMenu(
        fileName = "Kamikaze_Data",
        menuName = "Game/Enemies/Kamikaze Enemy Data"
    )]
    public class KamikazeEnemyData : ScriptableObject
    {
        [Header("Health")]
        [Min(1)]
        public int maxHealth = 3;

        [Header("Movement")]
        [Min(0f)]
        public float moveSpeed = 2.8f;

        [Min(0f)]
        public float detectionRadius = 3.5f;

        [Header("Explosion")]
        [Min(0f)]
        public float triggerRange = 0.25f;

        [Min(0f)]
        public float fuseTime = 0.4f;

        [Min(0f)]
        public float explosionRadius = 0.6f;

        [Min(1)]
        public int explosionDamage = 40;

        [Header("Knockback")]
        [Min(0f)]
        public float knockbackSpeed = 5f;

        [Min(0f)]
        public float knockbackStun = 0.25f;

        [Header("Effects")]
        public GameObject explosionVfxPrefab;

        public GameObject deathVfxPrefab;
    }
}