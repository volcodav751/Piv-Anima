using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Скелет-камикадзе: быстро бежит к игроку, подбегает — запал — взрыв (урон + откидывание).
    /// Если его убили до взрыва — просто умирает, без взрыва.
    /// Все цифры — в KamikazeEnemyData.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemyPathfinder2D))]
    public class KamikazeSkeleton : MonoBehaviour
    {
        [SerializeField] private KamikazeEnemyData data;

        [Header("Ссылки")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform target;
        [SerializeField] private string playerTag = "Player";

        [Header("Кого задевает взрыв")]
        [SerializeField] private LayerMask hitMask;

        [Header("Запал")]
        [SerializeField] private Color fuseColor = Color.white;

        private enum State { Idle, Chase, Fuse, Dead }
        private State state = State.Idle;

        private Rigidbody2D rb;
        private Health health;
        private EnemyPathfinder2D pathfinder;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            pathfinder = GetComponent<EnemyPathfinder2D>();
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();

            if (data == null)
            {
                Debug.LogError($"{name}: не назначен KamikazeEnemyData", this);
                enabled = false;
                return;
            }

            health.SetMaxHealth(data.maxHealth);
        }

        private void OnEnable()  => health.Died += Die;
        private void OnDisable() => health.Died -= Die;

        private void Start()
        {
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null) target = p.transform;
            }
        }

        private void FixedUpdate()
        {
            if (state == State.Dead || state == State.Fuse || target == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toTarget = (Vector2)target.position - rb.position;
            float dist = toTarget.magnitude;

            if (dist <= data.triggerRange)
            {
                StartCoroutine(FuseRoutine());
                return;
            }

            if (dist <= data.detectionRadius)
            {
                state = State.Chase;
                Vector2 dir = pathfinder.GetDirection(rb.position, target.position);
                rb.linearVelocity = dir * data.moveSpeed; // dir бывает нулевым, если пути нет

                if (bodyRenderer != null && Mathf.Abs(toTarget.x) > 0.01f)
                    bodyRenderer.flipX = toTarget.x < 0;
            }
            else
            {
                state = State.Idle;
                rb.linearVelocity = Vector2.zero;
            }
        }

        private IEnumerator FuseRoutine()
        {
            state = State.Fuse;
            rb.linearVelocity = Vector2.zero;

            Color baseColor = bodyRenderer != null ? bodyRenderer.color : Color.white;
            float t = 0f;
            while (t < data.fuseTime)
            {
                if (state == State.Dead) yield break;   // убили во время запала — без взрыва
                if (bodyRenderer != null)
                    bodyRenderer.color = Mathf.PingPong(t * 20f, 1f) > 0.5f ? fuseColor : baseColor;
                t += Time.deltaTime;
                yield return null;
            }

            Explode();
        }

        private void Explode()
        {
            if (state == State.Dead) return;
            state = State.Dead;

            Vector2 center = transform.position;
            var hits = Physics2D.OverlapCircleAll(center, data.explosionRadius, hitMask);
            var alreadyHit = new HashSet<GameObject>(); // у одного объекта может быть несколько коллайдеров

            foreach (var hit in hits)
            {
                GameObject root = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
                if (root == gameObject || !alreadyHit.Add(root)) continue;

                Vector2 dir = ((Vector2)root.transform.position - center).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;

                if (root.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(new DamageInfo(data.explosionDamage, dir, gameObject));

                if (root.TryGetComponent<KnockbackReceiver>(out var kb))
                    kb.Apply(dir * data.knockbackSpeed, data.knockbackStun);
                else if (hit.attachedRigidbody != null)
                    hit.attachedRigidbody.linearVelocity = dir * data.knockbackSpeed;
            }

            Spawn(data.explosionVfxPrefab);
            Destroy(gameObject);
        }

        // Вызывается через Health.Died — убили до взрыва
        private void Die()
        {
            if (state == State.Dead) return;
            state = State.Dead;
            StopAllCoroutines();
            rb.linearVelocity = Vector2.zero;

            Spawn(data.deathVfxPrefab);
            Destroy(gameObject);
        }

        private void Spawn(GameObject prefab)
        {
            if (prefab != null) Instantiate(prefab, transform.position, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.detectionRadius);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, data.triggerRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.explosionRadius);
        }
    }
}
