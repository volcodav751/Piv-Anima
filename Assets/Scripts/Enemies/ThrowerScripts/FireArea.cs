using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies.Thrower
{
    public class FireArea : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private SpriteRenderer areaRenderer;

        [Header("Fire Settings")]
        [SerializeField, Min(0.1f)]
        private float radius = 1.2f;

        [SerializeField, Min(0.1f)]
        private float duration = 4f;

        [SerializeField, Min(0.1f)]
        private float damageInterval = 0.5f;

        [SerializeField, Min(1)]
        private int damagePerTick = 5;

        [SerializeField]
        private LayerMask damageMask;

        private void Awake()
        {
            if (areaRenderer == null)
            {
                areaRenderer =
                    GetComponent<SpriteRenderer>();
            }

            UpdateVisualSize();
        }

        private void Start()
        {
            StartCoroutine(
                FireRoutine()
            );
        }

        private IEnumerator FireRoutine()
        {
            float elapsedTime = 0f;

            while (elapsedTime <
                   duration)
            {
                DealDamage();

                yield return
                    new WaitForSeconds(
                        damageInterval
                    );

                elapsedTime +=
                    damageInterval;
            }

            Destroy(gameObject);
        }

        private void DealDamage()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    radius,
                    damageMask
                );

            HashSet<GameObject>
                damagedObjects =
                    new HashSet<GameObject>();

            foreach (Collider2D hit
                     in hits)
            {
                GameObject root =
                    hit.attachedRigidbody != null
                        ? hit.attachedRigidbody
                            .gameObject
                        : hit.gameObject;

                if (!damagedObjects.Add(
                    root))
                {
                    continue;
                }

                IDamageable damageable =
                    hit.GetComponentInParent<
                        IDamageable>();

                if (damageable == null ||
                    !damageable.IsAlive)
                {
                    continue;
                }

                damageable.TakeDamage(
                    new DamageInfo(
                        damagePerTick,
                        Vector2.zero,
                        gameObject
                    )
                );
            }
        }

        private void UpdateVisualSize()
        {
            if (areaRenderer == null ||
                areaRenderer.sprite == null)
            {
                return;
            }

            Vector2 spriteSize =
                areaRenderer.sprite.bounds.size;

            if (spriteSize.x <= 0f ||
                spriteSize.y <= 0f)
            {
                return;
            }

            float diameter =
                radius * 2f;

            transform.localScale =
                new Vector3(
                    diameter /
                    spriteSize.x,
                    diameter /
                    spriteSize.y,
                    1f
                );
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (areaRenderer == null)
                {
                    areaRenderer =
                        GetComponent<SpriteRenderer>();
                }

                UpdateVisualSize();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(
                transform.position,
                radius
            );
        }
    }
}