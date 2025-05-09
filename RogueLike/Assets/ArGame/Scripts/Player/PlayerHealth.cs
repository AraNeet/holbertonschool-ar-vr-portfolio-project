using UnityEngine;
using UnityEngine.Events;

namespace ArGame.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private float invulnerabilityTime = 1f;
        [SerializeField] private bool godMode = false;
        
        [Header("Visuals")]
        [SerializeField] private GameObject damageEffectPrefab;
        [SerializeField] private Transform damageEffectPoint;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<float> onDamageTaken;
        [SerializeField] private UnityEvent<float> onHealthChanged;
        [SerializeField] private UnityEvent onDeath;
        
        // Component references
        private PlayerAnimator playerAnimator;
        private PlayerController playerController;
        
        // State tracking
        private bool isInvulnerable = false;
        private float invulnerabilityTimer = 0f;
        private bool isDead = false;
        
        private void Awake()
        {
            // Get references
            playerAnimator = GetComponent<PlayerAnimator>();
            playerController = GetComponent<PlayerController>();
            
            // Initialize health
            currentHealth = maxHealth;
        }
        
        private void Start()
        {
            // Notify initial health
            onHealthChanged?.Invoke(currentHealth / maxHealth);
        }
        
        private void Update()
        {
            // Handle invulnerability
            if (isInvulnerable)
            {
                invulnerabilityTimer -= Time.deltaTime;
                if (invulnerabilityTimer <= 0)
                {
                    isInvulnerable = false;
                }
            }
        }
        
        public void TakeDamage(float damage, Vector3 damageSource = default)
        {
            // Check if we can take damage
            if (isInvulnerable || isDead || godMode)
                return;
                
            // Apply damage
            currentHealth -= damage;
            
            // Clamp health to 0
            currentHealth = Mathf.Max(0, currentHealth);
            
            // Invoke damage event
            onDamageTaken?.Invoke(damage);
            
            // Notify health percentage
            onHealthChanged?.Invoke(currentHealth / maxHealth);
            
            // Trigger animation
            if (playerAnimator != null)
            {
                playerAnimator.TriggerHitAnimation();
            }
            
            // Show damage effect
            if (damageEffectPrefab != null && damageEffectPoint != null)
            {
                Instantiate(damageEffectPrefab, damageEffectPoint.position, Quaternion.identity);
            }
            
            // Start invulnerability period
            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityTime;
            
            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        public void AddHealth(float amount)
        {
            if (isDead)
                return;
                
            // Add health and clamp to max
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            
            // Notify health percentage
            onHealthChanged?.Invoke(currentHealth / maxHealth);
        }
        
        private void Die()
        {
            if (isDead)
                return;
                
            isDead = true;
            
            // Trigger death animation
            if (playerAnimator != null)
            {
                playerAnimator.TriggerDeathAnimation();
            }
            
            // Disable player controller
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Trigger death event
            onDeath?.Invoke();
        }
        
        public void Respawn(Vector3 respawnPosition)
        {
            // Only respawn if dead
            if (!isDead)
                return;
                
            // Reset health
            currentHealth = maxHealth;
            
            // Teleport player
            if (playerController != null)
            {
                playerController.TeleportTo(respawnPosition);
                playerController.enabled = true;
            }
            
            // Reset state
            isDead = false;
            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityTime;
            
            // Notify health update
            onHealthChanged?.Invoke(currentHealth / maxHealth);
        }
        
        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }
        
        public bool IsDead()
        {
            return isDead;
        }
    }
} 