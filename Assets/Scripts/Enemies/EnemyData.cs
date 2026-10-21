using UnityEngine;

namespace Game.Enemies
{
    public class EnemyData : ScriptableObject
    {
        [Header("Health")]
        [Min(1)]
        public int maxHealth = 5;

        [Header("Movement")]
        [Min(0f)]
        public float moveSpeed = 2.5f;

        [Min(0f)]
        public float patrolRadius = 1.5f;

        [Header("Detection")]
        [Min(0f)]
        public float detectionRadius = 8f;
    }
}