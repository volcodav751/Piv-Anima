using System.Collections;
using Game.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private float shieldDuration = 5f;

    [SerializeField, Min(0f)]
    private float manaCost = 60f;

    private PlayerController playerController;
    private PlayerMana playerMana;
    private PlayerDash playerDash;
    private Health health;
    private KnockbackReceiver knockbackReceiver;

    private Coroutine shieldRoutine;

    private bool isShieldActive;

    public bool IsShieldActive =>
        isShieldActive;

    private void Awake()
    {
        playerController =
            GetComponent<PlayerController>();

        playerMana =
            GetComponent<PlayerMana>();

        playerDash =
            GetComponent<PlayerDash>();

        health =
            GetComponent<Health>();

        knockbackReceiver =
            GetComponent<KnockbackReceiver>();

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

        if (shieldRoutine != null)
        {
            StopCoroutine(shieldRoutine);
            shieldRoutine = null;
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

        if (isShieldActive &&
            shieldAction.action.WasReleasedThisFrame())
        {
            StopShieldEarly();
        }
    }

    private void TryActivateShield()
    {
        if (isShieldActive)
        {
            return;
        }

        if (knockbackReceiver != null &&
            knockbackReceiver.IsKnockedBack)
        {
            return;
        }

        if (!playerMana.TrySpendMana(manaCost))
        {
            return;
        }

        shieldRoutine =
            StartCoroutine(
                ShieldRoutine()
            );
    }

    private IEnumerator ShieldRoutine()
    {
        StartShield();

        yield return new WaitForSeconds(
            shieldDuration
        );

        shieldRoutine = null;

        StopShield();
    }

    private void StartShield()
    {
        isShieldActive = true;

        playerController
            .SetMovementEnabled(false);

        playerMana
            .SetRegenerationPaused(true);

        health
            .SetDamageBlocked(true);

        if (playerDash != null)
        {
            playerDash
                .SetDashEnabled(false);
        }

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }

        Debug.Log(
            $"Shield activated. Maximum duration: {shieldDuration:F1} seconds."
        );
    }

    private void StopShieldEarly()
    {
        if (!isShieldActive)
        {
            return;
        }

        if (shieldRoutine != null)
        {
            StopCoroutine(shieldRoutine);
            shieldRoutine = null;
        }

        StopShield();
    }

    private void StopShield()
    {
        if (!isShieldActive)
        {
            return;
        }

        isShieldActive = false;

        if (knockbackReceiver == null ||
            !knockbackReceiver.IsKnockedBack)
        {
            playerController
                .SetMovementEnabled(true);
        }

        playerMana
            .SetRegenerationPaused(false);

        health
            .SetDamageBlocked(false);

        if (playerDash != null)
        {
            playerDash
                .SetDashEnabled(true);
        }

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }

        Debug.Log(
            "Shield ended. Mana regeneration resumed."
        );
    }

    public void BreakShield()
    {
        if (!isShieldActive)
        {
            return;
        }

        if (shieldRoutine != null)
        {
            StopCoroutine(shieldRoutine);
            shieldRoutine = null;
        }

        StopShield();

        Debug.Log(
            "Shield was broken."
        );
    }
}