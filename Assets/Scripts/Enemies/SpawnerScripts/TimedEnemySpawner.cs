using System;
using System.Collections;
using System.Collections.Generic;
using Game.Combat;
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

    [Serializable]
    public class EnemyWave
    {
        [Header("Wave Settings")]
        [SerializeField, Min(1)]
        private int enemyCount = 5;

        [SerializeField, Min(0.1f)]
        private float spawnInterval = 2f;

        [SerializeField, Min(1)]
        private int enemiesPerSpawn = 1;

        [Header("Enemies")]
        [SerializeField]
        private EnemySpawnOption[] enemyOptions;

        public int EnemyCount =>
            enemyCount;

        public float SpawnInterval =>
            spawnInterval;

        public int EnemiesPerSpawn =>
            enemiesPerSpawn;

        public EnemySpawnOption[] EnemyOptions =>
            enemyOptions;
    }

    public class TimedEnemySpawner : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField]
        private bool spawnOnStart = true;

        [SerializeField, Min(0f)]
        private float firstWaveDelay = 2f;

        [Header("Waves")]
        [SerializeField]
        private EnemyWave[] waves;

        [Header("Spawn Points")]
        [SerializeField]
        private EnemySpawnPoint[] spawnPoints;

        private Coroutine spawnRoutine;

        private int aliveEnemyCount;
        private int pendingSpawnCount;

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

            if (!HasValidSetup())
            {
                return;
            }

            spawnRoutine =
                StartCoroutine(
                    WavesRoutine()
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

        private IEnumerator WavesRoutine()
        {
            if (firstWaveDelay > 0f)
            {
                yield return new WaitForSeconds(
                    firstWaveDelay
                );
            }

            for (int waveIndex = 0;
                 waveIndex < waves.Length;
                 waveIndex++)
            {
                EnemyWave wave =
                    waves[waveIndex];

                yield return SpawnWave(
                    wave,
                    waveIndex
                );

                yield return new WaitUntil(
                    () =>
                        pendingSpawnCount == 0 &&
                        aliveEnemyCount == 0
                );

                Debug.Log(
                    $"Wave {waveIndex + 1} completed."
                );
            }

            Debug.Log(
                "All enemy waves completed."
            );

            spawnRoutine = null;
        }

        private IEnumerator SpawnWave(
            EnemyWave wave,
            int waveIndex)
        {
            Debug.Log(
                $"Wave {waveIndex + 1} started."
            );

            int spawnedEnemyCount = 0;

            while (spawnedEnemyCount <
                   wave.EnemyCount)
            {
                int remainingEnemyCount =
                    wave.EnemyCount -
                    spawnedEnemyCount;

                int batchSize =
                    Mathf.Min(
                        wave.EnemiesPerSpawn,
                        remainingEnemyCount
                    );

                SpawnBatch(
                    wave,
                    batchSize
                );

                spawnedEnemyCount +=
                    batchSize;

                if (spawnedEnemyCount <
                    wave.EnemyCount)
                {
                    yield return
                        new WaitForSeconds(
                            wave.SpawnInterval
                        );
                }
            }
        }

        private void SpawnBatch(
            EnemyWave wave,
            int batchSize)
        {
            List<EnemySpawnPoint>
                availableSpawnPoints =
                    GetValidSpawnPoints();

            for (int i = 0;
                 i < batchSize;
                 i++)
            {
                GameObject enemyPrefab =
                    GetRandomEnemy(
                        wave.EnemyOptions
                    );

                if (enemyPrefab == null)
                {
                    Debug.LogWarning(
                        $"{name}: wave has no valid enemy prefabs.",
                        this
                    );

                    continue;
                }

                if (availableSpawnPoints.Count == 0)
                {
                    availableSpawnPoints =
                        GetValidSpawnPoints();
                }

                if (availableSpawnPoints.Count == 0)
                {
                    return;
                }

                int randomPointIndex =
                    UnityEngine.Random.Range(
                        0,
                        availableSpawnPoints.Count
                    );

                EnemySpawnPoint spawnPoint =
                    availableSpawnPoints[
                        randomPointIndex
                    ];

                availableSpawnPoints.RemoveAt(
                    randomPointIndex
                );

                pendingSpawnCount++;

                spawnPoint.Spawn(
                    enemyPrefab,
                    OnEnemySpawned
                );
            }
        }

        private void OnEnemySpawned(
            GameObject enemy)
        {
            pendingSpawnCount =
                Mathf.Max(
                    0,
                    pendingSpawnCount - 1
                );

            if (enemy == null)
            {
                return;
            }

            Health health =
                enemy.GetComponent<Health>();

            if (health == null)
            {
                Debug.LogWarning(
                    $"{enemy.name} has no Health component.",
                    enemy
                );

                return;
            }

            aliveEnemyCount++;

            health.Died += OnEnemyDied;
        }

        private void OnEnemyDied()
        {
            aliveEnemyCount =
                Mathf.Max(
                    0,
                    aliveEnemyCount - 1
                );
        }

        private GameObject GetRandomEnemy(
            EnemySpawnOption[] enemyOptions)
        {
            if (enemyOptions == null ||
                enemyOptions.Length == 0)
            {
                return null;
            }

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

            if (spawnPoints == null)
            {
                return result;
            }

            foreach (EnemySpawnPoint point
                     in spawnPoints)
            {
                if (point != null &&
                    point.gameObject.activeInHierarchy)
                {
                    result.Add(point);
                }
            }

            return result;
        }

        private bool HasValidSetup()
        {
            if (waves == null ||
                waves.Length == 0)
            {
                Debug.LogError(
                    $"{name}: no waves are configured.",
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