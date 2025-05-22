using UnityEngine;

/// <summary>
/// State for Mage's ranged attack.
/// </summary>
public class MageAttackState : State
{
    private Animator animator;
    private MovementController movementController;
    private MageCombatController combatController;
    private float attackTimer;
    private float attackDuration;
    private bool hasTriggeredAttack = false;
    private float interruptibleAfter = 0.7f; // Allow state change only in the last 0.3 seconds
    private Vector3 targetPosition; // Store the target position for the projectile

    public MageAttackState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                         Animator animator, MageCombatController combatController)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.combatController = combatController;
        this.attackDuration = combatController.magicAttackDuration;
    }

    public override void Enter()
    {
        // Mark as uninterruptible
        IsUninterruptible = true;

        // Stop movement while casting
        movementController.SetMoveDirection(Vector3.zero);

        // Reset timer and flags
        attackTimer = 0f;
        hasTriggeredAttack = false;

        // Get target position from mouse
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast to find where the player clicked
        if (Physics.Raycast(ray, out hit))
        {
            targetPosition = hit.point;
        }
        else
        {
            // If no hit, project to a point far away in the ray direction
            targetPosition = ray.origin + ray.direction * 100f;
        }

        // Calculate direction to target (without Y component for level aiming)
        Vector3 horizontalTargetPosition = new Vector3(targetPosition.x, owner.transform.position.y, targetPosition.z);
        Vector3 lookDirection = horizontalTargetPosition - owner.transform.position;

        // Rotate player to face target
        if (lookDirection != Vector3.zero)
        {
            owner.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Start the cast animation
        if (animator != null)
        {
            // Reset the cast time parameter to 0
            animator.SetFloat("CastTime", 0f);
            // Set the trigger for casting
            animator.SetBool("IsCasting", true);
            // Set attack speed multiplier
            animator.SetFloat("AttackSpeedMultiplier", 1.1f);
        }
    }

    public override void Exit()
    {
        // Reset uninterruptible state
        IsUninterruptible = false;

        // Reset animation parameters
        if (animator != null)
        {
            animator.SetBool("IsCasting", false);
            animator.SetFloat("CastTime", 0f);
            animator.SetFloat("AttackSpeedMultiplier", 1f);
        }
    }

    public override void Update()
    {
        // Update timer
        attackTimer += Time.deltaTime;

        // Update CastTime parameter for blend tree animation
        // Normalized value from 0 to 1 representing casting progress
        if (animator != null)
        {
            animator.SetFloat("CastTime", attackTimer / attackDuration);
        }

        // Fire projectile in the last 0.5 seconds of the cast
        float projectileTime = attackDuration - 0.5f;
        if (!hasTriggeredAttack && attackTimer >= projectileTime)
        {
            // Fire the projectile
            FireProjectile();
            hasTriggeredAttack = true;
        }

        // Allow interruption during the later part of the animation
        if (attackTimer >= attackDuration * interruptibleAfter)
        {
            IsUninterruptible = false;
        }

        // Check if attack animation is finished
        if (attackTimer >= attackDuration)
        {
            // Switch back to idle state when attack completes
            stateMachine.ChangeState(new MageIdleState(stateMachine, owner, movementController, animator));
            return;
        }

        // Allow chaining into another attack near the end of the animation if cooldown is ready
        if (attackTimer >= attackDuration * interruptibleAfter &&
            Input.GetMouseButtonDown(0) &&
            combatController.canAttack)
        {
            // Reset to a new attack state
            stateMachine.ChangeState(new MageAttackState(stateMachine, owner, movementController, animator, combatController));
            return;
        }
    }

    /// <summary>
    /// Fire the projectile toward the target position
    /// </summary>
    private void FireProjectile()
    {
        if (combatController.projectilePrefab != null && combatController.projectileSpawnPoint != null)
        {
            // Get the spawn position
            Vector3 spawnPos = combatController.projectileSpawnPoint.position;

            // Create projectile
            GameObject projectile = Object.Instantiate(combatController.projectilePrefab, spawnPos, Quaternion.identity);

            // Get direction to target
            Vector3 direction = (targetPosition - spawnPos).normalized;

            // Add MageProjectile component and initialize
            MageProjectile mageProjectile = projectile.GetComponent<MageProjectile>();
            if (mageProjectile == null)
            {
                mageProjectile = projectile.AddComponent<MageProjectile>();
            }

            // Initialize projectile with damage and direction
            mageProjectile.Initialize(combatController.magicAttackDamage, direction, combatController.projectileSpeed, combatController.enemyLayer);

            // Play any VFX or sound effects for projectile launch
            // You could add special effects here

            Debug.Log($"[MageAttackState] Projectile fired toward {targetPosition}");
        }
        else
        {
            Debug.LogError("[MageAttackState] Cannot spawn projectile - prefab or spawn point is missing");
        }

        // Start cooldown in combat controller
        combatController.StartCoroutine(combatController.AttackCooldown());
    }

    /// <summary>
    /// Custom interruption logic for attack state
    /// </summary>
    public override bool CanBeInterrupted(State newState)
    {
        // Always allow damage and death states to interrupt
        if (newState is MageDamagedState || newState is MageDeathState)
        {
            return true;
        }

        // For other states, use the uninterruptible flag
        return !IsUninterruptible;
    }
}