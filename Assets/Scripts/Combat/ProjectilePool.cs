using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Combat
{
    /// <summary>
    /// Scene-level pool for all projectiles (one ObjectPool per prefab).
    /// Created lazily on first use, destroyed with the scene. No setup required.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        private static ProjectilePool _instance;

        private readonly Dictionary<Projectile, ObjectPool<Projectile>> _pools = new Dictionary<Projectile, ObjectPool<Projectile>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null; // for "Enter Play Mode" without domain reload

        public static Projectile Spawn(Projectile prefab, Vector3 position, float angleDeg)
        {
            if (_instance == null)
                _instance = new GameObject("[ProjectilePool]").AddComponent<ProjectilePool>();

            return _instance.SpawnInternal(prefab, position, angleDeg);
        }

        private Projectile SpawnInternal(Projectile prefab, Vector3 position, float angleDeg)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new ObjectPool<Projectile>(
                    createFunc: () =>
                    {
                        var p = Instantiate(prefab, transform);
                        p.gameObject.SetActive(false);
                        p.BindToPool(this, prefab);
                        return p;
                    },
                    actionOnGet: null,
                    actionOnRelease: p => p.gameObject.SetActive(false),
                    actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                    collectionCheck: false,
                    defaultCapacity: 32,
                    maxSize: 256);
                _pools.Add(prefab, pool);
            }

            var projectile = pool.Get();
            // Position first, activate second — so the bullet never appears for a frame at its old spot.
            projectile.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDeg));
            projectile.gameObject.SetActive(true);
            return projectile;
        }

        internal void Release(Projectile projectile, Projectile prefabKey)
        {
            if (prefabKey != null && _pools.TryGetValue(prefabKey, out var pool))
                pool.Release(projectile);
            else
                Destroy(projectile.gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
