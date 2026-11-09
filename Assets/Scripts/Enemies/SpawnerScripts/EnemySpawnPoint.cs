using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Enemies.Spawning
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [Header("Spawn Positions")]
        [SerializeField]
        private Transform[] spawnPositions;

        [Header("Spawn Settings")]
        [SerializeField, Min(0f)]
        private float spawnDelay = 0f;

        [SerializeField, Min(0f)]
        private float spawnCheckRadius = 0.25f;

        [SerializeField]
        private LayerMask blockedLayers;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onSpawnStarted;

        [SerializeField]
        private UnityEvent onSpawnFinished;

        public void Spawn(
            GameObject enemyPrefab,
            Action<GameObject> onSpawned)
        {
            if (enemyPrefab == null)
            {
                return;
            }

            StartCoroutine(
                SpawnRoutine(
                    enemyPrefab,
                    onSpawned
                )
            );
        }

        private IEnumerator SpawnRoutine(
            GameObject enemyPrefab,
            Action<GameObject> onSpawned)
        {
            onSpawnStarted?.Invoke();

            if (spawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    spawnDelay
                );
            }

            Transform spawnPoint =
                GetSpawnPosition();

            if (spawnPoint == null)
            {
                Debug.LogWarning(
                    $"{name}: no free spawn position found.",
                    this
                );

                yield break;
            }

            GameObject enemy =
                Instantiate(
                    enemyPrefab,
                    spawnPoint.position,
                    spawnPoint.rotation
                );

            onSpawned?.Invoke(enemy);

            onSpawnFinished?.Invoke();
        }

        private Transform GetSpawnPosition()
        {
            if (spawnPositions == null ||
                spawnPositions.Length == 0)
            {
                return transform;
            }

            List<Transform> freePositions =
                new List<Transform>();

            foreach (Transform point in spawnPositions)
            {
                if (point == null)
                {
                    continue;
                }

                bool blocked =
                    Physics2D.OverlapCircle(
                        point.position,
                        spawnCheckRadius,
                        blockedLayers
                    ) != null;

                if (!blocked)
                {
                    freePositions.Add(point);
                }
            }

            if (freePositions.Count == 0)
            {
                return null;
            }

            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    freePositions.Count
                );

            return freePositions[randomIndex];
        }

        private void OnDrawGizmosSelected()
        {
            if (spawnPositions == null)
            {
                return;
            }

            Gizmos.color = Color.green;

            foreach (Transform point in spawnPositions)
            {
                if (point == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(
                    point.position,
                    spawnCheckRadius
                );
            }
        }
    }
}