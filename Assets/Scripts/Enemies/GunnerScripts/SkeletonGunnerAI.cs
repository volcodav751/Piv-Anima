using System.Collections;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    [RequireComponent(
        typeof(Rigidbody2D),
        typeof(Health),
        typeof(EnemyPathfinder2D))]
    [DisallowMultipleComponent]
    public class SkeletonGunnerAI : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Chase,
            Attack,
            Dead
        }

        [Header("Config")]
        [SerializeField]
        private RangedEnemyData data;

        [Header("References")]
        [SerializeField]
        private EnemyWeapon weapon;

        [SerializeField]
        private Transform aimPivot;

        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private SpriteRenderer gunRenderer;

        [Header("Target")]
        [SerializeField]
        private Transform target;

        [SerializeField]
        private string playerTag = "Player";

        private const float TargetSearchInterval = 0.5f;

        private const float AimTurnSpeed = 540f;
        private const float AimTolerance = 12f;

        private const float RetreatDistanceFactor = 0.6f;
        private const float LoseTargetDistanceFactor = 1.25f;

        private const float PatrolSpeedFactor = 0.5f;
        private const float PatrolStopDistance = 0.1f;
        private const float PatrolWaitMin = 1f;
        private const float PatrolWaitMax = 2.5f;

        private const float DeathDuration = 0.4f;
        private const float DeathSpinSpeed = 720f;

        private Rigidbody2D rigidBody;
        private Health health;
        private EnemyPathfinder2D pathfinder;

        private Collider2D[] colliders;
        private IDamageable targetHealth;

        private State currentState =
            State.Idle;

        private Vector2 desiredVelocity;
        private Vector2 homePosition;
        private Vector2 patrolPoint;

        private float aimAngle;
        private float nextTargetSearchTime;
        private float patrolWaitUntil;

        private bool waitingAtPatrolPoint;

        private int waterLayer;
        private int enemyLayer;

        private void Awake()
        {
            rigidBody =
                GetComponent<Rigidbody2D>();

            health =
                GetComponent<Health>();

            pathfinder =
                GetComponent<EnemyPathfinder2D>();

            colliders =
                GetComponentsInChildren<Collider2D>();

            if (weapon == null)
            {
                weapon =
                    GetComponentInChildren<EnemyWeapon>();
            }

            rigidBody.gravityScale = 0f;
            rigidBody.freezeRotation = true;

            waterLayer =
                LayerMask.NameToLayer("Water");

            enemyLayer =
                LayerMask.NameToLayer("Enemy");

            if (data == null)
            {
                enabled = false;
                return;
            }

            health.SetMaxHealth(
                data.maxHealth
            );

            if (weapon != null)
            {
                weapon.Init(
                    data,
                    gameObject
                );
            }

            if (aimPivot != null)
            {
                aimAngle =
                    aimPivot.eulerAngles.z;
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Start()
        {
            homePosition =
                rigidBody.position;

            FindTarget();
            StartPatrolWait();
        }

        private void Update()
        {
            if (currentState == State.Dead)
            {
                return;
            }

            UpdateTarget();

            switch (currentState)
            {
                case State.Idle:
                    UpdateIdle();
                    break;

                case State.Chase:
                    UpdateChase();
                    break;

                case State.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (currentState == State.Dead)
            {
                return;
            }

            rigidBody.linearVelocity =
                desiredVelocity;
        }

        private void UpdateIdle()
        {
            if (CanDetectTarget())
            {
                SetState(State.Chase);
                return;
            }

            UpdatePatrol();
        }

        private void UpdateChase()
        {
            if (!HasValidTarget())
            {
                SetState(State.Idle);
                return;
            }

            Vector2 toTarget =
                (Vector2)target.position -
                rigidBody.position;

            float distance =
                toTarget.magnitude;

            if (distance >
                data.detectionRadius *
                LoseTargetDistanceFactor)
            {
                SetState(State.Idle);
                return;
            }

            bool hasLineOfSight =
                HasLineOfSight();

            float retreatDistance =
                data.preferredDistance *
                RetreatDistanceFactor;

            if (hasLineOfSight)
            {
                RotateAim(toTarget);
            }

            if (hasLineOfSight &&
                distance < retreatDistance)
            {
                Vector2 retreatTarget =
                    rigidBody.position -
                    toTarget.normalized *
                    data.preferredDistance;

                Vector2 retreatDirection =
                    pathfinder.GetDirection(
                        rigidBody.position,
                        retreatTarget
                    );

                desiredVelocity =
                    retreatDirection *
                    data.moveSpeed;

                return;
            }

            if (hasLineOfSight &&
                distance <=
                data.preferredDistance)
            {
                SetState(State.Attack);
                return;
            }

            Vector2 moveDirection =
                pathfinder.GetDirection(
                    rigidBody.position,
                    target.position
                );

            desiredVelocity =
                moveDirection *
                data.moveSpeed;

            if (!hasLineOfSight &&
                moveDirection != Vector2.zero)
            {
                RotateAim(moveDirection);
            }
        }

        private void UpdateAttack()
        {
            desiredVelocity =
                Vector2.zero;

            if (!HasValidTarget())
            {
                SetState(State.Idle);
                return;
            }

            Vector2 toTarget =
                (Vector2)target.position -
                rigidBody.position;

            float distance =
                toTarget.magnitude;

            float retreatDistance =
                data.preferredDistance *
                RetreatDistanceFactor;

            if (!HasLineOfSight() ||
                distance >
                data.preferredDistance ||
                distance <
                retreatDistance)
            {
                SetState(State.Chase);
                return;
            }

            RotateAim(toTarget);

            if (weapon != null &&
                IsAimedAt(toTarget))
            {
                weapon.TryStartBurst();
            }
        }

        private void UpdatePatrol()
        {
            if (data.patrolRadius <= 0f)
            {
                desiredVelocity =
                    Vector2.zero;

                return;
            }

            if (waitingAtPatrolPoint)
            {
                desiredVelocity =
                    Vector2.zero;

                if (Time.time >=
                    patrolWaitUntil)
                {
                    ChoosePatrolPoint();
                }

                return;
            }

            Vector2 toPatrolPoint =
                patrolPoint -
                rigidBody.position;

            if (toPatrolPoint.magnitude <=
                PatrolStopDistance)
            {
                StartPatrolWait();
                return;
            }

            Vector2 moveDirection =
                pathfinder.GetDirection(
                    rigidBody.position,
                    patrolPoint
                );

            if (moveDirection ==
                Vector2.zero)
            {
                StartPatrolWait();
                return;
            }

            desiredVelocity =
                moveDirection *
                data.moveSpeed *
                PatrolSpeedFactor;

            RotateAim(moveDirection);
        }

        private void ChoosePatrolPoint()
        {
            waitingAtPatrolPoint = false;

            patrolPoint =
                homePosition +
                UnityEngine.Random.insideUnitCircle *
                data.patrolRadius;
        }

        private void StartPatrolWait()
        {
            waitingAtPatrolPoint =
                true;

            patrolWaitUntil =
                Time.time +
                UnityEngine.Random.Range(
                    PatrolWaitMin,
                    PatrolWaitMax
                );
        }

        private void SetState(State newState)
        {
            if (currentState ==
                State.Dead)
            {
                return;
            }

            currentState =
                newState;

            if (newState ==
                    State.Idle ||
                newState ==
                    State.Attack)
            {
                desiredVelocity =
                    Vector2.zero;
            }

            if (newState ==
                State.Idle)
            {
                StartPatrolWait();
            }
        }

        private void UpdateTarget()
        {
            if (HasValidTarget())
            {
                return;
            }

            if (Time.time <
                nextTargetSearchTime)
            {
                return;
            }

            nextTargetSearchTime =
                Time.time +
                TargetSearchInterval;

            FindTarget();
        }

        private void FindTarget()
        {
            if (target == null)
            {
                GameObject player = null;

                try
                {
                    player =
                        GameObject.FindWithTag(
                            playerTag
                        );
                }
                catch (UnityException)
                {
                    return;
                }

                if (player == null)
                {
                    return;
                }

                target =
                    player.transform;
            }

            targetHealth =
                target.GetComponentInParent
                    <IDamageable>();
        }

        private bool HasValidTarget()
        {
            return
                target != null &&
                target.gameObject
                    .activeInHierarchy &&
                (targetHealth == null ||
                 targetHealth.IsAlive);
        }

        private bool CanDetectTarget()
        {
            if (!HasValidTarget())
            {
                return false;
            }

            Vector2 difference =
                (Vector2)target.position -
                rigidBody.position;

            float detectionRadiusSquared =
                data.detectionRadius *
                data.detectionRadius;

            return
                difference.sqrMagnitude <=
                detectionRadiusSquared;
        }

        private bool HasLineOfSight()
        {
            if (target == null)
            {
                return false;
            }

            Vector2 origin =
                aimPivot != null
                    ? aimPivot.position
                    : rigidBody.position;

            RaycastHit2D[] hits =
                Physics2D.LinecastAll(
                    origin,
                    target.position
                );

            foreach (RaycastHit2D hit in hits)
            {
                Collider2D collider =
                    hit.collider;

                if (collider == null ||
                    collider.isTrigger)
                {
                    continue;
                }

                if (collider.transform ==
                        transform ||
                    collider.transform
                        .IsChildOf(transform))
                {
                    continue;
                }

                if (collider.transform ==
                        target ||
                    collider.transform
                        .IsChildOf(target))
                {
                    continue;
                }

                int layer =
                    collider.gameObject.layer;

                if (layer == waterLayer ||
                    layer == enemyLayer)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void RotateAim(
            Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0f)
            {
                return;
            }

            float targetAngle =
                Mathf.Atan2(
                    direction.y,
                    direction.x
                ) * Mathf.Rad2Deg;

            aimAngle =
                Mathf.MoveTowardsAngle(
                    aimAngle,
                    targetAngle,
                    AimTurnSpeed *
                    Time.deltaTime
                );

            if (aimPivot != null)
            {
                aimPivot.rotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        aimAngle
                    );
            }

            bool facingLeft =
                Mathf.Cos(
                    aimAngle *
                    Mathf.Deg2Rad
                ) < 0f;

            if (bodyRenderer != null)
            {
                bodyRenderer.flipX =
                    facingLeft;
            }

            if (gunRenderer != null)
            {
                gunRenderer.flipY =
                    facingLeft;
            }
        }

        private bool IsAimedAt(
            Vector2 direction)
        {
            float angle =
                aimAngle *
                Mathf.Deg2Rad;

            Vector2 aimDirection =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );

            return
                Vector2.Angle(
                    aimDirection,
                    direction
                ) <= AimTolerance;
        }

        private void OnDamaged(
            DamageInfo damage)
        {
            if (currentState ==
                    State.Idle &&
                HasValidTarget())
            {
                SetState(State.Chase);
            }
        }

        private void OnDied()
        {
            currentState =
                State.Dead;

            if (weapon != null)
            {
                weapon.StopFiring();
            }

            desiredVelocity =
                Vector2.zero;

            rigidBody.linearVelocity =
                Vector2.zero;

            rigidBody.simulated =
                false;

            foreach (
                Collider2D collider
                in colliders)
            {
                if (collider != null)
                {
                    collider.enabled =
                        false;
                }
            }

            StartCoroutine(
                DeathRoutine()
            );
        }

        private IEnumerator DeathRoutine()
        {
            Vector3 startScale =
                transform.localScale;

            float elapsedTime = 0f;

            while (elapsedTime <
                   DeathDuration)
            {
                elapsedTime +=
                    Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime /
                        DeathDuration
                    );

                transform.localScale =
                    startScale *
                    (1f - progress);

                transform.Rotate(
                    0f,
                    0f,
                    DeathSpinSpeed *
                    Time.deltaTime
                );

                yield return null;
            }

            Destroy(gameObject);
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (data == null)
            {
                return;
            }

            UnityEditor.Handles.DrawWireDisc(
                transform.position,
                Vector3.forward,
                data.detectionRadius
            );

            UnityEditor.Handles.DrawWireDisc(
                transform.position,
                Vector3.forward,
                data.preferredDistance
            );
        }

#endif
    }
}