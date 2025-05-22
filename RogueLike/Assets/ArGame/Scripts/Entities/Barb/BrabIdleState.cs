using UnityEngine;

/// <summary>
/// Idle state for the Barbarian character.
/// Handles player input to transition to movement states.
/// </summary>
public class BrabIdleState : State
{
    private MovementController movementController; // Reference to the movement controller
    private Animator animator;                    // Reference to the animator component

    /// <summary>
    /// Initialize the idle state with required components
    /// </summary>
    /// <param name="stateMachine">Reference to state machine</param>
    /// <param name="owner">GameObject that owns this state</param>
    /// <param name="movementController">Movement controller reference</param>
    /// <param name="animator">Animator component reference</param>
    public BrabIdleState(StateMachine stateMachine, GameObject owner, MovementController movementController, Animator animator)
        : base(stateMachine, owner)
    {
        this.movementController = movementController;
        this.animator = animator;
    }

    /// <summary>
    /// Called when entering the idle state.
    /// Stops movement and plays idle animation.
    /// </summary>
    public override void Enter()
    {
        // Stop any movement
        movementController.SetMoveDirection(Vector3.zero);

        // Set animation parameters
        if (animator != null)
        {
            animator.SetBool("IsIdle", true);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
        }
    }

    /// <summary>
    /// Called when exiting the idle state.
    /// Resets animation parameters.
    /// </summary>
    public override void Exit()
    {
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
        }
    }

    /// <summary>
    /// Update is called every frame.
    /// Checks for player movement input to transition to walk or run states.
    /// </summary>
    public override void Update()
    {
        // Get movement input from keyboard
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);

        // Create movement direction vector and normalize it
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // Check if movement is coming from UI controls by checking the MovementController
        if (inputDirection.magnitude <= 0.01f && movementController.GetMoveDirection().magnitude > 0.01f)
        {
            // UI is controlling movement - use that direction instead
            inputDirection = movementController.GetMoveDirection();
            // Check if UI sprint is active
            sprint = movementController.IsSprinting();
        }

        // Check if there is any movement input (from keyboard or UI)
        if (inputDirection.magnitude > 0.01f)
        {
            // Transition to run state if sprint key is pressed or UI sprint is active, otherwise to walk state
            if (sprint)
                stateMachine.ChangeState(new BrabRunState(stateMachine, owner, movementController, animator));
            else
                stateMachine.ChangeState(new BrabWalkState(stateMachine, owner, movementController, animator));
        }
    }
}