using Game.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("Mana")]
    [SerializeField] private PlayerMana playerMana;
    [SerializeField] private Slider manaBar;
    [SerializeField] private TMP_Text manaText;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed += UpdateHealth;
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            UpdateHealth(
                playerHealth.Current,
                playerHealth.Max
            );
        }

        if (playerMana != null)
        {
            UpdateMana();
        }
    }

    private void Update()
    {
        if (playerMana != null)
        {
            UpdateMana();
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed -= UpdateHealth;
        }
    }

    private void UpdateHealth(int current, int max)
    {
        healthBar.maxValue = max;
        healthBar.value = current;

        if (healthText != null)
        {
            healthText.text = $"{current} / {max}";
        }
    }

    private void UpdateMana()
    {
        manaBar.maxValue = playerMana.MaxMana;
        manaBar.value = playerMana.CurrentMana;

        if (manaText != null)
        {
            manaText.text =
                $"{playerMana.CurrentMana:F0} / {playerMana.MaxMana:F0}";
        }
    }
}