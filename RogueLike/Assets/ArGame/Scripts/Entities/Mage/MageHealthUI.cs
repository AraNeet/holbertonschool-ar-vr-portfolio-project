using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Manages the UI display for the Mage's health.
/// </summary>
public class MageHealthUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;        // The slider that displays health
    public Image fillImage;            // The fill image to change color based on health
    public Text healthText;            // Text to display numeric health value

    [Header("Color Settings")]
    public Color fullHealthColor = new Color(0.2f, 0.4f, 1.0f);  // Blue for mage's health
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;  // 30% health considered "low"

    // Reference to the player's health component
    private Health playerHealth;

    void Start()
    {
        // Find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Subscribe to health change events with the correct handler method
                playerHealth.OnHealthChanged.AddListener(OnHealthChangedHandler);

                // Initialize UI
                UpdateHealthUI();
            }
            else
            {
                Debug.LogError("[MageHealthUI] Player does not have a Health component!");
            }
        }
        else
        {
            Debug.LogError("[MageHealthUI] Player not found in scene!");
        }

        // Create UI elements if not assigned
        if (healthSlider == null)
        {
            Debug.LogWarning("[MageHealthUI] Health slider not assigned. Please assign in the inspector.");
        }

        if (fillImage == null && healthSlider != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
            if (fillImage == null)
            {
                Debug.LogWarning("[MageHealthUI] Fill image not found on health slider.");
            }
        }
    }

    /// <summary>
    /// Handler for OnHealthChanged event that matches the event signature
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    private void OnHealthChangedHandler(float currentHealth, float maxHealth)
    {
        // Simply call our existing update method
        UpdateHealthUI();
    }

    /// <summary>
    /// Update the health UI display
    /// </summary>
    public void UpdateHealthUI()
    {
        if (playerHealth == null || healthSlider == null)
            return;

        // Update slider value
        float healthPercentage = playerHealth.CurrentHealth / playerHealth.MaxHealth;
        healthSlider.value = healthPercentage;

        // Update text if available
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
        }

        // Update fill color based on health percentage
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, Mathf.Clamp01(healthPercentage / lowHealthThreshold));
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from health events to prevent memory leaks
        if (playerHealth != null && playerHealth.OnHealthChanged != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChangedHandler);
        }
    }
}