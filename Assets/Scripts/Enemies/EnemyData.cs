using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Stats shared by every enemy type. Tune in the asset, not in code.</summary>
    [CreateAssetMenu(menuName = "Game/Enemies/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Health")]
        [Min(1)] public int maxHealth = 5;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 2.5f;
        [Tooltip("How fast velocity changes (units/s²). Higher = snappier.")]
        [Min(0.1f)] public float acceleration = 25f;
        [Tooltip("Velocity added when hit. 0 = no knockback.")]
        [Min(0f)] public float knockback = 3f;

        [Header("Perception")]
        [Tooltip("Player closer than this (and visible) gets noticed.")]
        [Min(0f)] public float detectionRadius = 8f;

        [Header("Spawn")]
        [Tooltip("Enemy does nothing for this long after spawning — gives the player a moment.")]
        [Min(0f)] public float activationDelay = 0.6f;

        [Header("Idle patrol")]
        public bool patrol = true;
        [Min(0f)] public float patrolRadius = 1.5f;
        [Range(0.1f, 1f)] public float patrolSpeedMultiplier = 0.5f;
        public Vector2 patrolWaitRange = new Vector2(1f, 2.5f);

        [Header("Death")]
        [Tooltip("Duration of the death effect before the object is destroyed.")]
        [Min(0f)] public float deathDuration = 0.4f;

        protected virtual void OnValidate()
        {
            if (patrolWaitRange.y < patrolWaitRange.x) patrolWaitRange.y = patrolWaitRange.x;
        }
    }
}
