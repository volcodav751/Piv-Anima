using System;
using UnityEngine;

namespace Game.Combat
{
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable, IHealth
    {
        [SerializeField, Min(1)]
        private int maxHealth = 5;

        [SerializeField, Min(0f)]
        private float invulnerabilityDuration = 0f;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;

        public bool IsInvulnerable =>
            damageBlocked || Time.time < _invulnerableUntil;

        public float Normalized => (float)Current / maxHealth;

        public event Action<DamageInfo> Damaged;
        public event Action<int, int> Changed;
        public event Action Died;

        private float _invulnerableUntil;
        private bool _initialized;
        private bool damageBlocked;

        private void Awake()
        {
            if (_initialized)
            {
                return;
            }

            Current = maxHealth;
            _initialized = true;
        }

        public void SetMaxHealth(int value, bool refill = true)
        {
            maxHealth = Mathf.Max(1, value);

            Current = refill || !_initialized
                ? maxHealth
                : Mathf.Min(Current, maxHealth);

            _initialized = true;

            Changed?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(DamageInfo damage)
        {
            ApplyDamage(damage, false);
        }

        public void Kill(GameObject source = null)
        {
            ApplyDamage(
                new DamageInfo(Current, Vector2.zero, source),
                true
            );
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            Current = Mathf.Min(
                maxHealth,
                Current + amount
            );

            Changed?.Invoke(Current, maxHealth);
        }

        public void SetDamageBlocked(bool blocked)
        {
            damageBlocked = blocked;
        }

        private void ApplyDamage(
            DamageInfo damage,
            bool ignoreInvulnerability)
        {
            if (!IsAlive || damage.Amount <= 0)
            {
                return;
            }

            if (!ignoreInvulnerability && IsInvulnerable)
            {
                return;
            }

            Current = Mathf.Max(
                0,
                Current - damage.Amount
            );

            if (invulnerabilityDuration > 0f)
            {
                _invulnerableUntil =
                    Time.time + invulnerabilityDuration;
            }

            Damaged?.Invoke(damage);
            Changed?.Invoke(Current, maxHealth);

            if (Current == 0)
            {
                Died?.Invoke();
            }
        }
    }
}