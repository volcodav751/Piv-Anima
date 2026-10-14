using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    private float moveSpeedMultiplier = 1f;

    [SerializeField]
    private InputActionReference moveAction;

    private Rigidbody2D rigidBody;
    private Vector2 moveInput;

    private bool movementEnabled = true;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }

        moveInput = Vector2.zero;
    }

    private void Update()
    {
        if (moveAction == null)
        {
            return;
        }

        moveInput = moveAction.action.ReadValue<Vector2>();
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        if (!movementEnabled)
        {
            return;
        }

        rigidBody.linearVelocity =
            moveInput * moveSpeed * moveSpeedMultiplier;
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!movementEnabled)
        {
            rigidBody.linearVelocity = Vector2.zero;
        }
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }
}