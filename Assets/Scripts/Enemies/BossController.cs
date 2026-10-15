using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform areaMin;
    [SerializeField] private Transform areaMax;
    [SerializeField] private float changeTargetInterval = 4f;

    [Header("Стрільба")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform firePoint;
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private float shootInterval = 2f;

    private Rigidbody2D rb;
    private Vector2 moveTarget;
    private float moveTimer;
    private float shootTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ChooseMoveTarget();
        shootTimer = shootInterval;
    }

    private void Update()
    {
        // Через певний час вибираємо нову точку руху.
        moveTimer -= Time.deltaTime;

        if (moveTimer <= 0f)
        {
            ChooseMoveTarget();
        }

        // Відраховуємо час до наступного пострілу.
        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f)
        {
            Shoot();
            shootTimer = Mathf.Max(0.1f, shootInterval);
        }
    }

    private void FixedUpdate()
    {
        if (areaMin == null || areaMax == null)
            return;

        if (Vector2.Distance(rb.position, moveTarget) < 0.1f)
        {
            ChooseMoveTarget();
        }

        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            moveTarget,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);
    }

    private void ChooseMoveTarget()
    {
        moveTimer = Mathf.Max(0.1f, changeTargetInterval);

        if (areaMin == null || areaMax == null)
            return;

        float x = Random.Range(
            Mathf.Min(areaMin.position.x, areaMax.position.x),
            Mathf.Max(areaMin.position.x, areaMax.position.x)
        );

        float y = Random.Range(
            Mathf.Min(areaMin.position.y, areaMax.position.y),
            Mathf.Max(areaMin.position.y, areaMax.position.y)
        );

        moveTarget = new Vector2(x, y);
    }

    private void Shoot()
    {
        if (player == null || firePoint == null ||
            projectilePrefab == null)
            return;

        Vector2 direction =
            (Vector2)player.position - (Vector2)firePoint.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        BossProjectile projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        projectile.Initialize(direction);
    }
}