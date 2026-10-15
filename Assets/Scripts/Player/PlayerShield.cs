using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;

[RequireComponent(
    typeof(PlayerController),
    typeof(PlayerMana),
    typeof(Health))]
public class PlayerShield : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private GameObject shieldVisual;

    [Header("Input")]
    [SerializeField]
    private InputActionReference shieldAction;

    [Header("Shield Settings")]
    [SerializeField, Min(0.1f)]
    private float shieldDuration = 3f;

    [SerializeField, Min(0f)]
    private float manaCost = 40f;

    private PlayerController playerController;
    private PlayerMana playerMana;
    private PlayerDash playerDash;
    private Health health;

    private bool isShieldActive;

    public bool IsShieldActive => isShieldActive;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerMana = GetComponent<PlayerMana>();
        playerDash = GetComponent<PlayerDash>();
        health = GetComponent<Health>();

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (shieldAction != null)
        {
            shieldAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (shieldAction != null)
        {
            shieldAction.action.Disable();
        }

        if (isShieldActive)
        {
            StopShield();
        }
    }

    private void Update()
    {
        if (shieldAction == null)
        {
            return;
        }

        if (shieldAction.action.WasPressedThisFrame())
        {
            TryActivateShield();
        }
    }

    private void TryActivateShield()
    {
        if (isShieldActive)
        {
            return;
        }

        if (!playerMana.TrySpendMana(manaCost))
        {
            return;
        }

        StartCoroutine(ShieldRoutine());
    }

    private IEnumerator ShieldRoutine()
    {
        StartShield();

        yield return new WaitForSeconds(shieldDuration);

        StopShield();
    }

    private void StartShield()
    {
        isShieldActive = true;

        playerController.SetMovementEnabled(false);
        playerMana.SetRegenerationPaused(true);

        health.SetDamageBlocked(true);

        if (playerDash != null)
        {
            playerDash.SetDashEnabled(false);
        }

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }

        Debug.Log(
            $"Shield activated for {shieldDuration:F1} seconds."
        );
    }

    private void StopShield()
    {
        isShieldActive = false;

        playerController.SetMovementEnabled(true);
        playerMana.SetRegenerationPaused(false);

        health.SetDamageBlocked(false);

        if (playerDash != null)
        {
            playerDash.SetDashEnabled(true);
        }

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }

        Debug.Log("Shield ended. Mana regeneration resumed.");
    }
}