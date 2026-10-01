using System;
using System.Collections;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Ranged skeleton with an assault rifle.
    /// Spawning -> Idle (patrol) -> Chase (keep distance) -> Attack (bursts) -> Dead.
    /// Top-down 2D: Rigidbody2D with gravity 0.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    [DisallowMultipleComponent]
    public class SkeletonGunnerAI : MonoBehaviour
    {
        public enum State { Spawning, Idle, Chase, Attack, Dead }

        [Header("Config")]
        [SerializeField] private RangedEnemyData data;

        [Header("References")]
        [SerializeField] private EnemyWeapon weapon;
        [Tooltip("Rotated toward the aim direction. Gun and muzzle are its children, pointing along +X.")]
        [SerializeField] private Transform aimPivot;
        [Tooltip("Optional. flipX follows aim direction (for the real sprite later).")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("Optional. flipY when aiming left, so the gun sprite isn't upside down.")]
        [SerializeField] private SpriteRenderer gunRenderer;

        [Header("Targeting")]
        [Tooltip("Leave empty — found by tag at runtime.")]
        [SerializeField] private Transform target;
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Layers that block line of sight (walls, rocks). Empty = enemy sees through everything.")]
        [SerializeField] private LayerMask obstacleMask;

        [Header("Death")]
        [Tooltip("Optional particle/animation prefab spawned on death.")]
        [SerializeField] private GameObject deathVfxPrefab;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        public State CurrentState => _state;
        public RangedEnemyData Data => data;

        /// <summary>Fired once on death (before the death effect finishes).</summary>
        public event Action<SkeletonGunnerAI> Died;

        private const float LostSightGrace = 0.4f;       // how long LOS may be broken before leaving Attack
        private const float TargetSearchInterval = 0.5f;
        private const float StuckSpeed = 0.15f;
        private const float StuckRetreatTime = 0.4f;     // cornered this long while retreating -> just shoot
        private const float RetreatLockout = 1.5f;
        private const float PatrolArriveDistance = 0.1f;
        private const float PatrolMaxLegTime = 3f;

        private Rigidbody2D _rb;
        private Health _health;
        private Collider2D[] _colliders;
        private IDamageable _targetHealth;

        private State _state = State.Spawning;
        private Vector2 _desiredVelocity;
        private float _aimAngle;

        private float _activateAt;
        private float _nextTargetSearch;
        private float _lostSightTimer;
        private float _attackReadyAt;
        private float _retreatStuckTimer;
        private float _ignoreRetreatUntil;

        private Vector2 _home;
        private Vector2 _patrolPoint;
        private bool _patrolWaiting;
        private float _patrolWaitUntil;
        private float _patrolLegStartedAt;

        // ───────────────────────── Lifecycle ─────────────────────────

        private void Reset()
        {
            weapon = GetComponentInChildren<EnemyWeapon>();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _colliders = GetComponentsInChildren<Collider2D>();
            if (weapon == null) weapon = GetComponentInChildren<EnemyWeapon>();

            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            if (data == null)
            {
                Debug.LogError($"{name}: RangedEnemyData is not assigned — AI disabled.", this);
                enabled = false;
                return;
            }

            _health.SetMaxHealth(data.maxHealth);
            if (weapon != null) weapon.Init(data, gameObject);
            _aimAngle = aimPivot != null ? aimPivot.eulerAngles.z : 0f;
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void Start()
        {
            _home = _rb.position;
            _activateAt = Time.time + data.activationDelay;
            TryFindTarget(force: true);
        }

        private void Update()
        {
            if (_state == State.Dead) return;

            if (!HasValidTarget())
            {
                TryFindTarget(force: false);
                if (_state == State.Chase || _state == State.Attack)
                    ChangeState(State.Idle); // player died / disappeared
            }

            switch (_state)
            {
                case State.Spawning: TickSpawning(); break;
                case State.Idle:     TickIdle();     break;
                case State.Chase:    TickChase();    break;
                case State.Attack:   TickAttack();   break;
            }
        }

        private void FixedUpdate()
        {
            if (_state == State.Dead) return;
            // Accelerate toward the desired velocity; knockback decays naturally the same way.
            _rb.linearVelocity = Vector2.MoveTowards(
                _rb.linearVelocity, _desiredVelocity, data.acceleration * Time.fixedDeltaTime);
        }

        // ───────────────────────── States ─────────────────────────

        private void ChangeState(State next)
        {
            if (_state == next || _state == State.Dead) return;
            _state = next;

            switch (next)
            {
                case State.Idle:
                    _desiredVelocity = Vector2.zero;
                    StartPatrolWait();
                    break;
                case State.Chase:
                    _retreatStuckTimer = 0f;
                    break;
                case State.Attack:
                    _desiredVelocity = Vector2.zero;
                    _lostSightTimer = 0f;
                    _attackReadyAt = Time.time + data.firstBurstDelay;
                    break;
            }
        }

        private void TickSpawning()
        {
            _desiredVelocity = Vector2.zero;
            if (Time.time >= _activateAt)
                ChangeState(State.Idle);
        }

        private void TickIdle()
        {
            if (HasValidTarget() && DistanceToTarget() <= data.detectionRadius && HasLineOfSight())
            {
                ChangeState(State.Chase);
                return;
            }

            if (data.patrol) TickPatrol();
            else _desiredVelocity = Vector2.zero;
        }

        private void TickChase()
        {
            Vector2 toTarget = ToTarget();
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.001f ? toTarget / dist : Vector2.right;
            bool los = HasLineOfSight();

            RotateAim(dir);

            // Too close -> back off.
            if (dist < data.retreatDistance)
            {
                _desiredVelocity = -dir * (data.moveSpeed * data.retreatSpeedMultiplier);

                // Pinned against a wall: can't retreat, so fight back instead of freezing.
                bool stuck = _rb.linearVelocity.sqrMagnitude < StuckSpeed * StuckSpeed;
                _retreatStuckTimer = stuck ? _retreatStuckTimer + Time.deltaTime : 0f;
                if (_retreatStuckTimer >= StuckRetreatTime && los)
                {
                    _ignoreRetreatUntil = Time.time + RetreatLockout;
                    ChangeState(State.Attack);
                }
                return;
            }
            _retreatStuckTimer = 0f;

            // Reached firing position.
            if (los && dist <= data.preferredDistance)
            {
                ChangeState(State.Attack);
                return;
            }

            // Too far or no line of sight -> approach. (No pathfinding yet: straight line.)
            _desiredVelocity = dir * data.moveSpeed;
        }

        private void TickAttack()
        {
            _desiredVelocity = Vector2.zero;

            Vector2 toTarget = ToTarget();
            float dist = toTarget.magnitude;
            RotateAim(toTarget);

            bool los = HasLineOfSight();
            _lostSightTimer = los ? 0f : _lostSightTimer + Time.deltaTime;

            bool tooFar = dist > data.attackRange;
            bool lostSight = _lostSightTimer > LostSightGrace;
            bool tooClose = dist < data.retreatDistance && Time.time >= _ignoreRetreatUntil;

            if (tooFar || lostSight || tooClose)
            {
                // A burst in progress is allowed to finish — looks more natural.
                ChangeState(State.Chase);
                return;
            }

            if (weapon != null && los && Time.time >= _attackReadyAt && IsAimedAt(toTarget))
                weapon.TryStartBurst();
        }

        // ───────────────────────── Patrol ─────────────────────────

        private void TickPatrol()
        {
            if (_patrolWaiting)
            {
                _desiredVelocity = Vector2.zero;
                if (Time.time < _patrolWaitUntil) return;

                _patrolWaiting = false;
                _patrolPoint = _home + UnityEngine.Random.insideUnitCircle * data.patrolRadius;
                _patrolLegStartedAt = Time.time;
            }

            Vector2 toPoint = _patrolPoint - _rb.position;
            bool arrived = toPoint.magnitude <= PatrolArriveDistance;
            bool tookTooLong = Time.time - _patrolLegStartedAt > PatrolMaxLegTime; // bumped into something

            if (arrived || tookTooLong)
            {
                StartPatrolWait();
                return;
            }

            _desiredVelocity = toPoint.normalized * (data.moveSpeed * data.patrolSpeedMultiplier);
            RotateAim(toPoint);
        }

        private void StartPatrolWait()
        {
            _patrolWaiting = true;
            _patrolWaitUntil = Time.time + UnityEngine.Random.Range(data.patrolWaitRange.x, data.patrolWaitRange.y);
        }

        // ───────────────────────── Health callbacks ─────────────────────────

        private void OnDamaged(DamageInfo info)
        {
            if (_state == State.Dead) return;

            if (data.knockback > 0f && info.Direction.sqrMagnitude > 0.0001f)
                _rb.linearVelocity += info.Direction.normalized * data.knockback;

            // Got shot while idle -> "heard" the player, aggro immediately.
            if (_state == State.Idle && HasValidTarget())
                ChangeState(State.Chase);
        }

        private void OnDied()
        {
            if (_state == State.Dead) return;
            _state = State.Dead;

            if (weapon != null) weapon.StopFiring();

            _desiredVelocity = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
            foreach (var col in _colliders)
                if (col != null) col.enabled = false;

            if (deathVfxPrefab != null)
                Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);

            Died?.Invoke(this);
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            // Placeholder effect: spin + shrink. Replace with an Animator trigger when there's art.
            float duration = Mathf.Max(0.01f, data.deathDuration);
            Vector3 startScale = transform.localScale;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                transform.localScale = startScale * k;
                transform.Rotate(0f, 0f, 720f * Time.deltaTime);
                yield return null;
            }

            Destroy(gameObject);
        }

        // ───────────────────────── Targeting helpers ─────────────────────────

        private void TryFindTarget(bool force)
        {
            if (!force && Time.time < _nextTargetSearch) return;
            _nextTargetSearch = Time.time + TargetSearchInterval;

            if (target == null)
            {
                GameObject player = null;
                try { player = GameObject.FindWithTag(playerTag); }
                catch (UnityException) { /* tag doesn't exist in project */ }

                if (player == null) return;
                target = player.transform;
            }

            _targetHealth = target.GetComponentInParent<IDamageable>();
        }

        private bool HasValidTarget() =>
            target != null &&
            target.gameObject.activeInHierarchy &&
            (_targetHealth == null || _targetHealth.IsAlive);

        private Vector2 ToTarget() => (Vector2)target.position - _rb.position;

        private float DistanceToTarget() => ToTarget().magnitude;

        private bool HasLineOfSight()
        {
            if (obstacleMask.value == 0) return true;
            Vector2 from = aimPivot != null ? (Vector2)aimPivot.position : _rb.position;
            return !Physics2D.Linecast(from, target.position, obstacleMask);
        }

        // ───────────────────────── Aiming ─────────────────────────

        private Vector2 AimDirection
        {
            get
            {
                float rad = _aimAngle * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }
        }

        private void RotateAim(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;

            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _aimAngle = Mathf.MoveTowardsAngle(_aimAngle, targetAngle, data.turnSpeed * Time.deltaTime);

            if (aimPivot != null) aimPivot.rotation = Quaternion.Euler(0f, 0f, _aimAngle);

            bool facingLeft = AimDirection.x < 0f;
            if (bodyRenderer != null) bodyRenderer.flipX = facingLeft;
            if (gunRenderer != null) gunRenderer.flipY = facingLeft;
        }

        private bool IsAimedAt(Vector2 toTarget) =>
            Vector2.Angle(AimDirection, toTarget) <= data.aimToleranceDeg;

        // ───────────────────────── Gizmos ─────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || data == null) return;
            Vector3 p = transform.position;

            UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.5f);
            UnityEditor.Handles.DrawWireDisc(p, Vector3.forward, data.detectionRadius);
            UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            UnityEditor.Handles.DrawWireDisc(p, Vector3.forward, data.attackRange);
            UnityEditor.Handles.color = new Color(0.3f, 1f, 0.3f, 0.8f);
            UnityEditor.Handles.DrawWireDisc(p, Vector3.forward, data.preferredDistance);
            UnityEditor.Handles.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            UnityEditor.Handles.DrawWireDisc(p, Vector3.forward, data.retreatDistance);

            if (Application.isPlaying)
                UnityEditor.Handles.Label(p + Vector3.up * 0.8f, _state.ToString());
        }
#endif
    }
}
