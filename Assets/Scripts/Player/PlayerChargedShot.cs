using System.Collections.Generic;
using Game.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(
    typeof(PlayerController),
    typeof(PlayerWeapon))]
public class PlayerChargedShot : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private LineRenderer laserLine;

    [Header("Input")]
    [SerializeField]
    private InputActionReference chargedShotAction;

    [Header("Laser Settings")]
    [SerializeField, Min(0.01f)]
    private float laserWidth = 0.3f;

    [SerializeField, Min(0.01f)]
    private float laserDuration = 1.5f;

    [SerializeField, Min(0f)]
    private float laserCooldown = 5f;

    [SerializeField, Min(0.1f)]
    private float laserMaxDistance = 20f;

    [SerializeField]
    private LayerMask wallMask = ~0;

    [SerializeField]
    private LayerMask ignoredLayers;

    [Header("Laser Damage")]
    [SerializeField, Min(1)]
    private int laserDamage = 1;

    [SerializeField, Min(0.01f)]
    private float laserDamageInterval = 0.1f;

    [Header("Player During Laser")]
    [SerializeField, Range(0f, 1f)]
    private float laserMoveSpeedMultiplier = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float laserAimSpeedMultiplier = 0.15f;

    private PlayerController playerController;
    private PlayerWeapon playerWeapon;
    private PlayerDash playerDash;

    private bool isLaserActive;
    private float laserEndTime;
    private float nextLaserTime;
    private float nextLaserDamageTime;

    private KnockbackReceiver knockbackReceiver;

    public bool IsLaserActive =>
        isLaserActive;

    private void Awake()
    {
        playerController =
            GetComponent<PlayerController>();

        playerWeapon =
            GetComponent<PlayerWeapon>();

        playerDash =
            GetComponent<PlayerDash>();

        knockbackReceiver =
            GetComponent<KnockbackReceiver>();

        if (laserLine != null)
        {
            laserLine.positionCount = 2;
            laserLine.useWorldSpace = true;
            laserLine.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (chargedShotAction != null)
        {
            chargedShotAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (chargedShotAction != null)
        {
            chargedShotAction.action.Disable();
        }

        if (isLaserActive)
        {
            StopLaser();
        }
    }

    private void Update()
    {
        if (isLaserActive &&
            knockbackReceiver != null &&
            knockbackReceiver.IsKnockedBack)
        {
            StopLaser();
            return;
        }

        if (chargedShotAction != null &&
            chargedShotAction.action.WasPressedThisFrame())
        {
            TryStartLaser();
        }

        if (!isLaserActive)
        {
            return;
        }

        UpdateLaser();

        if (Time.time >= laserEndTime)
        {
            StopLaser();
        }
    }

    private void TryStartLaser()
    {
        if (knockbackReceiver != null &&
            knockbackReceiver.IsKnockedBack)
        {
            return;
        }

        if (isLaserActive)
        {
            return;
        }

        if (Time.time < nextLaserTime)
        {
            return;
        }

        StartLaser();
    }

    private void StartLaser()
    {
        if (laserLine == null ||
            firePoint == null)
        {
            return;
        }

        isLaserActive = true;

        laserEndTime =
            Time.time + laserDuration;

        nextLaserDamageTime =
            Time.time;

        playerController
            .SetMoveSpeedMultiplier(
                laserMoveSpeedMultiplier
            );

        playerWeapon
            .SetAimSpeedMultiplier(
                laserAimSpeedMultiplier
            );

        playerWeapon
            .SetShootingEnabled(false);

        if (playerDash != null)
        {
            playerDash.SetDashEnabled(false);
        }

        laserLine.startWidth =
            laserWidth;

        laserLine.endWidth =
            laserWidth;

        laserLine.enabled = true;
    }

    private void UpdateLaser()
    {
        Vector2 origin =
            firePoint.position;

        Vector2 direction =
            firePoint.right;

        RaycastHit2D[] hits =
            Physics2D.CircleCastAll(
                origin,
                laserWidth * 0.5f,
                direction,
                laserMaxDistance,
                wallMask
            );

        float nearestDistance =
            laserMaxDistance;

        // Спочатку знаходимо найближчу стіну / перешкоду.
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform.root ==
                transform.root)
            {
                continue;
            }

            if ((ignoredLayers.value &
                (1 << hit.collider.gameObject.layer)) != 0)
            {
                continue;
            }

            IDamageable damageable =
                hit.collider
                    .GetComponentInParent<IDamageable>();

            // Ворог не зупиняє лазер.
            if (damageable != null)
            {
                continue;
            }

            // Звичайні trigger-и теж не зупиняють лазер.
            if (hit.collider.isTrigger)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance =
                    hit.distance;
            }
        }

        Vector2 endPoint =
            origin +
            direction * nearestDistance;

        laserLine.SetPosition(
            0,
            origin
        );

        laserLine.SetPosition(
            1,
            endPoint
        );

        if (Time.time >= nextLaserDamageTime)
        {
            DamageEnemies(
                hits,
                nearestDistance,
                direction
            );

            nextLaserDamageTime =
                Time.time +
                laserDamageInterval;
        }
    }

    private void DamageEnemies(
        RaycastHit2D[] hits,
        float laserDistance,
        Vector2 direction)
    {
        HashSet<IDamageable> damagedEnemies =
            new HashSet<IDamageable>();

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.collider.transform.root ==
                transform.root)
            {
                continue;
            }

            if ((ignoredLayers.value &
                (1 << hit.collider.gameObject.layer)) != 0)
            {
                continue;
            }

            // Ворог знаходиться вже за стіною.
            if (hit.distance > laserDistance)
            {
                continue;
            }

            IDamageable damageable =
                hit.collider
                    .GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            if (!damageable.IsAlive)
            {
                continue;
            }

            // Якщо у ворога декілька Collider2D,
            // він все одно отримує damage лише один раз за tick.
            if (!damagedEnemies.Add(damageable))
            {
                continue;
            }

            damageable.TakeDamage(
                new DamageInfo(
                    laserDamage,
                    direction,
                    gameObject
                )
            );
        }
    }

    public void CancelLaser()
    {
        if (!isLaserActive)
        {
            return;
        }

        StopLaser();
    }

    private void StopLaser()
    {
        if (!isLaserActive)
        {
            return;
        }

        isLaserActive = false;

        if (laserLine != null)
        {
            laserLine.enabled = false;
        }

        playerController
            .SetMoveSpeedMultiplier(1f);

        playerWeapon
            .SetAimSpeedMultiplier(1f);

        playerWeapon
            .SetShootingEnabled(true);

        if (playerDash != null)
        {
            playerDash.SetDashEnabled(true);
        }

        nextLaserTime =
            Time.time + laserCooldown;
    }
}