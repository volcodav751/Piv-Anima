using Game.Combat;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class BossDeath : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameEndController gameEndController;

    private Health health;
    private bool deathHandled;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        health.Died -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (deathHandled)
            return;

        deathHandled = true;

        // Hide the boss and disable its components immediately.
        gameObject.SetActive(false);

        if (gameEndController != null)
        {
            gameEndController.ShowVictory();
        }
        else
        {
            Debug.LogWarning(
                "BossDeath: Game End Controller is not assigned.",
                this
            );
        }

        Destroy(gameObject);
    }
}