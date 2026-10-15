using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifetime = 5f;

    private Vector2 direction;

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;

        // Повертаємо картинку кулі в напрямку польоту.
        float angle = Mathf.Atan2(direction.y, direction.x)
                      * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);
    }
}