using System;
using Game.Combat;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Enemies
{
    /// <summary>
    /// Minimal room spawner: spawns one enemy per spawn point and reports when all are dead.
    /// The room/door system can subscribe to AllEnemiesDead (code) or onAllEnemiesDead (inspector).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [Tooltip("Empty = spawn one enemy at the spawner's own position.")]
        [SerializeField] private Transform[] spawnPoints;

        [Tooltip("Spawn when the scene starts. Turn off if the room calls SpawnAll() itself.")]
        [SerializeField] private bool spawnOnStart = true;
        [Tooltip("Spawn when the player enters this object's trigger collider.")]
        [SerializeField] private bool spawnOnPlayerEnter;
        [SerializeField] private string playerTag = "Player";

        [SerializeField] private UnityEvent onAllEnemiesDead;

        public int AliveCount { get; private set; }
        public event Action AllEnemiesDead;

        private bool _spawned;

        private void Start()
        {
            if (spawnOnStart) SpawnAll();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!spawnOnPlayerEnter || _spawned) return;
            GameObject root = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (root.CompareTag(playerTag)) SpawnAll();
        }

        public void SpawnAll()
        {
            if (_spawned) return;
            _spawned = true;

            if (enemyPrefab == null)
            {
                Debug.LogError($"{name}: enemyPrefab is not assigned.", this);
                return;
            }

            Transform[] points = spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints : new[] { transform };
            foreach (var point in points)
            {
                if (point == null) continue;
                GameObject enemy = Instantiate(enemyPrefab, point.position, Quaternion.identity, transform);
                if (enemy.TryGetComponent(out Health health))
                {
                    AliveCount++;
                    health.Died += OnEnemyDied;
                }
            }
        }

        private void OnEnemyDied()
        {
            AliveCount--;
            if (AliveCount > 0) return;
            AllEnemiesDead?.Invoke();
            onAllEnemiesDead?.Invoke();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
                return;
            }
            foreach (var p in spawnPoints)
                if (p != null) Gizmos.DrawWireCube(p.position, Vector3.one * 0.8f);
        }
    }
}
