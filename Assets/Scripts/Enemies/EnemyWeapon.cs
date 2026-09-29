using System;
using System.Collections;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Fires bursts from the muzzle along muzzle's +X axis.
    /// The AI decides WHEN to shoot; the weapon handles burst timing and cooldown.
    /// </summary>
    public class EnemyWeapon : MonoBehaviour
    {
        [Tooltip("Spawn point for bullets. Must be a child of the aim pivot, pointing along +X.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("Layers bullets can hit: Player + walls/obstacles. Do NOT include the Enemy layer.")]
        [SerializeField] private LayerMask hitMask = ~0;

        public bool IsFiring => _burstRoutine != null;
        public bool IsReady => !IsFiring && Time.time >= _nextBurstTime;

        /// <summary>Hook for SFX / muzzle flash / camera shake.</summary>
        public event Action ShotFired;

        private RangedEnemyData _data;
        private GameObject _owner;
        private Coroutine _burstRoutine;
        private float _nextBurstTime;

        public void Init(RangedEnemyData data, GameObject owner)
        {
            _data = data;
            _owner = owner;
            if (muzzle == null)
            {
                muzzle = transform;
                Debug.LogWarning($"{name}: EnemyWeapon has no muzzle assigned, bullets will fly along the root's +X.", this);
            }
        }

        /// <returns>true if a new burst started.</returns>
        public bool TryStartBurst()
        {
            if (_data == null || !IsReady) return false;
            _burstRoutine = StartCoroutine(BurstRoutine());
            return true;
        }

        public void StopFiring()
        {
            if (_burstRoutine != null) StopCoroutine(_burstRoutine);
            _burstRoutine = null;
        }

        private void OnDisable() => _burstRoutine = null; // coroutines die with the component anyway

        private IEnumerator BurstRoutine()
        {
            int count = _data.bulletsPerBurst;
            for (int i = 0; i < count; i++)
            {
                FireOne();
                if (i < count - 1)
                    yield return new WaitForSeconds(_data.timeBetweenShots);
            }

            _nextBurstTime = Time.time + _data.burstCooldown;
            _burstRoutine = null;
        }

        private void FireOne()
        {
            if (_data.projectilePrefab == null)
            {
                Debug.LogWarning($"{name}: projectilePrefab is not set in {_data.name}.", this);
                return;
            }

            float angle = muzzle.eulerAngles.z + UnityEngine.Random.Range(-0.5f, 0.5f) * _data.spreadDeg;
            float rad = angle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            Projectile bullet = ProjectilePool.Spawn(_data.projectilePrefab, muzzle.position, angle);
            bullet.Launch(dir * _data.projectileSpeed, _data.projectileDamage, _data.projectileLifetime, hitMask, _owner);

            ShotFired?.Invoke();
        }
    }
}
