using Game.Combat;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeath : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += OnPlayerDied;
    }

    private void OnDisable()
    {
        health.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        Debug.Log("Player died. Game ended.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}