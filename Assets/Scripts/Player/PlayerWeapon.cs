using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform playerVisual;

    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private GameObject bulletPrefab;

    [Header("Input")]
    [SerializeField]
    private InputActionReference aimAction;

    [SerializeField]
    private InputActionReference shootAction;

    [Header("Settings")]
    [SerializeField, Min(0f)]
    private float aimSpeed = 360f;

    [SerializeField, Min(0f)]
    private float shootCooldown = 0.25f;

    [SerializeField, Min(0f)]
    private float bulletSpeed = 10f;

    [SerializeField, Min(0f)]
    private float bulletLifetime = 3f;

    private Camera mainCamera;
    private float nextShotTime;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        aimAction.action.Enable();
        shootAction.action.Enable();
    }

    private void OnDisable()
    {
        aimAction.action.Disable();
        shootAction.action.Disable();
    }

    private void Update()
    {
        Aim();
        HandleShooting();
    }

    private void Aim()
    {
        Vector2 mouseScreenPosition =
            aimAction.action.ReadValue<Vector2>();

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 direction =
            mouseWorldPosition - transform.position;

        float targetAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        playerVisual.rotation = Quaternion.RotateTowards(
            playerVisual.rotation,
            targetRotation,
            aimSpeed * Time.deltaTime
        );
    }

    private void HandleShooting()
    {
        if (!shootAction.action.IsPressed())
        {
            return;
        }

        if (Time.time < nextShotTime)
        {
            return;
        }

        Shoot();

        nextShotTime = Time.time + shootCooldown;
    }

    private void Shoot()
    {
        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        Rigidbody2D bulletRigidBody =
            bullet.GetComponent<Rigidbody2D>();

        bulletRigidBody.linearVelocity =
            firePoint.right * bulletSpeed;

        Destroy(bullet, bulletLifetime);
    }
}