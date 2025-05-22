using UnityEngine;

/// <summary>
/// State for Mage's damage reaction.
/// </summary>
public class MageDamagedState : State
{
    private Animator animator;
    private MovementController movementController;
    private Health health;
    private float damagedStateTime = 0.5f;
    private float stateTimer = 0f;
    private float knockbackDuration = 0.2f;
    private float knockbackTimer = 0f;
    private Vector3 knockbackDirection;

    public MageDamagedState(StateMachine stateMachine, GameObject owner, MovementController movementController,
                          Animator animator, Health health)
        : base(stateMachine, owner)
    {
        this.animator = animator;
        this.movementController = movementController;
        this.health = health;
    }

    public override void Enter()
    {
        // Stop movement
        movementController.SetMoveDirection(Vector3.zero);

        // Set animation
        if (animator != null)
        {
            animator.SetTrigger("Damaged");
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
        }

        // Reset timer
        stateTimer = 0f;
        knockbackTimer = 0f;

        // Calculate knockback direction
        CalculateKnockbackDirection();
    }

    public override void Exit()
    {
        // Ensure movement is stopped
        movementController.SetMoveDirection(Vector3.zero);
    }

    public override void Update()
    {
        // Apply knockback if timer is still active
        if (knockbackTimer < knockbackDuration)
        {
            knockbackTimer += Time.deltaTime;

            // Apply knockback using SetMoveDirection
            // Scale the knockback to fade out over time
            float knockbackStrength = Mathf.Lerp(5f, 0f, knockbackTimer / knockbackDuration);
            movementController.SetMoveDirection(knockbackDirection * knockbackStrength);
        }
        else if (stateTimer < damagedStateTime)
        {
            // After knockback ends, ensure movement is stopped for the rest of damaged state
            movementController.SetMoveDirection(Vector3.zero);
        }

        // Update state timer
        stateTimer += Time.deltaTime;

        // Check if damage state duration has elapsed
        if (stateTimer >= damagedStateTime)
        {
            // Return to idle when damage reaction is complete
            stateMachine.ChangeState(new MageIdleState(stateMachine, owner, movementController, animator));
        }
    }

    /// <summary>
    /// Calculate the knockback direction away from nearest enemy
    /// </summary>
    private void CalculateKnockbackDirection()
    {
        // Get nearest enemy as a potential source of damage
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(owner.transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        // Calculate knockback direction (away from enemy or random if no enemy found)
        if (nearestEnemy != null)
        {
            knockbackDirection = (owner.transform.position - nearestEnemy.transform.position).normalized;
        }
        else
        {
            // Random horizontal direction if no enemy found
            knockbackDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        }

        Debug.Log($"[MageDamagedState] Applied knockback in direction: {knockbackDirection}");
    }
}