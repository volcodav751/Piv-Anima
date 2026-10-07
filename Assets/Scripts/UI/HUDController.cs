using Game.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.Changed += UpdateHealth;
    }

    private void Start()
    {
        if (playerHealth != null)
            UpdateHealth(playerHealth.Current, playerHealth.Max);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Changed -= UpdateHealth;
    }

    private void UpdateHealth(int current, int max)
    {
        healthBar.maxValue = max;
        healthBar.value = current;

        healthText.text = $"{current} / {max}";
    }
}