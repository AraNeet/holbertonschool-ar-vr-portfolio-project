using UnityEngine;

/// <summary>
/// State for Barb's normal attack.
/// </summary>
public class BrabAttackState : State
{
    private Animator animator;
    private MovementController movementController;
    private BrabCombatController combatController;
    private float attackTimer;
    private float attackDuration;
    private float interruptibleAfter = 0.8f; // Allow state change only in the last 0.2 seconds

    public BrabAttackState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                         Animator animator, BrabCombatController combatController)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.combatController = combatController;
        this.attackDuration = combatController.normalAttackDuration;
    }

    public override void Enter()
    {
        // Mark as uninterruptible
        IsUninterruptible = true;

        // Stop movement while attacking
        movementController.SetMoveDirection(Vector3.zero);

        // Reset timer
        attackTimer = 0f;
        
        // Set hitbox to activate immediately
        if (combatController != null)
        {
            combatController.hitboxDelay = 0f;
            combatController.hitboxActiveFraction = 0.8f;
        }

        // Apply damage immediately via the combat controller
        // Let the combat controller handle the animation flag
        combatController.PerformNormalAttack();

        // Speed up animation transition
        if (animator != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", 1.2f); // Slightly faster attack animation
        }
    }

    public override void Exit()
    {
        // Don't reset animation parameters here to avoid conflicts
        // The combat controller will handle this via its coroutine

        // Reset uninterruptible state
        IsUninterruptible = false;

        // Reset animation speed
        if (animator != null)
        {
            animator.SetFloat("AttackSpeedMultiplier", 1f);
        }
    }

    public override void Update()
    {
        // Update timer
        attackTimer += Time.deltaTime;

        // Allow interruption during the later part of the animation
        if (attackTimer >= attackDuration * interruptibleAfter)
        {
            IsUninterruptible = false;
        }

        // Check if attack animation is finished
        if (attackTimer >= attackDuration)
        {
            // Switch back to idle state when attack completes
            stateMachine.ChangeState(new BrabIdleState(stateMachine, owner, movementController, animator));
            return;
        }

        // Allow chaining into another attack near the end of the animation
        if (attackTimer >= attackDuration * interruptibleAfter && Input.GetMouseButtonDown(0))
        {
            // Reset to a new attack state (combo system)
            stateMachine.ChangeState(new BrabAttackState(stateMachine, owner, movementController, animator, combatController));
            return;
        }

        // Allow transitioning to spin attack
        if (attackTimer >= attackDuration * interruptibleAfter && Input.GetMouseButtonDown(1))
        {
            stateMachine.ChangeState(new BrabSpinAttackState(stateMachine, owner, movementController, animator, combatController));
            return;
        }
    }

    /// <summary>
    /// Custom interruption logic for attack state
    /// </summary>
    public override bool CanBeInterrupted(State newState)
    {
        // Always allow transitions to other attack states
        if (newState is BrabAttackState || newState is BrabSpinAttackState || newState is BrabChargeSpinAttackState)
        {
            return true;
        }

        // Always allow damage and death states to interrupt
        if (newState is BrabDamagedState || newState is BrabDeathState)
        {
            return true;
        }

        // For other states, use the uninterruptible flag
        return !IsUninterruptible;
    }
}