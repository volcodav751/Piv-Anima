using UnityEngine;

namespace Game.Enemies
{
    [CreateAssetMenu(fileName = "Kamikaze_Data", menuName = "Game/Enemies/Kamikaze Enemy Data")]
    public class KamikazeEnemyData : ScriptableObject
    {
        [Header("Здоровье")]
        [Min(1)] public int maxHealth = 3;

        [Header("Движение")]
        [Min(0f)] public float moveSpeed = 2.8f;          // ганнер ходит на 1
        [Min(0f)] public float detectionRadius = 3.5f;    // ганнер видит на 2.5

        [Header("Взрыв")]
        [Min(0f)] public float triggerRange = 0.25f;      // на каком расстоянии поджигает запал
        [Min(0f)] public float fuseTime = 0.4f;           // 0 = взрыв сразу
        [Min(0f)] public float explosionRadius = 0.6f;
        [Min(1)]  public int explosionDamage = 2;

        [Header("Откидывание")]
        [Min(0f)] public float knockbackSpeed = 5f;
        [Min(0f)] public float knockbackStun = 0.25f;

        [Header("Эффекты")]
        public GameObject explosionVfxPrefab;
        public GameObject deathVfxPrefab;
    }
}
