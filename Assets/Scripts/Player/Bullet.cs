using Game.Combat;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int damage = 1;

    [SerializeField, Min(0f)]
    private float lifetime = 3f;

    private Rigidbody2D rigidBody;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable target =
            collision.collider.GetComponentInParent<IDamageable>();

        if (target != null && target.IsAlive)
        {
            Vector2 damageDirection =
                rigidBody.linearVelocity.normalized;

            target.TakeDamage(
                new DamageInfo(
                    damage,
                    damageDirection,
                    gameObject
                )
            );
        }

        Destroy(gameObject);
    }
}