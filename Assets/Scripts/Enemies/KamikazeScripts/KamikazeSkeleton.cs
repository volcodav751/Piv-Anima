using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyPathfinder2D))]
    public class KamikazeSkeleton : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField]
        private KamikazeEnemyData data;

        [Header("References")]
        [SerializeField]
        private SpriteRenderer bodyRenderer;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private string playerTag = "Player";

        [Header("Explosion")]
        [SerializeField]
        private LayerMask hitMask;

        [Header("Fuse")]
        [SerializeField]
        private Color fuseColor = Color.white;

        private const float DeathDuration = 0.4f;
        private const float DeathSpinSpeed = 720f;

        private enum State
        {
            Idle,
            Chase,
            Fuse,
            Dead
        }

        private State state = State.Idle;

        private Rigidbody2D rb;
        private Health health;
        private EnemyPathfinder2D pathfinder;
        private Collider2D[] colliders;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            pathfinder = GetComponent<EnemyPathfinder2D>();

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
                    $"{name}: KamikazeEnemyData is not assigned.",
                    this
                );

                enabled = false;
                return;
            }

            health.SetMaxHealth(data.maxHealth);
        }

        private void OnEnable()
        {
            health.Died += Die;
        }

        private void OnDisable()
        {
            health.Died -= Die;
        }

        private void Start()
        {
            if (target != null)
            {
                return;
            }

            GameObject player =
                GameObject.FindGameObjectWithTag(
                    playerTag
                );

            if (player != null)
            {
                target = player.transform;
            }
        }

        private void FixedUpdate()
        {
            if (state == State.Dead ||
                state == State.Fuse ||
                target == null)
            {
                rb.linearVelocity =
                    Vector2.zero;

                return;
            }

            Vector2 toTarget =
                (Vector2)target.position -
                rb.position;

            float distance =
                toTarget.magnitude;

            if (distance <= data.triggerRange)
            {
                StartCoroutine(
                    FuseRoutine()
                );

                return;
            }

            if (distance <= data.detectionRadius)
            {
                state = State.Chase;

                Vector2 direction =
                    pathfinder.GetDirection(
                        rb.position,
                        target.position
                    );

                rb.linearVelocity =
                    direction *
                    data.moveSpeed;

                if (bodyRenderer != null &&
                    Mathf.Abs(toTarget.x) > 0.01f)
                {
                    bodyRenderer.flipX =
                        toTarget.x < 0f;
                }

                return;
            }

            state = State.Idle;

            rb.linearVelocity =
                Vector2.zero;
        }

        private IEnumerator FuseRoutine()
        {
            state = State.Fuse;

            rb.linearVelocity =
                Vector2.zero;

            Color baseColor =
                bodyRenderer != null
                    ? bodyRenderer.color
                    : Color.white;

            float elapsedTime = 0f;

            while (elapsedTime < data.fuseTime)
            {
                if (state == State.Dead)
                {
                    yield break;
                }

                if (bodyRenderer != null)
                {
                    bool flash =
                        Mathf.PingPong(
                            elapsedTime * 20f,
                            1f
                        ) > 0.5f;

                    bodyRenderer.color =
                        flash
                            ? fuseColor
                            : baseColor;
                }

                elapsedTime +=
                    Time.deltaTime;

                yield return null;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.color =
                    baseColor;
            }

            Explode();
        }

        private void Explode()
        {
            if (state == State.Dead)
            {
                return;
            }

            state = State.Dead;

            rb.linearVelocity =
                Vector2.zero;

            Vector2 center =
                transform.position;

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    center,
                    data.explosionRadius,
                    hitMask
                );

            HashSet<GameObject> alreadyHit =
                new HashSet<GameObject>();

            foreach (Collider2D hit in hits)
            {
                GameObject root =
                    hit.attachedRigidbody != null
                        ? hit.attachedRigidbody.gameObject
                        : hit.gameObject;

                if (root == gameObject ||
                    !alreadyHit.Add(root))
                {
                    continue;
                }

                Vector2 direction =
                    (Vector2)root.transform.position -
                    center;

                if (direction == Vector2.zero)
                {
                    direction =
                        Random.insideUnitCircle.normalized;
                }
                else
                {
                    direction.Normalize();
                }

                if (root.TryGetComponent<IDamageable>(
                    out var damageable))
                {
                    damageable.TakeDamage(
                        new DamageInfo(
                            data.explosionDamage,
                            direction,
                            gameObject
                        )
                    );
                }

                if (root.TryGetComponent<KnockbackReceiver>(
                    out var knockbackReceiver))
                {
                    knockbackReceiver.Apply(
                        direction *
                        data.knockbackSpeed,
                        data.knockbackStun
                    );
                }
                else if (hit.attachedRigidbody != null)
                {
                    hit.attachedRigidbody
                        .linearVelocity =
                        direction *
                        data.knockbackSpeed;
                }
            }

            Spawn(
                data.explosionVfxPrefab
            );

            StartDeath();
        }

        private void Die()
        {
            if (state == State.Dead)
            {
                return;
            }

            state = State.Dead;

            StopAllCoroutines();

            rb.linearVelocity =
                Vector2.zero;

            Spawn(
                data.deathVfxPrefab
            );

            StartDeath();
        }

        private void StartDeath()
        {
            rb.linearVelocity =
                Vector2.zero;

            rb.simulated = false;

            foreach (Collider2D collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = false;
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

        private void Spawn(
            GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            Instantiate(
                prefab,
                transform.position,
                Quaternion.identity
            );
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null)
            {
                return;
            }

            Gizmos.color =
                Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                data.detectionRadius
            );

            Gizmos.color =
                new Color(
                    1f,
                    0.5f,
                    0f
                );

            Gizmos.DrawWireSphere(
                transform.position,
                data.triggerRange
            );

            Gizmos.color =
                Color.red;

            Gizmos.DrawWireSphere(
                transform.position,
                data.explosionRadius
            );
        }
    }
}