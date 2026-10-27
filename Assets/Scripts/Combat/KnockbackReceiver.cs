using System.Collections;
using UnityEngine;

namespace Game.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)]
        private float decayPerStep = 0.85f;

        private Rigidbody2D rb;
        private PlayerController controller;
        private PlayerShield playerShield;
        private PlayerDash playerDash;
        private PlayerChargedShot playerChargedShot;

        private Coroutine routine;

        public bool IsKnockedBack =>
            routine != null;

        private void Awake()
        {
            rb =
                GetComponent<Rigidbody2D>();

            controller =
                GetComponent<PlayerController>();

            playerShield =
                GetComponent<PlayerShield>();

            playerDash =
                GetComponent<PlayerDash>();

            playerChargedShot =
                GetComponent<PlayerChargedShot>();
        }

        public void Apply(
            Vector2 velocity,
            float stunDuration)
        {
            if (playerShield != null &&
                playerShield.IsShieldActive)
            {
                playerShield.BreakShield();
                return;
            }

            if (playerDash != null)
            {
                playerDash.CancelDash();
            }

            if (playerChargedShot != null)
            {
                playerChargedShot.CancelLaser();
            }

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            routine =
                StartCoroutine(
                    KnockbackRoutine(
                        velocity,
                        stunDuration
                    )
                );
        }

        private IEnumerator KnockbackRoutine(
            Vector2 velocity,
            float stunDuration)
        {
            if (controller != null)
            {
                controller
                    .SetMovementEnabled(false);
            }

            rb.linearVelocity =
                velocity;

            float elapsedTime = 0f;

            WaitForFixedUpdate wait =
                new WaitForFixedUpdate();

            while (elapsedTime < stunDuration)
            {
                yield return wait;

                rb.linearVelocity *=
                    decayPerStep;

                elapsedTime +=
                    Time.fixedDeltaTime;
            }

            rb.linearVelocity =
                Vector2.zero;

            routine = null;

            if (controller != null &&
                (playerShield == null ||
                 !playerShield.IsShieldActive))
            {
                controller
                    .SetMovementEnabled(true);
            }
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            rb.linearVelocity =
                Vector2.zero;
        }
    }
}