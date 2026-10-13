using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [SerializeField]
    private InputActionReference moveAction;

    private Rigidbody2D rigidBody;
    private Vector2 moveInput;

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
        rigidBody.linearVelocity = moveInput * moveSpeed;
    }
}