using System.Collections;
using Game.Combat;
using Game.Enemies;
using UnityEngine;

namespace Game.Enemies.Thrower
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyPathfinder2D))]
    public class ThrowerSkeleton : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private ThrowerEnemyData data;

        [Header("References")]
        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private Transform throwPoint;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private string playerTag = "Player";

        private const float DeathDuration = 0.4f;
        private const float DeathSpinSpeed = 720f;

        private Rigidbody2D rb;
        private Health health;
        private EnemyPathfinder2D pathfinder;
        private Collider2D[] colliders;

        private IDamageable targetHealth;

        private float nextThrowTime;
        private bool isDead;

        private void Awake()
        {
            rb =
                GetComponent<Rigidbody2D>();

            health =
                GetComponent<Health>();

            pathfinder =
                GetComponent<EnemyPathfinder2D>();

            colliders =
                GetComponentsInChildren<Collider2D>();

            if (bodyRenderer == null)
            {
                bodyRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }

            if (data == null)
            {
                Debug.LogError(
                    $"{name}: ThrowerEnemyData is not assigned.",
                    this
                );

                enabled = false;
                return;
            }

            health.SetMaxHealth(
                data.maxHealth
            );
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void Start()
        {
            FindTarget();
        }

        private void FixedUpdate()
        {
            if (isDead)
            {
                return;
            }

            if (!HasValidTarget())
            {
                FindTarget();

                rb.linearVelocity =
                    Vector2.zero;

                return;
            }

            Vector2 toTarget =
                (Vector2)target.position -
                rb.position;

            float distance =
                toTarget.magnitude;

            if (distance >
                data.detectionRadius)
            {
                rb.linearVelocity =
                    Vector2.zero;

                return;
            }

            UpdateFacing(toTarget);

            TryThrowMolotov(distance);

            if (distance <
                data.retreatDistance)
            {
                MoveAwayFromTarget(
                    toTarget
                );

                return;
            }

            if (distance >
                data.preferredDistance)
            {
                MoveTowardsTarget();

                return;
            }

            rb.linearVelocity =
                Vector2.zero;
        }

        private void MoveTowardsTarget()
        {
            Vector2 direction =
                pathfinder.GetDirection(
                    rb.position,
                    target.position
                );

            if (direction == Vector2.zero)
            {
                direction =
                    ((Vector2)target.position -
                     rb.position).normalized;
            }

            rb.linearVelocity =
                direction *
                data.moveSpeed;
        }

        private void MoveAwayFromTarget(
            Vector2 toTarget)
        {
            Vector2 direction =
                -toTarget.normalized;

            rb.linearVelocity =
                direction *
                data.moveSpeed;
        }

        private void TryThrowMolotov(
            float distance)
        {
            if (distance >
                data.throwRange)
            {
                return;
            }

            if (Time.time <
                nextThrowTime)
            {
                return;
            }

            if (data.molotovPrefab == null)
            {
                Debug.LogWarning(
                    $"{name}: Molotov prefab is not assigned.",
                    this
                );

                return;
            }

            if (target == null)
            {
                return;
            }

            Vector3 spawnPosition =
                throwPoint != null
                    ? throwPoint.position
                    : transform.position;

            MolotovProjectile molotov =
                Instantiate(
                    data.molotovPrefab,
                    spawnPosition,
                    Quaternion.identity
                );

            molotov.Launch(
                target.position
            );

            nextThrowTime =
                Time.time +
                data.throwCooldown;
        }

        private void FindTarget()
        {
            GameObject player =
                GameObject.FindGameObjectWithTag(
                    playerTag
                );

            if (player == null)
            {
                target = null;
                targetHealth = null;
                return;
            }

            target =
                player.transform;

            targetHealth =
                player.GetComponentInParent<IDamageable>();
        }

        private bool HasValidTarget()
        {
            if (target == null)
            {
                return false;
            }

            if (!target.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (targetHealth != null &&
                !targetHealth.IsAlive)
            {
                return false;
            }

            return true;
        }

        private void UpdateFacing(
            Vector2 direction)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            if (Mathf.Abs(direction.x) <
                0.01f)
            {
                return;
            }

            bodyRenderer.flipX =
                direction.x < 0f;
        }

        private void OnDied()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;

            rb.linearVelocity =
                Vector2.zero;

            rb.simulated =
                false;

            foreach (Collider2D collider
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

        private void OnDrawGizmosSelected()
        {
            if (data == null)
            {
                return;
            }

            Gizmos.color =
                Color.red;

            Gizmos.DrawWireSphere(
                transform.position,
                data.retreatDistance
            );

            Gizmos.color =
                Color.green;

            Gizmos.DrawWireSphere(
                transform.position,
                data.preferredDistance
            );

            Gizmos.color =
                Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                data.throwRange
            );

            Gizmos.color =
                Color.white;

            Gizmos.DrawWireSphere(
                transform.position,
                data.detectionRadius
            );
        }
    }
}