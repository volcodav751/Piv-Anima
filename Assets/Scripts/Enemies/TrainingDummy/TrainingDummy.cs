using UnityEngine;

namespace Game.Combat
{
    // Standalone damage receiver: do not add Health to the same object.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private Vector3 popupOffset = new Vector3(0f, 0.25f, 0f);
        [SerializeField] private Color damageColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField, Min(0.1f)] private float popupLifetime = 0.8f;
        [SerializeField, Min(0.1f)] private float fontSize = 2f;

        private Collider2D hitbox;

        public bool IsAlive => isActiveAndEnabled;

        private void Awake()
        {
            hitbox = GetComponent<BoxCollider2D>();
        }

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = false;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                gameObject.layer = enemyLayer;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0)
                return;

            Vector3 position = new Vector3(
                hitbox.bounds.center.x,
                hitbox.bounds.max.y,
                transform.position.z) + popupOffset;
            position.x += Random.Range(-0.15f, 0.15f);

            DamagePopup.Show(damage.Amount, position, damageColor, popupLifetime, fontSize);
        }
    }
}
