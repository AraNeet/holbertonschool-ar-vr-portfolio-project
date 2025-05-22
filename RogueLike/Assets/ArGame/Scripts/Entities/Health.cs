using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles entity health, damage, healing, and death states.
/// Provides events for other systems to respond to health changes.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;        // Maximum health value
    [SerializeField] private float currentHealth;           // Current health value
    [SerializeField] private bool isInvulnerable = false;   // Is entity permanently invulnerable
    [SerializeField] private float invulnerabilityTime = 1f; // Temporary invulnerability after taking damage

    [Header("Visual Feedback")]
    [SerializeField] private GameObject hitEffectPrefab;    // Visual effect when hit
    [SerializeField] private GameObject deathEffectPrefab;  // Visual effect when dying

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip hitSound;           // Sound when hit
    [SerializeField] private AudioClip deathSound;         // Sound when dying

    [Header("Events")]
    // Events that other systems can subscribe to
    public UnityEvent OnDeath = new UnityEvent();                          // Triggered when entity dies
    public UnityEvent<float, float> OnHealthChanged = new UnityEvent<float, float>(); // Triggered when health changes, passes (currentHealth, maxHealth)
    public UnityEvent OnDamaged = new UnityEvent();                        // Triggered when entity takes damage
    public UnityEvent OnHealed = new UnityEvent();                         // Triggered when entity is healed

    private bool isInvulnerabilityActive = false;           // Track temporary invulnerability state
    private AudioSource audioSource;                        // Reference to the AudioSource component

    // Public properties to access health information
    public float MaxHealth => maxHealth;                    // Get maximum health
    public float CurrentHealth => currentHealth;            // Get current health
    public float HealthPercentage => currentHealth / maxHealth; // Get health percentage (0-1)
    public bool IsDead => currentHealth <= 0;               // Check if entity is dead

    /// <summary>
    /// Initialize health and audio components on Awake
    /// </summary>
    private void Awake()
    {
        // Get or add AudioSource component if sounds are assigned
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hitSound != null || deathSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize health to maximum
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage to the entity if not invulnerable or dead
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    public void TakeDamage(float damageAmount)
    {
        // Don't take damage if invulnerable, already dead, or invalid damage amount
        if (isInvulnerabilityActive || isInvulnerable || IsDead || damageAmount <= 0)
            return;

        // Reduce health and clamp to minimum of 0
        currentHealth = Mathf.Max(0, currentHealth - damageAmount);

        // Trigger events
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged?.Invoke();

        // Spawn hit effect if assigned
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play hit sound if available
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Check if entity died from this damage
        if (currentHealth <= 0)
        {
            Die();
        }
        // Apply temporary invulnerability if configured
        else if (invulnerabilityTime > 0)
        {
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    /// <summary>
    /// Heal the entity if not dead
    /// </summary>
    /// <param name="healAmount">Amount to heal</param>
    public void Heal(float healAmount)
    {
        // Can't heal if dead or invalid heal amount
        if (IsDead || healAmount <= 0)
            return;

        // Store previous health to check if healing actually occurred
        float previousHealth = currentHealth;

        // Increase health and clamp to maximum
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        // Only trigger events if healing actually happened
        if (currentHealth > previousHealth)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealed?.Invoke();
        }
    }

    /// <summary>
    /// Set a new maximum health value
    /// </summary>
    /// <param name="newMaxHealth">New maximum health</param>
    public void SetMaxHealth(float newMaxHealth)
    {
        // Ensure max health is at least 1
        maxHealth = Mathf.Max(1, newMaxHealth);

        // Clamp current health to new maximum
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        // Notify listeners of the change
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Process entity death
    /// </summary>
    private void Die()
    {
        // Spawn death effect if assigned
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play death sound if available
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // Trigger death event for other systems to respond
        OnDeath?.Invoke();
    }

    /// <summary>
    /// Coroutine to handle temporary invulnerability
    /// </summary>
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerabilityActive = true;
        yield return new WaitForSeconds(invulnerabilityTime);
        isInvulnerabilityActive = false;
    }

    /// <summary>
    /// Reset health to maximum value
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}