using UnityEngine;

public class BrabRunState : State
{
    private MovementController movementController;
    private Animator animator;

    public BrabRunState(StateMachine stateMachine, GameObject owner, MovementController movementController, Animator animator)
        : base(stateMachine, owner)
    {
        this.movementController = movementController;
        this.animator = animator;
    }

    public override void Enter()
    {
        if (animator != null)
        {
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", true);
        }
    }

    public override void Update()
    {
        // Check keyboard input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);
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
            stateMachine.ChangeState(new BrabIdleState(stateMachine, owner, movementController, animator));
            return;
        }

        // Sprint deactivated - transition to walk
        if (!sprint)
        {
            stateMachine.ChangeState(new BrabWalkState(stateMachine, owner, movementController, animator));
            return;
        }
    }
}