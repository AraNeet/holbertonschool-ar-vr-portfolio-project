using UnityEngine;

/// <summary>
/// State for Mage's death behavior.
/// </summary>
public class MageDeathState : State
{
    private Animator animator;
    private MovementController movementController;
    private Health health;
    private bool deathHandled = false;

    public MageDeathState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                        Animator animator, Health health)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.health = health;
        this.IsUninterruptible = true; // Death state cannot be interrupted
    }

    public override void Enter()
    {
        // Stop all movement
        movementController.SetMoveDirection(Vector3.zero);
        movementController.enabled = false;

        // Set death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
            animator.SetBool("IsDead", true);
        }

        // Disable collisions
        Collider collider = owner.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Handle death logic
        if (!deathHandled)
        {
            deathHandled = true;
            Debug.Log("[MageDeathState] Player has died!");

            // Look for a game manager but handle death locally if not found
            GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                // Try to use reflection to find the method
                System.Type gameManagerType = gameManager.GetType();
                System.Reflection.MethodInfo methodInfo = gameManagerType.GetMethod("OnPlayerDeath");

                if (methodInfo != null)
                {
                    methodInfo.Invoke(gameManager, null);
                }
                else
                {
                    // Fallback handling if method doesn't exist
                    HandlePlayerDeathLocally();
                }
            }
            else
            {
                Debug.LogWarning("[MageDeathState] No GameManager found to handle player death!");
                HandlePlayerDeathLocally();
            }
        }
    }

    /// <summary>
    /// Local handling of player death when no GameManager is available
    /// </summary>
    private void HandlePlayerDeathLocally()
    {
        Debug.Log("[MageDeathState] Handling player death locally");

        // You could implement a respawn timer here
        // StartCoroutine(RespawnAfterDelay(3f));

        // Or display a game over message
        GameObject.FindObjectOfType<Canvas>()?.transform.Find("GameOverPanel")?.gameObject.SetActive(true);
    }

    public override void Exit()
    {
        // This state shouldn't exit normally, but if it does:
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }

        // Re-enable components that were disabled
        movementController.enabled = true;

        Collider collider = owner.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    public override void Update()
    {
        // Death state persists until game manager handles it
        // No transitions out of this state from Update
    }

    public override bool CanBeInterrupted(State newState)
    {
        // Death state cannot be interrupted by any other state
        return false;
    }
}