using System.Collections;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Flashes sprites white when the Health takes damage. Pure feedback, optional.</summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Health health;
        [Tooltip("Leave empty to use all SpriteRenderers in children.")]
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField, Min(0f)] private float duration = 0.08f;

        private Color[] _originalColors;
        private Coroutine _routine;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<Health>();
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>();

            _originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                _originalColors[i] = renderers[i].color;
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            _routine = null;
            RestoreColors();
        }

        private void OnDamaged(DamageInfo _)
        {
            if (!isActiveAndEnabled) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            foreach (var r in renderers)
                if (r != null) r.color = flashColor;

            yield return new WaitForSeconds(duration);

            RestoreColors();
            _routine = null;
        }

        private void RestoreColors()
        {
            if (_originalColors == null) return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = _originalColors[i];
        }
    }
}
