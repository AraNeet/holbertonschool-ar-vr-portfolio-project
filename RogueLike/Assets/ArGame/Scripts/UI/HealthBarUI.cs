using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the health bar UI element for the player
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Image fillImage;

    [Header("Color Settings")]
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float mediumHealthThreshold = 0.6f;

    [Header("References")]
    [SerializeField] private Health playerHealth;

    [Header("Animation")]
    [SerializeField] private bool animate = true;
    [SerializeField] private float animationSpeed = 5f;
    private float targetFillAmount;

    // Start is called before the first frame update
    private void Start()
    {
        // Try to find player health component if not assigned
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        // Setup health bar UI
        if (playerHealth != null)
        {
            // Initialize the slider
            healthSlider.maxValue = playerHealth.MaxHealth;
            healthSlider.value = playerHealth.CurrentHealth;

            // Set the initial target fill amount
            targetFillAmount = playerHealth.HealthPercentage;

            // Subscribe to health changed event
            playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);

            // Update UI immediately
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
        else
        {
            Debug.LogError("HealthBarUI: Player Health component not found!");
        }
    }

    private void Update()
    {
        // Animate health bar if enabled
        if (animate && healthSlider != null)
        {
            // Smoothly interpolate slider value to target
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetFillAmount * healthSlider.maxValue, Time.deltaTime * animationSpeed);
        }
    }

    /// <summary>
    /// Updates the health UI elements with the current health value
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            // Update slider max value if needed
            if (healthSlider.maxValue != maxHealth)
            {
                healthSlider.maxValue = maxHealth;
            }

            // Set the target value for animation
            targetFillAmount = currentHealth / maxHealth;

            // If animation is disabled, update immediately
            if (!animate)
            {
                healthSlider.value = currentHealth;
            }
        }

        // Update text if available
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";
        }

        // Update color based on health percentage
        UpdateHealthBarColor(currentHealth / maxHealth);
    }

    /// <summary>
    /// Updates the health bar fill color based on health percentage
    /// </summary>
    /// <param name="healthPercent">Current health percentage (0-1)</param>
    private void UpdateHealthBarColor(float healthPercent)
    {
        if (fillImage != null)
        {
            Color newColor;

            if (healthPercent <= lowHealthThreshold)
            {
                newColor = lowHealthColor;
            }
            else if (healthPercent <= mediumHealthThreshold)
            {
                newColor = mediumHealthColor;
            }
            else
            {
                newColor = highHealthColor;
            }

            fillImage.color = newColor;
        }
    }

    /// <summary>
    /// Clean up event subscriptions when destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
        }
    }
}