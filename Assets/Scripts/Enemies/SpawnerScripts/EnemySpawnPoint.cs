using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Enemies.Spawning
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [Header("Spawn Position")]
        [SerializeField]
        private Transform spawnPosition;

        [Header("Spawn Settings")]
        [SerializeField, Min(0f)]
        private float spawnDelay = 0f;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onSpawnStarted;

        [SerializeField]
        private UnityEvent onSpawnFinished;

        public void Spawn(GameObject enemyPrefab)
        {
            if (enemyPrefab == null)
            {
                return;
            }

            StartCoroutine(
                SpawnRoutine(enemyPrefab)
            );
        }

        private IEnumerator SpawnRoutine(
            GameObject enemyPrefab)
        {
            onSpawnStarted?.Invoke();

            if (spawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    spawnDelay
                );
            }

            Transform point =
                spawnPosition != null
                    ? spawnPosition
                    : transform;

            Instantiate(
                enemyPrefab,
                point.position,
                point.rotation
            );

            onSpawnFinished?.Invoke();
        }
    }
}