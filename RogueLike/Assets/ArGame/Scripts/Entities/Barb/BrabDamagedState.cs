using UnityEngine;

/// <summary>
/// State for when the Barb player takes damage.
/// </summary>
public class BrabDamagedState : State
{
    private Animator animator;
    private MovementController movementController;
    private Health health;
    private BrabCombatController combatController;
    private float damageStateTime = 0.5f;
    private float exitTransitionTime = 0.2f; // Time at the end of the animation when exit is allowed
    private float timer;

    public BrabDamagedState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                          Animator animator, Health health)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.health = health;
        this.timer = 0f;

        // Get the combat controller to cancel attacks
        this.combatController = owner.GetComponent<BrabCombatController>();

        // Damage state should be uninterruptible except by death
        this.IsUninterruptible = true;
    }

    public override void Enter()
    {
        // Stop movement
        movementController.SetMoveDirection(Vector3.zero);

        // Cancel any ongoing attacks
        if (combatController != null)
        {
            combatController.CancelAttacks();
        }

        // Play hit animation
        if (animator != null)
        {
            animator.SetBool("IsDamaged", true);
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);

            // Speed up animation transition
            animator.SetFloat("DamageSpeedMultiplier", 1.2f);
        }

        // Reset timer
        timer = 0f;
    }

    public override void Exit()
    {
        // Reset animation parameter
        if (animator != null)
        {
            animator.SetBool("IsDamaged", false);
            animator.SetFloat("DamageSpeedMultiplier", 1.0f);
        }

        // Reset uninterruptible flag
        IsUninterruptible = false;
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        // If player is dead, change to death state immediately
        if (health.IsDead)
        {
            stateMachine.ChangeState(new BrabDeathState(stateMachine, owner, movementController, animator, health));
            return;
        }

        // Only automatically exit the damaged state if it's fully completed
        if (timer >= damageStateTime)
        {
            // Return to idle state
            stateMachine.ChangeState(new BrabIdleState(stateMachine, owner, movementController, animator));
        }
    }

    /// <summary>
    /// Custom interruption logic for damaged state
    /// </summary>
    public override bool CanBeInterrupted(State newState)
    {
        // Only allow death state to interrupt damage state at any time
        if (newState is BrabDeathState)
        {
            return true;
        }

        // Allow other state transitions only in the last 0.2 seconds
        if (timer >= damageStateTime - exitTransitionTime)
        {
            return true;
        }

        // Don't allow any other interruptions
        return false;
    }
}