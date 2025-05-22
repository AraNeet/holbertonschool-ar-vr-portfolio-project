using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles combat abilities for the Mage character.
/// Manages ranged attacks, projectiles, and damage.
/// </summary>
public class MageCombatController : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Should match the exact duration of the magic attack animation clip")]
    public float magicAttackDuration = 0.8f;     // Duration of magic attack animation
    public float magicAttackCooldown = 1.0f;     // Cooldown time between attacks
    public float projectileSpeed = 15f;          // Speed of magic projectile

    [Header("Damage Settings")]
    public float magicAttackDamage = 15f;        // Damage for magic attack

    [Header("References")]
    public LayerMask enemyLayer;                 // Layer mask for enemy detection
    public GameObject projectilePrefab;          // Prefab for magic projectile
    public Transform projectileSpawnPoint;       // Where the projectile spawns from

    [Header("Auto-Targeting")]
    public float autoTargetRange = 20f;          // Maximum range to detect enemies for auto-targeting
    public bool showTargetingGizmos = false;     // Debug option to visualize the targeting range

    // Component references
    private Animator animator;                   // Reference to animator component
    private MageStateMachine stateMachine;       // Reference to state machine
    private Camera mainCamera;                   // Reference to main camera for raycasting

    // Attack state flags
    private bool isAttacking = false;            // Currently performing attack
    private bool _canAttack = true;              // Attack is off cooldown

    // Public property to check if attack is available
    public bool canAttack { get { return _canAttack; } }

    /// <summary>
    /// Initialize component references
    /// </summary>
    private void Awake()
    {
        animator = GetComponent<Animator>();
        stateMachine = GetComponent<MageStateMachine>();
        mainCamera = Camera.main;

        // Validate that enemyLayer is set and includes the Enemy layer
        if (enemyLayer.value == 0 || ((1 << LayerMask.NameToLayer("Enemy")) & enemyLayer.value) == 0)
        {
            Debug.LogWarning("Enemy layer mask not configured correctly. Auto-assigning to Enemy layer.");
            enemyLayer = LayerMask.GetMask("Enemy");
            Debug.Log($"Updated enemyLayer value: {enemyLayer.value}");
        }

        // Create spawn point if not assigned
        if (projectileSpawnPoint == null)
        {
            GameObject spawnPointObj = new GameObject("ProjectileSpawnPoint");
            spawnPointObj.transform.SetParent(transform);
            spawnPointObj.transform.localPosition = new Vector3(0, 1.5f, 0.5f); // Positioned slightly in front and above the mage
            projectileSpawnPoint = spawnPointObj.transform;
            Debug.LogWarning("Projectile spawn point not assigned. Created a default one.");
        }

        // Load default projectile prefab if not assigned
        if (projectilePrefab == null)
        {
            // Try to load from Resources folder
            projectilePrefab = Resources.Load<GameObject>("Prefabs/MageProjectile");
            if (projectilePrefab == null)
            {
                Debug.LogError("Projectile prefab not assigned and not found in Resources. Magic attacks will not work correctly.");
            }
        }
    }

    /// <summary>
    /// Finds the nearest enemy within range
    /// </summary>
    /// <returns>Position of the nearest enemy, or null if no enemies in range</returns>
    public Vector3? FindNearestEnemyPosition()
    {
        // Find all colliders in the specified range that match the enemy layer
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, autoTargetRange, enemyLayer);

        // If no enemies found, return null
        if (hitColliders.Length == 0)
        {
            Debug.Log("[MageCombatController] No enemies found in range for auto-targeting");
            return null;
        }

        // Find the closest enemy
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        foreach (Collider hitCollider in hitColliders)
        {
            // Calculate distance to this enemy
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);

            // Check if this is the closest enemy so far
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = hitCollider.transform;
            }
        }

        // If we found a closest enemy, return its position
        if (closestEnemy != null)
        {
            Debug.Log($"[MageCombatController] Auto-targeting enemy: {closestEnemy.name} at distance: {closestDistance}");
            return closestEnemy.position;
        }

        return null;
    }

    /// <summary>
    /// Execute a ranged magic attack, firing a projectile at the cursor's position or nearest enemy
    /// </summary>
    /// <param name="useAutoTarget">Whether to use auto-targeting</param>
    public void PerformMagicAttack(bool useAutoTarget = false)
    {
        if (isAttacking || !_canAttack) return;

        isAttacking = true;
        _canAttack = false;

        // Set animation trigger
        animator.SetBool("IsAttacking", true);

        Vector3 targetPosition;

        if (useAutoTarget)
        {
            // Try to find the nearest enemy
            Vector3? nearestEnemyPos = FindNearestEnemyPosition();

            if (nearestEnemyPos.HasValue)
            {
                // Use the nearest enemy as target
                targetPosition = nearestEnemyPos.Value;
            }
            else
            {
                // No enemies found, target straight ahead of the player
                targetPosition = transform.position + transform.forward * 100f;
            }
        }
        else
        {
            // Use traditional mouse targeting
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Raycast to find where the player clicked
            if (Physics.Raycast(ray, out hit))
            {
                targetPosition = hit.point;
            }
            else
            {
                // If no hit, project to a point far away in the ray direction
                targetPosition = ray.origin + ray.direction * 100f;
            }
        }

        // Calculate direction to target (without Y component for level aiming)
        Vector3 horizontalTargetPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        Vector3 lookDirection = horizontalTargetPosition - transform.position;

        // Rotate player to face target
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Delay projectile spawning to match animation
        StartCoroutine(SpawnProjectileWithDelay(0.4f, targetPosition));

        // Reset attack state and start cooldown
        StartCoroutine(ResetAttackState(magicAttackDuration));
        StartCoroutine(AttackCooldown());
    }

    /// <summary>
    /// Spawn a projectile after a delay to match the animation
    /// </summary>
    private IEnumerator SpawnProjectileWithDelay(float delay, Vector3 targetPosition)
    {
        yield return new WaitForSeconds(delay);

        if (projectilePrefab != null && projectileSpawnPoint != null)
        {
            // Create projectile
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);

            // Get direction to target
            Vector3 direction = (targetPosition - projectileSpawnPoint.position).normalized;

            // Add MageProjectile component and initialize
            MageProjectile mageProjectile = projectile.GetComponent<MageProjectile>();
            if (mageProjectile == null)
            {
                mageProjectile = projectile.AddComponent<MageProjectile>();
            }

            // Initialize projectile with damage and direction
            mageProjectile.Initialize(magicAttackDamage, direction, projectileSpeed, enemyLayer);

            Debug.Log($"[MageCombatController] Projectile fired toward {targetPosition}");
        }
        else
        {
            Debug.LogError("[MageCombatController] Cannot spawn projectile - prefab or spawn point is missing");
        }
    }

    /// <summary>
    /// Reset attack animation state after completion
    /// </summary>
    private IEnumerator ResetAttackState(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Reset animation state
        animator.SetBool("IsAttacking", false);
        isAttacking = false;

        Debug.Log("[MageCombatController] Attack animation completed");
    }

    /// <summary>
    /// Apply cooldown before next attack can be used
    /// </summary>
    public IEnumerator AttackCooldown()
    {
        _canAttack = false;
        yield return new WaitForSeconds(magicAttackCooldown);
        _canAttack = true;
        Debug.Log("[MageCombatController] Attack cooldown completed");
    }

    /// <summary>
    /// Cancel current attack if interrupted
    /// </summary>
    public void CancelAttack()
    {
        if (isAttacking)
        {
            Debug.Log("[MageCombatController] Attack canceled");
            isAttacking = false;
            animator.SetBool("IsAttacking", false);

            // Don't reset cooldown when canceled
        }
    }

    /// <summary>
    /// Draw gizmos for visualization in the editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (showTargetingGizmos)
        {
            // Draw the auto-targeting range sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, autoTargetRange);
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
