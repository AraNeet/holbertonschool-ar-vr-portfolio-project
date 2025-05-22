using UnityEngine;

/// <summary>
/// State for Barb's full charge spin attack.
/// </summary>
public class BrabChargeSpinAttackState : State
{
    private Animator animator;
    private MovementController movementController;
    private BrabCombatController combatController;
    private float attackTimer;
    private float attackDuration;
    private bool canMove = false;
    private float movementStartTime = 0.5f;     // When player can start moving (0.5s into animation)
    private float movementEndTime = 3.5f;       // When player must stop moving (0.5s before end)
    private float interruptibleAfter = 3.5f;    // Allow interruption only in the last 0.5 seconds
    private Vector3 moveDirection = Vector3.zero;
    private bool rightMouseHeld = false;

    // Animation control variables
    private float singleAnimationLength = 1.2f; // Estimated length of a single spin animation cycle
    private float animationTimer = 0f;          // Timer for controlling animation loops

    public BrabChargeSpinAttackState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                         Animator animator, BrabCombatController combatController)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.combatController = combatController;
        this.attackDuration = combatController.chargeSpinAttackDuration;
    }

    public override void Enter()
    {
        // Mark as uninterruptible except during final phase
        IsUninterruptible = true;

        // Stop movement initially
        movementController.SetMoveDirection(Vector3.zero);
        canMove = false;

        // Reset timers
        attackTimer = 0f;
        animationTimer = 0f;
        
        // Set hitbox to activate immediately
        if (combatController != null)
        {
            combatController.hitboxDelay = 0f;
            // Let the charge spin attack have a longer hitbox duration 
            // since it's active through the entire attack
        }

        // Let combat controller handle the attack
        combatController.PerformChargeSpinAttack();

        // Track mouse button state
        rightMouseHeld = Input.GetMouseButton(1);
    }

    public override void Exit()
    {
        // Reset all movement and animation states
        movementController.SetMoveDirection(Vector3.zero);
        canMove = false;

        // Don't reset animation flags here - the combat controller will handle this

        // Reset uninterruptible state
        IsUninterruptible = false;
    }

    public override void Update()
    {
        // Update timer
        attackTimer += Time.deltaTime;
        animationTimer += Time.deltaTime;

        // Reset animation timer when it exceeds a single cycle
        if (animationTimer > singleAnimationLength)
        {
            animationTimer -= singleAnimationLength;
        }

        // Update movement ability based on attack phase
        UpdateMovement();

        // Allow interruption during final phase
        if (attackTimer >= interruptibleAfter)
        {
            IsUninterruptible = false;
        }

        // Check if attack is finished
        if (attackTimer >= attackDuration)
        {
            // Return to idle state
            stateMachine.ChangeState(new BrabIdleState(stateMachine, owner, movementController, animator));
            return;
        }

        // Early cancellation if player releases right mouse button
        if (!Input.GetMouseButton(1) && rightMouseHeld)
        {
            rightMouseHeld = false;
            if (attackTimer < interruptibleAfter) // Only allow early cancellation before final phase
            {
                stateMachine.ChangeState(new BrabIdleState(stateMachine, owner, movementController, animator));
                return;
            }
        }
    }

    /// <summary>
    /// Update player movement based on attack phase
    /// </summary>
    private void UpdateMovement()
    {
        // Enable movement during the middle phase
        if (attackTimer >= movementStartTime && attackTimer < movementEndTime)
        {
            if (!canMove)
            {
                canMove = true;
                Debug.Log("[ChargeSpinAttack] Movement enabled");
            }

            // Get movement input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Calculate movement direction
            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Apply movement at a reduced speed (70% of normal)
            // Scale the direction vector to reduce speed
            movementController.SetMoveDirection(direction * 0.7f);
        }
        else
        {
            // Disable movement during start and end phases
            if (canMove)
            {
                canMove = false;
                movementController.SetMoveDirection(Vector3.zero);
                Debug.Log("[ChargeSpinAttack] Movement disabled");
            }
        }
    }

    /// <summary>
    /// Custom interruption logic for charge spin attack
    /// </summary>
    public override bool CanBeInterrupted(State newState)
    {
        // Always allow damage and death states to interrupt
        if (newState is BrabDamagedState || newState is BrabDeathState)
        {
            return true;
        }

        // For other states, use the uninterruptible flag
        return !IsUninterruptible;
    }
}