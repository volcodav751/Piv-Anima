using System.Collections;
using Game.Combat;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField, Min(1)]
    private int damage = 20;

    [SerializeField, Min(0.01f)]
    private float damageInterval = 0.25f;

    [SerializeField, Min(0f)]
    private float activationDelay = 0.1f;

    [Header("Trap State")]
    [SerializeField]
    private bool trapActive = true;

    private Coroutine damageRoutine;
    private Health playerHealth;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!trapActive)
        {
            return;
        }

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        playerHealth =
            player.GetComponent<Health>();

        if (playerHealth == null ||
            !playerHealth.IsAlive)
        {
            return;
        }

        if (damageRoutine == null)
        {
            damageRoutine =
                StartCoroutine(DamageRoutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        StopDamage();
    }

    private IEnumerator DamageRoutine()
    {
        if (activationDelay > 0f)
        {
            yield return new WaitForSeconds(
                activationDelay
            );
        }

        while (trapActive &&
               playerHealth != null &&
               playerHealth.IsAlive)
        {
            playerHealth.TakeDamage(
                new DamageInfo(
                    damage,
                    Vector2.zero,
                    gameObject
                )
            );

            yield return new WaitForSeconds(
                damageInterval
            );
        }

        damageRoutine = null;
    }

    private void StopDamage()
    {
        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }

        playerHealth = null;
    }

    public void SetTrapActive(bool active)
    {
        trapActive = active;

        if (!trapActive)
        {
            StopDamage();
        }
    }
}