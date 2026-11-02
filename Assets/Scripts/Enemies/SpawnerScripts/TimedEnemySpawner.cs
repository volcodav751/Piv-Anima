using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies.Spawning
{
    [Serializable]
    public class EnemySpawnOption
    {
        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField, Range(0f, 100f)]
        private float spawnChance = 1f;

        public GameObject EnemyPrefab =>
            enemyPrefab;

        public float SpawnChance =>
            spawnChance;
    }

    public class TimedEnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField, Min(1)]
        private int totalEnemyCount = 10;

        [SerializeField, Min(0.1f)]
        private float spawnInterval = 3f;

        [SerializeField, Min(1)]
        private int enemiesPerSpawn = 1;

        [Header("Enemies")]
        [SerializeField]
        private EnemySpawnOption[] enemyOptions;

        [Header("Spawn Points")]
        [SerializeField]
        private EnemySpawnPoint[] spawnPoints;

        private Coroutine spawnRoutine;
        private int spawnedEnemyCount;

        private void Start()
        {
            if (spawnOnStart)
            {
                StartSpawning();
            }
        }

        public void StartSpawning()
        {
            if (spawnRoutine != null)
            {
                return;
            }

            if (spawnedEnemyCount >=
                totalEnemyCount)
            {
                return;
            }

            if (!HasValidSetup())
            {
                return;
            }

            spawnRoutine =
                StartCoroutine(
                    SpawnRoutine()
                );
        }

        public void StopSpawning()
        {
            if (spawnRoutine == null)
            {
                return;
            }

            StopCoroutine(spawnRoutine);

            spawnRoutine = null;
        }

        private IEnumerator SpawnRoutine()
        {
            while (spawnedEnemyCount <
                   totalEnemyCount)
            {
                SpawnBatch();

                if (spawnedEnemyCount >=
                    totalEnemyCount)
                {
                    break;
                }

                yield return new WaitForSeconds(
                    spawnInterval
                );
            }

            spawnRoutine = null;
        }

        private void SpawnBatch()
        {
            int remainingEnemyCount =
                totalEnemyCount -
                spawnedEnemyCount;

            int batchSize =
                Mathf.Min(
                    enemiesPerSpawn,
                    remainingEnemyCount
                );

            List<EnemySpawnPoint>
                availableSpawnPoints =
                    GetValidSpawnPoints();

            for (int i = 0;
                 i < batchSize;
                 i++)
            {
                GameObject enemyPrefab =
                    GetRandomEnemy();

                if (enemyPrefab == null)
                {
                    return;
                }

                if (availableSpawnPoints.Count == 0)
                {
                    availableSpawnPoints =
                        GetValidSpawnPoints();
                }

                int spawnPointIndex =
                    UnityEngine.Random.Range(
                        0,
                        availableSpawnPoints.Count
                    );

                EnemySpawnPoint spawnPoint =
                    availableSpawnPoints[
                        spawnPointIndex
                    ];

                availableSpawnPoints.RemoveAt(
                    spawnPointIndex
                );

                spawnPoint.Spawn(
                    enemyPrefab
                );

                spawnedEnemyCount++;
            }
        }

        private GameObject GetRandomEnemy()
        {
            float totalChance = 0f;
            GameObject fallbackEnemy = null;

            foreach (EnemySpawnOption option
                     in enemyOptions)
            {
                if (option == null ||
                    option.EnemyPrefab == null ||
                    option.SpawnChance <= 0f)
                {
                    continue;
                }

                totalChance +=
                    option.SpawnChance;

                fallbackEnemy =
                    option.EnemyPrefab;
            }

            if (totalChance <= 0f)
            {
                return null;
            }

            float randomValue =
                UnityEngine.Random.Range(
                    0f,
                    totalChance
                );

            foreach (EnemySpawnOption option
                     in enemyOptions)
            {
                if (option == null ||
                    option.EnemyPrefab == null ||
                    option.SpawnChance <= 0f)
                {
                    continue;
                }

                randomValue -=
                    option.SpawnChance;

                if (randomValue <= 0f)
                {
                    return option.EnemyPrefab;
                }
            }

            return fallbackEnemy;
        }

        private List<EnemySpawnPoint>
            GetValidSpawnPoints()
        {
            List<EnemySpawnPoint> result =
                new List<EnemySpawnPoint>();

            foreach (EnemySpawnPoint point
                     in spawnPoints)
            {
                if (point != null)
                {
                    result.Add(point);
                }
            }

            return result;
        }

        private bool HasValidSetup()
        {
            if (enemyOptions == null ||
                enemyOptions.Length == 0)
            {
                Debug.LogError(
                    $"{name}: no enemies are assigned.",
                    this
                );

                return false;
            }

            if (spawnPoints == null ||
                spawnPoints.Length == 0)
            {
                Debug.LogError(
                    $"{name}: no spawn points are assigned.",
                    this
                );

                return false;
            }

            return true;
        }
    }
}