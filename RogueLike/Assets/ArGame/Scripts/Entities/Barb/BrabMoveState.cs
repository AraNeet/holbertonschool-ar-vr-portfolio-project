using UnityEngine;

/// <summary>
/// State for handling Barbarian movement (player-controlled).
/// </summary>
public class BrabMoveState : State
{
    private MovementController movementController;

    public BrabMoveState(StateMachine stateMachine, GameObject owner, MovementController movementController)
        : base(stateMachine, owner)
    {
        this.movementController = movementController;
    }

    public override void Update()
    {
        // Read input (WASD/Arrow keys)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // Convert input to world space (relative to camera if needed)
        movementController.SetMoveDirection(inputDirection);
    }
}