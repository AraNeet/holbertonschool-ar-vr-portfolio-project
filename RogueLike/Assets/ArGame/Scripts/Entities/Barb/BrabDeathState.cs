using UnityEngine;

/// <summary>
/// State for when the Barb player dies.
/// Handles death animation, disables controllers, and triggers game over.
/// </summary>
public class BrabDeathState : State
{
    private Animator animator;                    // Reference to the animator component
    private MovementController movementController; // Reference to movement controller
    private Health health;                        // Reference to the health component
    private float deathStateTime = 3f;            // Time to show death animation before game over
    private float timer;                          // Tracks elapsed time in this state

    /// <summary>
    /// Initialize the death state with required components
    /// </summary>
    /// <param name="stateMachine">Reference to state machine</param>
    /// <param name="owner">GameObject that owns this state</param>
    /// <param name="movementController">Movement controller reference</param>
    /// <param name="animator">Animator component reference</param>
    /// <param name="health">Health component reference</param>
    public BrabDeathState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                        Animator animator, Health health)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.health = health;
        this.timer = 0f;
    }

    /// <summary>
    /// Called when entering the death state.
    /// Stops movement, plays death animation, and disables components.
    /// </summary>
    public override void Enter()
    {
        // Stop all movement
        movementController.SetMoveDirection(Vector3.zero);

        // Disable character controller to allow ragdoll physics if implemented
        CharacterController characterController = owner.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Play death animation
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetBool("IsIdle", false);
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsDamaged", false);
        }

        // Reset timer
        timer = 0f;

        // Disable the combat controller to prevent attacking while dead
        BrabCombatController combatController = owner.GetComponent<BrabCombatController>();
        if (combatController != null)
        {
            combatController.enabled = false;
        }

        // Disable the movement controller to prevent movement while dead
        if (movementController != null)
        {
            movementController.enabled = false;
        }

        // Call game over UI or respawn logic through the GameManager
        GameManager.Instance?.PlayerDied();
    }

    /// <summary>
    /// Update is called every frame.
    /// Maintains the death state until the game is reset.
    /// </summary>
    public override void Update()
    {
        timer += Time.deltaTime;

        // After death animation, implement game over logic or respawn
        if (timer >= deathStateTime)
        {
            // If a GameManager exists, it will handle the game over or respawn
            // This state persists until game is reset
        }
    }
}