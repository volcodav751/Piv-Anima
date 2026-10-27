using System.Collections;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Вешается на игрока (и на кого угодно ещё, кого надо откидывать).
    /// На время откидывания выключает PlayerController, чтобы тот не перетёр скорость,
    /// и плавно гасит скорость отлёта.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float decayPerStep = 0.85f; // во сколько раз падает скорость каждый FixedUpdate

        private Rigidbody2D rb;
        private PlayerController controller;
        private Coroutine routine;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            controller = GetComponent<PlayerController>();
        }

        public void Apply(Vector2 velocity, float stunDuration)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(KnockbackRoutine(velocity, stunDuration));
        }

        private IEnumerator KnockbackRoutine(Vector2 velocity, float stunDuration)
        {
            // SetMovementEnabled(false) обнуляет скорость, поэтому сначала он, потом отлёт
            if (controller != null) controller.SetMovementEnabled(false);
            rb.linearVelocity = velocity;

            float t = 0f;
            var wait = new WaitForFixedUpdate();
            while (t < stunDuration)
            {
                yield return wait;
                rb.linearVelocity *= decayPerStep;
                t += Time.fixedDeltaTime;
            }

            if (controller != null) controller.SetMovementEnabled(true);
            routine = null;
        }

        private void OnDisable()
        {
            if (routine != null && controller != null) controller.SetMovementEnabled(true);
            routine = null;
        }
    }
}
