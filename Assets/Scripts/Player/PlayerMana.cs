using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana Settings")]
    [SerializeField, Min(1f)]
    private float maxMana = 100f;

    [SerializeField, Min(0f)]
    private float manaRegenerationPerSecond = 5f;

    private float currentMana;
    private bool regenerationPaused;

    private float nextManaLogTime;

    public float CurrentMana => currentMana;
    public float MaxMana => maxMana;

    private void Awake()
    {
        currentMana = maxMana;
    }

    private void Update()
    {
        RegenerateMana();
    }

    public bool TrySpendMana(float amount)
    {
        if (currentMana < amount)
        {
            Debug.Log(
                $"Not enough mana. Mana: {currentMana:F0}/{maxMana:F0}"
            );

            return false;
        }

        currentMana -= amount;

        Debug.Log(
            $"Mana spent: {amount:F0}. Mana: {currentMana:F0}/{maxMana:F0}"
        );

        return true;
    }

    public void SetRegenerationPaused(bool paused)
    {
        regenerationPaused = paused;
    }

    private void RegenerateMana()
    {
        if (regenerationPaused)
        {
            return;
        }

        if (currentMana >= maxMana)
        {
            return;
        }

        currentMana += manaRegenerationPerSecond * Time.deltaTime;
        currentMana = Mathf.Min(currentMana, maxMana);

        if (Time.time >= nextManaLogTime)
        {
            Debug.Log(
                $"Mana: {currentMana:F0}/{maxMana:F0}"
            );

            nextManaLogTime = Time.time + 1f;
        }
    }
}