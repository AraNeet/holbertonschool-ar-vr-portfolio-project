using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Links an enemy's health bar UI to their Health component
/// </summary>
public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;

    private Health enemyHealth;
    private Transform mainCameraTransform;

    private void Start()
    {
        // Find the enemy's Health component (try parent if this is a child UI object)
        enemyHealth = GetComponentInParent<Health>();
        if (enemyHealth == null)
        {
            Debug.LogError($"No Health component found for {gameObject.name}");
            return;
        }

        // Get the camera transform for billboard effect
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // Initialize health bar
        if (healthSlider != null)
        {
            healthSlider.maxValue = enemyHealth.MaxHealth;
            healthSlider.value = enemyHealth.CurrentHealth;

            // Subscribe to health change events
            enemyHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        }
        else
        {
            Debug.LogError("Health slider reference is missing!");
        }
    }

    private void Update()
    {
        // Make the health bar face the camera (billboard effect)
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                            mainCameraTransform.rotation * Vector3.up);
        }
    }

    /// <summary>
    /// Updates the health bar value and color
    /// </summary>
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;

            // Update fill color based on health percentage
            if (fillImage != null)
            {
                float healthPercent = currentHealth / maxHealth;
                fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }
}