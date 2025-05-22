using System.Collections;
using UnityEngine;

/// <summary>
/// Component for enemy attack properties and collision detection.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;
    [Tooltip("Range of the attack in units. This also determines the hitbox size.")]
    public float attackRange = 1.5f;
    public float attackDuration = 0.8f;     // Duration of attack animation
    
    [Tooltip("Delay in seconds before attack hitbox activates (0 = immediate)")]
    public float hitboxDelay = 0f;          // Changed from hardcoded 0.2f to configurable with default 0
    
    [Tooltip("Duration in seconds the hitbox stays active")]
    public float hitboxDuration = 0.5f;     // Was hardcoded, now configurable

    [Header("Attack Effects")]
    public GameObject hitEffectPrefab;
    public AudioClip attackSound;

    private float nextAttackTime = 0f;
    private AudioSource audioSource;
    private bool isAttacking = false;
    private bool canDealDamage = false;     // Flag to indicate if attack can deal damage
    private Collider hitboxCollider;        // Optional collider used as a hitbox
    private Transform hitboxTransform;      // Reference to the hitbox transform

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && attackSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Try to find a child GameObject that acts as a hitbox
        hitboxTransform = transform.Find("Hitbox");
        if (hitboxTransform != null)
        {
            hitboxCollider = hitboxTransform.GetComponent<Collider>();
            if (hitboxCollider != null)
            {
                // Disable the hitbox collider by default
                hitboxCollider.enabled = false;

                // Make sure the hitbox has the EnemyHitbox tag
                hitboxTransform.gameObject.tag = "EnemyHitbox";
                
                // Update hitbox size to match attack range
                UpdateHitboxSize();
            }
        }
        // If no hitbox found, try to create one
        else
        {
            // Create a dedicated attack range object
            hitboxTransform = new GameObject("Hitbox").transform;
            hitboxTransform.SetParent(transform);
            hitboxTransform.localPosition = Vector3.forward * (attackRange / 2f); // Position in front of enemy

            // Add a trigger collider for the hitbox
            SphereCollider sphereCollider = hitboxTransform.gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = attackRange / 2f; // Set radius to half the attack range
            sphereCollider.isTrigger = true;

            // Set the tag
            hitboxTransform.gameObject.tag = "EnemyHitbox";

            // Store reference
            hitboxCollider = sphereCollider;
            hitboxCollider.enabled = false;

            Debug.Log($"[EnemyAttack] Created hitbox for {gameObject.name} with radius {attackRange / 2f}");
        }
    }
    
    private void Start()
    {
        // Get attackRange from a controller if available
        var skeletonController = GetComponent<SkeletonWarriorController>();
        if (skeletonController != null && skeletonController.attackRange != attackRange)
        {
            attackRange = skeletonController.attackRange;
            UpdateHitboxSize();
            Debug.Log($"[EnemyAttack] Updated hitbox size from SkeletonWarriorController: {attackRange}");
        }
    }
    
    private void OnValidate()
    {
        // Update hitbox size when attackRange is changed in the inspector
        if (hitboxCollider != null)
        {
            UpdateHitboxSize();
        }
    }
    
    /// <summary>
    /// Updates the hitbox size to match the current attackRange
    /// </summary>
    private void UpdateHitboxSize()
    {
        if (hitboxCollider == null) return;
        
        if (hitboxCollider is SphereCollider)
        {
            SphereCollider sphere = hitboxCollider as SphereCollider;
            sphere.radius = attackRange / 2f;
            
            // Position the hitbox in front of the enemy at half the attack range
            if (hitboxTransform != null)
            {
                hitboxTransform.localPosition = Vector3.forward * (attackRange / 2f);
            }
        }
        else if (hitboxCollider is BoxCollider)
        {
            BoxCollider box = hitboxCollider as BoxCollider;
            box.size = new Vector3(attackRange, attackRange / 2f, attackRange);
            
            // Position the hitbox in front of the enemy at half the attack range
            if (hitboxTransform != null)
            {
                hitboxTransform.localPosition = Vector3.forward * (attackRange / 2f);
            }
        }
        else if (hitboxCollider is CapsuleCollider)
        {
            CapsuleCollider capsule = hitboxCollider as CapsuleCollider;
            capsule.radius = attackRange / 2f;
            capsule.height = attackRange;
            
            // Position the hitbox in front of the enemy
            if (hitboxTransform != null)
            {
                hitboxTransform.localPosition = Vector3.forward * (attackRange / 2f);
            }
        }
    }

    /// <summary>
    /// Checks if the enemy can attack based on cooldown.
    /// </summary>
    public bool CanAttack()
    {
        return Time.time >= nextAttackTime && !isAttacking;
    }

    /// <summary>
    /// Checks if the enemy is currently in a state where it can deal damage
    /// </summary>
    public bool CanDealDamage()
    {
        return canDealDamage;
    }

    /// <summary>
    /// Attempts to attack a target if in range and off cooldown.
    /// </summary>
    public bool TryAttack(GameObject target)
    {
        if (!CanAttack())
            return false;

        if (target == null)
            return false;

        // Check if target is within range
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance <= attackRange)
        {
            // Attack successful
            nextAttackTime = Time.time + attackCooldown;
            isAttacking = true;

            // Start attack sequence with delayed damage
            StartCoroutine(AttackSequence(target));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles the attack sequence with proper timing
    /// </summary>
    private IEnumerator AttackSequence(GameObject target)
    {
        // Play attack sound
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Wait for hitboxDelay seconds before enabling damage (can be 0 for immediate activation)
        if (hitboxDelay > 0)
        {
            yield return new WaitForSeconds(hitboxDelay);
        }

        // Enable damage dealing
        canDealDamage = true;

        // Enable hitbox collider if we have one
        if (hitboxCollider != null)
        {
            // Make sure hitbox size is up-to-date
            UpdateHitboxSize();
            
            hitboxCollider.enabled = true;
            Debug.Log($"[EnemyAttack] Enabled hitbox collider on {gameObject.name} with size {attackRange}");
        }
        else
        {
            // Apply direct damage if the target is still valid and in range
            if (target != null)
            {
                float currentDistance = Vector3.Distance(transform.position, target.transform.position);
                if (currentDistance <= attackRange)
                {
                    // Apply damage to target if it has a Health component
                    Health targetHealth = target.GetComponent<Health>();
                    if (targetHealth != null)
                    {
                        Debug.Log($"[EnemyAttack] Direct hit on player: {target.name}, damage: {attackDamage}");
                        targetHealth.TakeDamage(attackDamage);

                        // Spawn hit effect
                        if (hitEffectPrefab != null)
                        {
                            Instantiate(hitEffectPrefab, target.transform.position, Quaternion.identity);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyAttack] Target {target.name} has no Health component!");
                    }
                }
            }
        }

        // Keep the attack hitbox active for the specified duration
        yield return new WaitForSeconds(hitboxDuration);

        // Disable damage dealing
        canDealDamage = false;

        // Disable hitbox collider if we have one
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
            Debug.Log($"[EnemyAttack] Disabled hitbox collider on {gameObject.name}");
        }

        // Finish the rest of the attack animation
        float remainingTime = attackDuration - hitboxDelay - hitboxDuration;
        if (remainingTime > 0)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        // End the attack state
        isAttacking = false;
    }

    /// <summary>
    /// Called when this enemy's attack hitbox collides with something.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Only apply damage during the damage-dealing phase
        if (!canDealDamage)
        {
            Debug.Log($"[EnemyAttack] Hit ignored - canDealDamage is false. Hit: {other.name}");
            return;
        }

        Debug.Log($"[EnemyAttack] OnTriggerEnter with {other.name}, tag: {other.tag}");

        // Check if we hit the player
        if (other.CompareTag("Player"))
        {
            // Get player's health component
            Health playerHealth = other.GetComponent<Health>();
            if (playerHealth == null)
            {
                // Try to get Health from parent
                playerHealth = other.GetComponentInParent<Health>();
            }

            if (playerHealth != null)
            {
                Debug.Log($"[EnemyAttack] Hit player: {other.name}, applying {attackDamage} damage");

                // Apply damage
                playerHealth.TakeDamage(attackDamage);

                // Play attack sound
                if (audioSource != null && attackSound != null)
                {
                    audioSource.PlayOneShot(attackSound);
                }

                // Spawn hit effect
                if (hitEffectPrefab != null)
                {
                    Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity);
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyAttack] Player hit but no Health component found on {other.name} or parent!");
            }
        }
    }

    /// <summary>
    /// Cancel the current attack (used when enemy is damaged)
    /// </summary>
    public void CancelAttack()
    {
        isAttacking = false;
        canDealDamage = false;

        // Disable hitbox collider if we have one
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        // Stop all coroutines to prevent any pending attack sequences
        StopAllCoroutines();
    }

    /// <summary>
    /// Draw attack range gizmo in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw hitbox if it exists
        if (hitboxTransform != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            if (hitboxCollider is SphereCollider)
            {
                SphereCollider sphere = hitboxCollider as SphereCollider;
                Gizmos.DrawSphere(hitboxTransform.position, sphere.radius);
            }
            else if (hitboxCollider is BoxCollider)
            {
                BoxCollider box = hitboxCollider as BoxCollider;
                Gizmos.matrix = hitboxTransform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}