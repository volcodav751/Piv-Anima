using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float speed = 6f;

    [SerializeField, Min(0.1f)]
    private float lifetime = 5f;

    [SerializeField, Min(1)]
    private int damage = 5;

    [Tooltip("Радіус перевірки попадання. Підбери під розмір кулі.")]
    [SerializeField, Min(0.001f)]
    private float hitRadius = 0.05f;

    [Tooltip("Вибери Player і шари стін. Не включай Enemy та Water.")]
    [SerializeField]
    private LayerMask hitMask;

    private readonly List<RaycastHit2D> hits =
        new List<RaycastHit2D>();

    private Vector2 direction;
    private GameObject owner;
    private bool active;

    public void Initialize(
        Vector2 shootDirection,
        GameObject source = null)
    {
        direction = shootDirection.normalized;
        owner = source;
        active = true;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Рух і попадання обробляє цей скрипт.
        // Додаткова фізична симуляція кулі не потрібна.
        Rigidbody2D body = GetComponent<Rigidbody2D>();

        if (body != null)
            body.simulated = false;

        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        if (hitMask.value == 0)
        {
            Debug.LogWarning(
                "BossProjectile: вибери Player і стіни в Hit Mask.",
                this
            );
        }

        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    private void Update()
    {
        if (!active)
            return;

        float distance = speed * Time.deltaTime;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true
        };

        filter.SetLayerMask(hitMask);

        // Перевіряємо весь шлях кулі за кадр,
        // щоб швидка куля не пролетіла крізь тонку стіну.
        Physics2D.CircleCast(
            transform.position,
            hitRadius,
            direction,
            filter,
            hits,
            distance
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (owner != null &&
                hit.collider.transform.IsChildOf(owner.transform))
            {
                continue;
            }

            IDamageable target =
                hit.collider.GetComponentInParent<IDamageable>();

            if (hit.collider.isTrigger && target == null)
                continue;

            if (target != null && target.IsAlive)
            {
                target.TakeDamage(
                    new DamageInfo(damage, direction, owner)
                );
            }

            active = false;
            Destroy(gameObject);
            return;
        }

        transform.position +=
            (Vector3)(direction * distance);
    }
}