using UnityEngine;

/// <summary>
/// State for Mage's running behavior.
/// </summary>
public class MageRunState : State
{
    private Animator animator;
    private MovementController movementController;
    private float runSpeed = 5f;

    public MageRunState(StateMachine stateMachine, GameObject owner, MovementController movementController, Animator animator)
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
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", true);
        }

        // Set movement speed by directly accessing the public field
        if (movementController != null)
        {
            // Set the movement speed property directly
            movementController.moveSpeed = runSpeed;
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

        // Sprint deactivated - transition to walk
        if (!sprint && inputDirection.magnitude > 0.01f)
        {
            stateMachine.ChangeState(new MageWalkState(stateMachine, owner, movementController, animator));
            return;
        }
    }
}