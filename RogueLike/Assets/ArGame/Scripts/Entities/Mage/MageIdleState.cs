using UnityEngine;

/// <summary>
/// State for Mage's idle behavior.
/// </summary>
public class MageIdleState : State
{
    private Animator animator;
    private MovementController movementController;

    public MageIdleState(StateMachine stateMachine, GameObject owner, MovementController movementController, Animator animator)
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
            animator.SetBool("IsRunning", false);
        }

        // Stop movement
        movementController.SetMoveDirection(Vector3.zero);
    }

    public override void Exit()
    {
        // No cleanup needed
    }

    public override void Update()
    {
        // Check for keyboard input
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

        // If movement detected (from keyboard or UI), transition to appropriate state
        if (inputDirection.magnitude > 0.01f)
        {
            // Check for sprint
            if (sprint)
            {
                stateMachine.ChangeState(new MageRunState(stateMachine, owner, movementController, animator));
            }
            else
            {
                stateMachine.ChangeState(new MageWalkState(stateMachine, owner, movementController, animator));
            }
            return;
        }
    }
}