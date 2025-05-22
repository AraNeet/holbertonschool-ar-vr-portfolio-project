using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main controller for the Barbarian player character.
/// Manages player state transitions, health, and combat.
/// Acts as the central hub for player-related systems.
/// </summary>
public class BrabPlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public float maxHealth = 100f;          // Maximum player health
    public float invulnerabilityTime = 1f;  // Time of invulnerability after taking damage

    [Header("Input Settings")]
    [Tooltip("Set to true to disable keyboard/mouse input (for mobile UI)")]
    public bool disableKeyboardMouseInput = false;

    // Components and references
    private BrabStateMachine stateMachine;      // Manages player states
    private MovementController movementController; // Handles movement
    private BrabCombatController combatController; // Handles combat abilities
    private Animator animator;                  // Controls animations
    private Health health;                      // Manages health and damage

    /// <summary>
    /// Initialize components and states on start
    /// </summary>
    void Start()
    {
        // Get or add the state machine component
        stateMachine = GetComponent<BrabStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<BrabStateMachine>();
        }

        // Get or add the movement controller
        movementController = GetComponent<MovementController>();
        if (movementController == null)
        {
            movementController = gameObject.AddComponent<MovementController>();
        }

        // Get or add the combat controller
        combatController = GetComponent<BrabCombatController>();
        if (combatController == null)
        {
            combatController = gameObject.AddComponent<BrabCombatController>();
        }

        // Get or add the health component
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
            // Set up health component
            health.SetMaxHealth(maxHealth);
        }

        // Subscribe to health events for damage and death handling
        health.OnDamaged.AddListener(OnPlayerDamaged);
        health.OnDeath.AddListener(OnPlayerDeath);

        // Get or add the animator component
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on the player. Adding a new one.");
            animator = gameObject.AddComponent<Animator>();
            // Note: You may need to set up the animator controller after adding it
            // animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Path/To/YourAnimatorController");
        }

        // Set default animator parameters
        if (animator != null)
        {
            // Initialize animation speed parameter
            animator.SetFloat("AttackSpeedMultiplier", 1.0f);
            Debug.Log("[BrabPlayerController] Initialized attack speed multiplier");
        }

        // Ensure player has necessary tags and layers
        gameObject.tag = "Player";

        // Initialize with idle state
        stateMachine.Initialize(new BrabIdleState(stateMachine, gameObject, movementController, animator));
    }

    /// <summary>
    /// Handle player input and state transitions each frame
    /// </summary>
    void Update()
    {
        // Don't process input if the player is dead
        if (health.IsDead)
            return;

        // Skip keyboard/mouse input processing if disabled
        if (disableKeyboardMouseInput)
            return;

        // Only handle combat inputs when in movement states
        // This prevents attack interruption during animations
        if (stateMachine.currentState is BrabIdleState ||
            stateMachine.currentState is BrabWalkState ||
            stateMachine.currentState is BrabRunState)
        {
            // Normal attack (left mouse button)
            if (Input.GetMouseButtonDown(0))
            {
                stateMachine.ChangeState(new BrabAttackState(stateMachine, gameObject, movementController, animator, combatController));
                return;
            }

            // Spin attack (right mouse button)
            if (Input.GetMouseButtonDown(1))
            {
                // Start spin attack (can transition to charge in the state itself)
                stateMachine.ChangeState(new BrabSpinAttackState(stateMachine, gameObject, movementController, animator, combatController));
                return;
            }
        }
    }

    /// <summary>
    /// Event handler for when the player takes damage
    /// Transitions to the damaged state
    /// </summary>
    private void OnPlayerDamaged()
    {
        // Only transition to damaged state if we're not already in a damaged or dead state
        if (!(stateMachine.currentState is BrabDamagedState) &&
            !(stateMachine.currentState is BrabDeathState))
        {
            stateMachine.ChangeState(new BrabDamagedState(stateMachine, gameObject, movementController, animator, health));
        }
    }

    /// <summary>
    /// Event handler for when the player dies
    /// Transitions to the death state
    /// </summary>
    private void OnPlayerDeath()
    {
        stateMachine.ChangeState(new BrabDeathState(stateMachine, gameObject, movementController, animator, health));
    }

    /// <summary>
    /// Handle collisions with physical colliders
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Player] OnCollisionEnter with {collision.gameObject.name}, tag: {collision.gameObject.tag}");
        HandleEnemyCollision(collision.gameObject);
    }

    /// <summary>
    /// Handle collisions with trigger colliders (like enemy attack hitboxes)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Player] OnTriggerEnter with {other.gameObject.name}, tag: {other.gameObject.tag}");
        HandleEnemyCollision(other.gameObject);
    }

    /// <summary>
    /// Process enemy collision and take damage if applicable
    /// </summary>
    /// <param name="collider">The GameObject that collided with the player</param>
    private void HandleEnemyCollision(GameObject collider)
    {
        // Check if it's an enemy or an enemy hitbox
        if (collider.CompareTag("Enemy") || collider.CompareTag("EnemyHitbox"))
        {
            Debug.Log($"[Player] Detected enemy/hitbox collision with {collider.name}");

            // Get enemy attack damage - can be a fixed value or from an EnemyAttack component
            float damageAmount = 10f; // Default damage amount

            // Try to get a custom damage amount if the enemy has an attack component
            var enemyAttack = collider.GetComponent<EnemyAttack>();
            if (enemyAttack == null && collider.transform.parent != null)
            {
                // If the hitbox doesn't have the component, check its parent
                enemyAttack = collider.transform.parent.GetComponent<EnemyAttack>();
            }

            if (enemyAttack != null)
            {
                // Only take damage if the enemy is in attack mode and can deal damage
                if (enemyAttack.CanDealDamage())
                {
                    damageAmount = enemyAttack.attackDamage;
                    Debug.Log($"[Player] Taking {damageAmount} damage from {collider.name}");

                    // Apply damage to player
                    health.TakeDamage(damageAmount);
                }
                else
                {
                    Debug.Log($"[Player] Enemy {collider.name} hit ignored - not in damage dealing phase");
                }
            }
            else
            {
                // If no EnemyAttack component, apply default damage
                Debug.Log($"[Player] Taking default {damageAmount} damage from {collider.name} (no EnemyAttack component)");
                health.TakeDamage(damageAmount);
            }
        }
    }

    /// <summary>
    /// Clean up event subscriptions when object is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (health != null)
        {
            if (health.OnDamaged != null)
                health.OnDamaged.RemoveListener(OnPlayerDamaged);

            if (health.OnDeath != null)
                health.OnDeath.RemoveListener(OnPlayerDeath);
        }
    }
}
