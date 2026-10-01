using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Shared health component for the player, enemies and destructibles.
    /// Knows nothing about who owns it: listeners subscribe to Damaged / Died.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 5;

        [Tooltip("I-frames after a hit. 0 for regular enemies, ~0.5-1s for the player.")]
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0f;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsAlive => Current > 0;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public float Normalized => (float)Current / maxHealth;

        /// <summary>Fired on every successful hit (before Died).</summary>
        public event Action<DamageInfo> Damaged;
        /// <summary>(current, max) — for health bars.</summary>
        public event Action<int, int> Changed;
        /// <summary>Fired once when health reaches 0.</summary>
        public event Action Died;

        private float _invulnerableUntil;
        private bool _initialized;

        private void Awake()
        {
            if (_initialized) return;
            Current = maxHealth;
            _initialized = true;
        }

        /// <summary>Called by owners that take their stats from data assets (e.g. EnemyData).</summary>
        public void SetMaxHealth(int value, bool refill = true)
        {
            maxHealth = Mathf.Max(1, value);
            Current = refill || !_initialized ? maxHealth : Mathf.Min(Current, maxHealth);
            _initialized = true;
            Changed?.Invoke(Current, maxHealth);
        }

        public void TakeDamage(DamageInfo damage) => ApplyDamage(damage, ignoreInvulnerability: false);

        public void Kill(GameObject source = null) =>
            ApplyDamage(new DamageInfo(Current, Vector2.zero, source), ignoreInvulnerability: true);

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        private void ApplyDamage(DamageInfo damage, bool ignoreInvulnerability)
        {
            if (!IsAlive || damage.Amount <= 0) return;
            if (!ignoreInvulnerability && IsInvulnerable) return;

            Current = Mathf.Max(0, Current - damage.Amount);
            if (invulnerabilityDuration > 0f)
                _invulnerableUntil = Time.time + invulnerabilityDuration;

            Damaged?.Invoke(damage);
            Changed?.Invoke(Current, maxHealth);

            if (Current == 0)
                Died?.Invoke();
        }

        // Right-click the component header in Play Mode to test without the player's weapon.
        [ContextMenu("Debug/Take 1 Damage")]
        private void DebugTakeDamage() => TakeDamage(new DamageInfo(1));

        [ContextMenu("Debug/Kill")]
        private void DebugKill() => Kill();
    }
}
