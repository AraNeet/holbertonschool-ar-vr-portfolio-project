using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the Barb player's health on the UI.
/// </summary>
public class BrabHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Image healthFillImage;
    public TextMeshProUGUI healthText;

    [Header("Color Settings")]
    public Color healthyColor = Color.green;
    public Color damagedColor = Color.yellow;
    public Color criticalColor = Color.red;

    private Health playerHealth;

    void Start()
    {
        playerHealth = GetComponent<Health>();

        if (playerHealth == null)
        {
            Debug.LogError("No Health component found on player!");
            enabled = false;
            return;
        }

        // Subscribe to health change events
        playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);

        // Initial update
        UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        // Update slider if assigned
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Update text if assigned
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        // Update color if assigned
        if (healthFillImage != null)
        {
            float healthPercentage = currentHealth / maxHealth;

            if (healthPercentage > 0.6f)
            {
                healthFillImage.color = healthyColor;
            }
            else if (healthPercentage > 0.3f)
            {
                healthFillImage.color = damagedColor;
            }
            else
            {
                healthFillImage.color = criticalColor;
            }
        }
    }
}