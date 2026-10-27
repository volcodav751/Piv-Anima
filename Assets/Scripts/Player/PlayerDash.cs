using System.Collections;
using Game.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(
    typeof(Rigidbody2D),
    typeof(PlayerController))]
public class PlayerDash : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference dashAction;

    [Header("Settings")]
    [SerializeField, Min(0f)]
    private float dashDistance = 2.5f;

    [SerializeField, Min(0.01f)]
    private float dashDuration = 0.15f;

    [SerializeField, Min(0f)]
    private float dashCooldown = 1f;

    private Rigidbody2D rigidBody;
    private PlayerController playerController;

    private bool isDashing;
    private bool dashEnabled = true;
    private float nextDashTime;
    private KnockbackReceiver knockbackReceiver;

    private Coroutine dashRoutine;

    public bool IsDashing =>
        isDashing;

    private void Awake()
    {
        rigidBody =
            GetComponent<Rigidbody2D>();

        playerController =
            GetComponent<PlayerController>();

        knockbackReceiver =
            GetComponent<KnockbackReceiver>();
    }

    private void OnEnable()
    {
        if (dashAction != null)
        {
            dashAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (dashAction != null)
        {
            dashAction.action.Disable();
        }

        CancelDash();
    }

    private void Update()
    {
        if (dashAction == null)
        {
            return;
        }

        if (dashAction.action
            .WasPressedThisFrame())
        {
            TryDash();
        }
    }

    private void TryDash()
    {
        if (knockbackReceiver != null &&
            knockbackReceiver.IsKnockedBack)
        {
            return;
        }

        if (!dashEnabled)
        {
            return;
        }

        if (isDashing)
        {
            return;
        }

        if (Time.time < nextDashTime)
        {
            return;
        }

        Vector2 direction =
            playerController.GetMoveInput();

        if (direction == Vector2.zero)
        {
            return;
        }

        dashRoutine =
            StartCoroutine(
                Dash(direction.normalized)
            );
    }

    private IEnumerator Dash(
        Vector2 direction)
    {
        isDashing = true;

        nextDashTime =
            Time.time + dashCooldown;

        playerController
            .SetMovementEnabled(false);

        float dashSpeed =
            dashDistance / dashDuration;

        rigidBody.linearVelocity =
            direction * dashSpeed;

        yield return new WaitForSeconds(
            dashDuration
        );

        rigidBody.linearVelocity =
            Vector2.zero;

        isDashing = false;
        dashRoutine = null;

        if (knockbackReceiver == null ||
            !knockbackReceiver.IsKnockedBack)
        {
            playerController
                .SetMovementEnabled(true);
        }
    }

    public void CancelDash()
    {
        if (!isDashing)
        {
            return;
        }

        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        rigidBody.linearVelocity =
            Vector2.zero;

        isDashing = false;
    }

    public void SetDashEnabled(
        bool enabled)
    {
        dashEnabled = enabled;
    }
}