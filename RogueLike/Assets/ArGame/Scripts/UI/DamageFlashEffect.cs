using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a screen flash effect when the player takes damage
/// </summary>
public class DamageFlashEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("References")]
    [SerializeField] private Health playerHealth;

    private Coroutine activeFlashCoroutine;

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

        // Subscribe to damage event
        if (playerHealth != null)
        {
            playerHealth.OnDamaged.AddListener(TriggerDamageFlash);
        }
        else
        {
            Debug.LogError("DamageFlashEffect: Player Health component not found!");
        }

        // Make sure flash image starts invisible
        if (flashImage != null)
        {
            Color startColor = flashColor;
            startColor.a = 0;
            flashImage.color = startColor;
        }
        else
        {
            Debug.LogError("DamageFlashEffect: Flash Image component not assigned!");
        }
    }

    /// <summary>
    /// Triggers the damage flash effect
    /// </summary>
    public void TriggerDamageFlash()
    {
        // Stop any active flash coroutine
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
        }

        // Start new flash effect
        activeFlashCoroutine = StartCoroutine(FlashCoroutine());
    }

    /// <summary>
    /// Coroutine to animate the flash effect
    /// </summary>
    private IEnumerator FlashCoroutine()
    {
        float timer = 0;

        // Set starting color
        Color currentColor = flashColor;

        // Animate flash
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / flashDuration;

            // Evaluate alpha from curve
            currentColor.a = flashColor.a * flashCurve.Evaluate(normalizedTime);

            // Apply color
            flashImage.color = currentColor;

            yield return null;
        }

        // Ensure flash is fully transparent at the end
        currentColor.a = 0;
        flashImage.color = currentColor;
        activeFlashCoroutine = null;
    }

    /// <summary>
    /// Clean up event subscriptions when destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged.RemoveListener(TriggerDamageFlash);
        }
    }
}