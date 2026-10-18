using System.Collections;
using UnityEngine;

namespace Game.Combat
{
    public class HitFlash : MonoBehaviour
    {
        [SerializeField]
        private Health health;

        [SerializeField]
        private SpriteRenderer[] renderers;

        [SerializeField]
        private Material flashMaterial;

        [SerializeField]
        private Color flashColor = Color.white;

        [SerializeField, Min(0f)]
        private float duration = 0.15f;

        private Color[] _originalColors;
        private Material[] _originalMaterials;

        private Coroutine _routine;
        private bool _isFlashing;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<SpriteRenderer>();
            }

            _originalColors = new Color[renderers.Length];
            _originalMaterials = new Material[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                _originalColors[i] = renderers[i].color;
                _originalMaterials[i] = renderers[i].sharedMaterial;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = null;
            _isFlashing = false;

            RestoreRenderers();
        }

        private void LateUpdate()
        {
            if (!_isFlashing)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                renderers[i].color = flashColor;
                renderers[i].sharedMaterial = flashMaterial;
            }
        }

        private void OnDamaged(DamageInfo damage)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _isFlashing = true;

            yield return new WaitForSeconds(duration);

            _isFlashing = false;

            RestoreRenderers();

            _routine = null;
        }

        private void RestoreRenderers()
        {
            if (_originalColors == null ||
                _originalMaterials == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                renderers[i].color = _originalColors[i];
                renderers[i].sharedMaterial =
                    _originalMaterials[i];
            }
        }
    }
}