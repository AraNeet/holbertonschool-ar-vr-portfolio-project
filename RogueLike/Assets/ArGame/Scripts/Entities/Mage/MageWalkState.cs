using UnityEngine;

/// <summary>
/// State for Mage's walking behavior.
/// </summary>
public class MageWalkState : State
{
    private Animator animator;
    private MovementController movementController;
    private float walkSpeed = 3f;

    public MageWalkState(StateMachine stateMachine, GameObject owner, MovementController movementController, Animator animator)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
    }

    public override void Enter()
    {
        // Set animation
        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsRunning", false);
        }

        // Set movement speed by directly accessing the public field
        if (movementController != null)
        {
            // Store original speed value in case we need to restore it later
            // Set the movement speed property directly
            movementController.moveSpeed = walkSpeed;
        }
    }

    public override void Exit()
    {
        // No specific cleanup needed
    }

    public override void Update()
    {
        // Get keyboard input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // Check if movement is coming from UI
        if (inputDirection.magnitude <= 0.01f && movementController.GetMoveDirection().magnitude > 0.01f)
        {
            // UI is controlling movement
            inputDirection = movementController.GetMoveDirection();
            // Check if UI sprint is active
            sprint = movementController.IsSprinting();
        }
        else
        {
            // Update movement direction in the controller (keyboard input)
            movementController.SetMoveDirection(inputDirection);
        }

        // No movement - return to idle
        if (inputDirection.magnitude <= 0.01f)
        {
            stateMachine.ChangeState(new MageIdleState(stateMachine, owner, movementController, animator));
            return;
        }

        // Sprint activated - transition to run
        if (sprint && inputDirection.magnitude > 0.01f)
        {
            stateMachine.ChangeState(new MageRunState(stateMachine, owner, movementController, animator));
            return;
        }
    }
}