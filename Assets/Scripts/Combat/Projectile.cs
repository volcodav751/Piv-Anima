using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Simple straight-flying bullet. Spawn it through ProjectilePool.Spawn(), then call Launch().
    /// Needs a trigger Collider2D. Rigidbody2D is forced to Kinematic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody2D _rb;
        private ProjectilePool _pool;
        private Projectile _prefabKey;

        private int _damage;
        private float _despawnAt;
        private LayerMask _hitMask;
        private GameObject _owner;
        private bool _active;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            foreach (var col in GetComponents<Collider2D>())
                col.isTrigger = true;
        }

        internal void BindToPool(ProjectilePool pool, Projectile prefabKey)
        {
            _pool = pool;
            _prefabKey = prefabKey;
        }

        /// <param name="hitMask">Layers this bullet reacts to (targets + walls).</param>
        /// <param name="owner">Shooter's root object: its own colliders are ignored.</param>
        public void Launch(Vector2 velocity, int damage, float lifetime, LayerMask hitMask, GameObject owner)
        {
            _damage = damage;
            _hitMask = hitMask;
            _owner = owner;
            _despawnAt = Time.time + lifetime;
            _active = true;
            _rb.linearVelocity = velocity;
        }

        private void Update()
        {
            if (_active && Time.time >= _despawnAt)
                Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active) return;
            if ((_hitMask.value & (1 << other.gameObject.layer)) == 0) return;

            GameObject otherRoot = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (_owner != null && otherRoot == _owner) return;

            var target = other.GetComponentInParent<IDamageable>();

            // Room triggers, other bullets, etc. — fly through them.
            if (other.isTrigger && target == null) return;

            if (target != null && target.IsAlive)
                target.TakeDamage(new DamageInfo(_damage, _rb.linearVelocity.normalized, _owner));

            Despawn();
        }

        public void Despawn()
        {
            if (!_active) return;
            _active = false;
            _rb.linearVelocity = Vector2.zero;

            if (_pool != null) _pool.Release(this, _prefabKey);
            else Destroy(gameObject);
        }
    }
}
