using System.Collections;
using UnityEngine;

namespace Game.Enemies
{
    public class MolotovProjectile : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Transform visual;

        [SerializeField]
        private GameObject fireAreaPrefab;

        [Header("Throw Settings")]
        [SerializeField, Min(0.05f)]
        private float travelDuration = 0.7f;

        [SerializeField, Min(0f)]
        private float arcHeight = 0.8f;

        private Coroutine travelRoutine;

        public void Launch(Vector2 targetPosition)
        {
            if (travelRoutine != null)
            {
                StopCoroutine(travelRoutine);
            }

            travelRoutine =
                StartCoroutine(
                    TravelRoutine(targetPosition)
                );
        }

        private IEnumerator TravelRoutine(
            Vector2 targetPosition)
        {
            Vector2 startPosition =
                transform.position;

            Vector3 visualStartPosition =
                visual != null
                    ? visual.localPosition
                    : Vector3.zero;

            float elapsedTime = 0f;

            while (elapsedTime < travelDuration)
            {
                elapsedTime +=
                    Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime /
                        travelDuration
                    );

                transform.position =
                    Vector2.Lerp(
                        startPosition,
                        targetPosition,
                        progress
                    );

                if (visual != null)
                {
                    float arc =
                        4f *
                        arcHeight *
                        progress *
                        (1f - progress);

                    visual.localPosition =
                        visualStartPosition +
                        Vector3.up * arc;
                }

                yield return null;
            }

            transform.position =
                targetPosition;

            if (fireAreaPrefab != null)
            {
                Instantiate(
                    fireAreaPrefab,
                    targetPosition,
                    Quaternion.identity
                );
            }

            Destroy(gameObject);
        }
    }
}