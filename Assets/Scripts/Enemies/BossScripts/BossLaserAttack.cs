using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

[DisallowMultipleComponent]
public class BossLaserAttack : MonoBehaviour
{
    [Header("Час")]
    [Tooltip("Тривалість звичайної стрільби між лазерними атаками.")]
    [SerializeField, Min(0.1f)]
    private float laserInterval = 8f;

    [Tooltip("Скільки часу показується попередження перед пострілом.")]
    [SerializeField, Min(0.1f)]
    private float chargeDuration = 1.2f;

    [Tooltip("Скільки часу видно спалах після пострілу.")]
    [SerializeField, Min(0.02f)]
    private float beamDuration = 0.25f;

    [Tooltip("Пауза після лазера перед поверненням до звичайної стрільби.")]
    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.5f;

    [Header("Характеристики лазера")]
    [SerializeField, Min(1)]
    private int damage = 25;

    [SerializeField, Min(0.1f)]
    private float range = 15f;

    [SerializeField, Min(0.01f)]
    private float beamWidth = 0.2f;

    [Tooltip("Вибери Player і шари стін. Не включай Enemy та Water.")]
    [SerializeField]
    private LayerMask hitMask;

    [Header("Відображення")]
    [SerializeField]
    private LineRenderer beam;

    [SerializeField]
    private Color warningColor = new Color(1f, 0.3f, 0.1f, 0.5f);

    [SerializeField]
    private Color beamColor = Color.red;

    [SerializeField, Min(0.001f)]
    private float warningWidth = 0.03f;

    private enum Phase
    {
        Normal,
        Charge,
        Beam,
        Recovery
    }

    private readonly List<RaycastHit2D> hits =
        new List<RaycastHit2D>();

    private Phase phase;
    private float remaining;

    private Vector2 origin;
    private Vector2 direction;

    public bool IsBusy => phase != Phase.Normal;

    private void Awake()
    {
        if (beam == null)
            beam = GetComponentInChildren<LineRenderer>();

        if (beam != null)
        {
            beam.useWorldSpace = true;
            beam.positionCount = 2;
        }
        else
        {
            Debug.LogWarning(
                "BossLaserAttack: признач LineRenderer у поле Beam.",
                this
            );
        }

        if (hitMask.value == 0)
        {
            Debug.LogWarning(
                "BossLaserAttack: вибери Player і стіни в Hit Mask.",
                this
            );
        }
    }

    private void OnEnable()
    {
        ResetAttack();
    }

    private void OnDisable()
    {
        ResetAttack();
    }

    public void ResetAttack()
    {
        phase = Phase.Normal;
        remaining = laserInterval;

        if (beam != null)
            beam.enabled = false;
    }

    // Цей метод викликає BossController.
    public void Tick(Transform target, Transform muzzle)
    {
        if (beam == null ||
            target == null ||
            muzzle == null ||
            hitMask.value == 0)
        {
            ResetAttack();
            return;
        }

        remaining -= Time.deltaTime;

        if (phase == Phase.Charge)
            DrawBeam(warningWidth, warningColor, false);

        if (remaining > 0f)
            return;

        switch (phase)
        {
            case Phase.Normal:
                BeginCharge(target, muzzle);
                break;

            case Phase.Charge:
                phase = Phase.Beam;
                remaining = Mathf.Max(0.02f, beamDuration);

                // Шкода завдається лише один раз у момент пострілу.
                DrawBeam(beamWidth, beamColor, true);
                break;

            case Phase.Beam:
                beam.enabled = false;

                phase = Phase.Recovery;
                remaining = recoveryDuration;
                break;

            case Phase.Recovery:
                phase = Phase.Normal;
                remaining = Mathf.Max(0.1f, laserInterval);
                break;
        }
    }

    private void BeginCharge(Transform target, Transform muzzle)
    {
        origin = muzzle.position;
        direction = (Vector2)target.position - origin;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // Фіксуємо напрямок на початку зарядки.
        // Після цього гравець може відійти з лінії пострілу.
        phase = Phase.Charge;
        remaining = Mathf.Max(0.1f, chargeDuration);

        DrawBeam(warningWidth, warningColor, false);
    }

    private void DrawBeam(float width, Color color, bool dealDamage)
    {
        Vector2 end = origin + direction * range;

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = true
        };

        filter.SetLayerMask(hitMask);

        // Перевіряємо попадання з урахуванням товщини лазера.
        // Для попередження використовується та сама область перевірки.
        Physics2D.CircleCast(
            origin,
            beamWidth * 0.5f,
            direction,
            filter,
            hits,
            range
        );

        // Результати йдуть від найближчого до найдальшого.
        foreach (RaycastHit2D hit in hits)
        {
            // Не влучаємо у власні колайдери.
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            IDamageable target =
                hit.collider.GetComponentInParent<IDamageable>();

            // Пропускаємо службові тригери кімнат тощо.
            if (hit.collider.isTrigger && target == null)
                continue;

            end = origin + direction * hit.distance;

            if (dealDamage && target != null && target.IsAlive)
            {
                target.TakeDamage(
                    new DamageInfo(damage, direction, gameObject)
                );
            }

            // Промінь зупиняється на першому гравці або стіні.
            break;
        }

        beam.enabled = true;
        beam.startWidth = width;
        beam.endWidth = width;

        beam.startColor = color;
        beam.endColor = color;

        beam.SetPosition(
            0,
            new Vector3(origin.x, origin.y, transform.position.z)
        );

        beam.SetPosition(
            1,
            new Vector3(end.x, end.y, transform.position.z)
        );
    }
}