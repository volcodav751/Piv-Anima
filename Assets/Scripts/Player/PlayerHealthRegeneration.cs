using System.Collections;
using Game.Combat;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealthRegeneration : MonoBehaviour
{
    [Header("Regeneration")]
    [SerializeField, Min(1)]
    private int regenerationAmount = 5;

    [SerializeField, Min(0.1f)]
    private float regenerationInterval = 5f;

    private Health health;
    private Coroutine regenerationRoutine;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        regenerationRoutine =
            StartCoroutine(RegenerationRoutine());
    }

    private void OnDisable()
    {
        if (regenerationRoutine != null)
        {
            StopCoroutine(regenerationRoutine);
            regenerationRoutine = null;
        }
    }

    private IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                regenerationInterval
            );

            if (!health.IsAlive)
            {
                continue;
            }

            if (health.Current >= health.Max)
            {
                continue;
            }

            health.Heal(regenerationAmount);

            Debug.Log(
                $"Player HP: {health.Current}/{health.Max}"
            );
        }
    }
}