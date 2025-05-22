using System.Collections;
using UnityEngine;
using System.Linq;

/// <summary>
/// Handles combat abilities for the Barbarian character.
/// Manages different attack types, damage, and cooldowns.
/// </summary>
public class BrabCombatController : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Should match the exact duration of the normal attack animation clip")]
    public float normalAttackDuration = 0.5f;     // Duration of normal attack animation
    [Tooltip("Should match the exact duration of the spin attack animation clip")]
    public float spinAttackDuration = 1.2f;       // Duration of spin attack animation
    [Tooltip("Fixed at 4 seconds regardless of animation length")]
    public float chargeSpinAttackDuration = 4f;   // Duration of charged spin attack
    public float chargeSpinAttackCooldown = 10f;  // Cooldown time for charged spin attack

    [Tooltip("Delay before hitbox activates after starting attack (0 = immediate)")]
    public float hitboxDelay = 0f;                // Set to 0 for immediate activation

    [Tooltip("Duration hitbox stays active as fraction of attack duration")]
    public float hitboxActiveFraction = 0.8f;     // Hitbox stays active for 80% of animation

    [Header("Damage Settings")]
    public float normalAttackDamage = 10f;        // Damage for normal attack
    public float spinAttackDamage = 15f;          // Damage for spin attack
    public float chargeSpinAttackDamage = 25f;    // Damage for charged spin attack

    [Header("References")]
    public LayerMask enemyLayer;                  // Layer mask for enemy detection
    public float attackRadius = 2f;               // Hit detection radius for normal attack
    public float spinAttackRadius = 3.5f;         // Hit detection radius for spin attack
    public float chargeSpinAttackRadius = 5f;     // Hit detection radius for charged spin attack

    [Header("Visual Feedback")]
    public GameObject hitEffectPrefab;            // Visual effect for hits
    public bool showHitboxGizmos = true;          // Whether to show hitbox gizmos

    // Component references
    private Animator animator;                    // Reference to animator component
    private BrabStateMachine stateMachine;        // Reference to state machine

    // Attack state flags
    private bool isAttacking = false;             // Currently performing normal attack
    private bool isSpinAttacking = false;         // Currently performing spin attack
    private bool isChargeSpinAttacking = false;   // Currently performing charged spin attack
    private bool _canChargeSpinAttack = true;     // Charged attack is off cooldown
    private float chargeStartTime;                // When the charge began
    private bool isCharging = false;              // Currently charging an attack

    // Hitbox visualization
    private bool isHitboxActive = false;
    private float activeHitboxRadius = 0f;
    private bool isActiveHitboxAOE = false;

    // Public property for cooldown state
    public bool canChargeSpinAttack { get { return _canChargeSpinAttack; } }

    // Reference to player controller to check input state
    private BrabPlayerController playerController;

    /// <summary>
    /// Initialize components
    /// </summary>
    private void Awake()
    {
        // Get references
        animator = GetComponent<Animator>();
        stateMachine = GetComponent<BrabStateMachine>();

        // Get reference to player controller for input settings
        playerController = GetComponent<BrabPlayerController>();

        // Initialize state
        _canChargeSpinAttack = true;
        isHitboxActive = false;

        // Debug enemyLayer to check it's set correctly
        Debug.Log($"BrabCombatController - enemyLayer value: {enemyLayer.value}, includes Enemy layer: {((1 << LayerMask.NameToLayer("Enemy")) & enemyLayer.value) != 0}");

        // Validate that enemyLayer is set and includes the Enemy layer
        if (enemyLayer.value == 0 || ((1 << LayerMask.NameToLayer("Enemy")) & enemyLayer.value) == 0)
        {
            Debug.LogWarning("Enemy layer mask not configured correctly. Auto-assigning to Enemy layer.");
            enemyLayer = LayerMask.GetMask("Enemy");
            Debug.Log($"Updated enemyLayer value: {enemyLayer.value}");
        }
    }

    /// <summary>
    /// Process input every frame
    /// </summary>
    private void Update()
    {
        // Skip input processing if keyboard/mouse input is disabled via player controller
        if (playerController != null && playerController.disableKeyboardMouseInput)
            return;

        // Normal Attack (Left Mouse Button)
        if (Input.GetMouseButtonDown(0) && !isAttacking && !isSpinAttacking && !isChargeSpinAttacking)
        {
            PerformNormalAttack();
        }

        // Spin Attack (Right Mouse Button - single press)
        if (Input.GetMouseButtonDown(1) && !isAttacking && !isSpinAttacking && !isChargeSpinAttacking)
        {
            isCharging = true;
            chargeStartTime = Time.time;
        }

        // Check for charge spin attack or release for normal spin attack
        if (Input.GetMouseButton(1) && isCharging)
        {
            float chargeTime = Time.time - chargeStartTime;

            // If held for more than 1 second and can perform charge attack
            if (chargeTime >= 1f && _canChargeSpinAttack && !isAttacking && !isSpinAttacking && !isChargeSpinAttacking)
            {
                PerformChargeSpinAttack();
                isCharging = false;
            }
        }
        else if (Input.GetMouseButtonUp(1) && isCharging)
        {
            float chargeTime = Time.time - chargeStartTime;

            // If released before 1 second, perform normal spin attack
            if (chargeTime < 1f && !isAttacking && !isSpinAttacking && !isChargeSpinAttacking)
            {
                PerformSpinAttack();
            }

            isCharging = false;
        }

        // Debug toggle
        if (Input.GetKeyDown(KeyCode.F6))
        {
            DebugDetectAllColliders();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            DebugAttackAllEnemies();
        }
    }

    /// <summary>
    /// Execute a normal attack
    /// </summary>
    public void PerformNormalAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        animator.SetBool("IsAttacking", true);

        // Activate hitbox immediately with small or no delay
        float activeDelay = hitboxDelay;
        float activeDuration = normalAttackDuration * hitboxActiveFraction;

        // Delay hit detection (can be 0 for immediate)
        StartCoroutine(DelayedHitDetection(attackRadius, normalAttackDamage, false, activeDelay, activeDuration));

        // Reset attack state after animation completes
        StartCoroutine(ResetAttackState(normalAttackDuration));
    }

    /// <summary>
    /// Execute a spin attack that hits enemies in all directions
    /// </summary>
    public void PerformSpinAttack()
    {
        if (isSpinAttacking) return;

        isSpinAttacking = true;
        animator.SetBool("IsIdle", false);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsSpinAttacking", true);

        // Activate hitbox immediately with small or no delay
        float activeDelay = hitboxDelay;
        float activeDuration = spinAttackDuration * hitboxActiveFraction;

        // Delay hit detection (can be 0 for immediate)
        StartCoroutine(DelayedHitDetection(spinAttackRadius, spinAttackDamage, true, activeDelay, activeDuration));

        // Reset attack state after animation completes
        StartCoroutine(ResetSpinAttackState(spinAttackDuration));
    }

    /// <summary>
    /// Execute a powerful charged spin attack with larger radius and damage
    /// </summary>
    public void PerformChargeSpinAttack()
    {
        if (isChargeSpinAttacking || !_canChargeSpinAttack) return;

        isChargeSpinAttacking = true;
        _canChargeSpinAttack = false;

        // Set animation loop mode to repeat
        animator.SetBool("IsIdle", false);
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool("IsChargeSpinAttack", true);

        // Use a special trigger to override the default animation transitions
        animator.SetTrigger("StartChargeSpinAttack");

        // Apply continuous damage during the charge spin attack, with minimal delay
        StartCoroutine(DelayedChargeSpinAttackDamageRoutine(hitboxDelay));

        // Reset attack state and start cooldown
        StartCoroutine(ResetChargeSpinAttackState(chargeSpinAttackDuration));
        StartCoroutine(ChargeSpinAttackCooldown());
    }

    /// <summary>
    /// Applies damage multiple times during the charge spin attack with an initial delay
    /// </summary>
    private IEnumerator DelayedChargeSpinAttackDamageRoutine(float initialDelay)
    {
        // Wait for initial delay before activating hitbox (can be 0)
        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        isHitboxActive = true;
        activeHitboxRadius = chargeSpinAttackRadius;
        isActiveHitboxAOE = true;
        Debug.Log("[BrabCombatController] Charge spin attack hitbox activated");

        // Apply damage multiple times during the attack duration
        float elapsed = 0f;
        float activeDuration = chargeSpinAttackDuration - initialDelay;

        // Movement coordination phases:
        // 0.0s-0.5s: No movement, initial windup (handled in state)
        // 0.5s-3.5s: Movement allowed, continuous damage (handled in state)
        // 3.5s-4.0s: No movement, final attack (handled in state)

        while (elapsed < activeDuration)
        {
            // Apply damage with visual feedback
            bool didHit = DetectHits(chargeSpinAttackRadius, chargeSpinAttackDamage / 8f, true);

            // Log phase information
            float currentTime = initialDelay + elapsed;
            if (currentTime >= 0.5f && currentTime < 3.5f)
            {
                Debug.Log($"[BrabCombatController] Charge spin attack in movement phase: {currentTime:F2}s");
            }
            else if (currentTime >= 3.5f)
            {
                Debug.Log($"[BrabCombatController] Charge spin attack in final phase: {currentTime:F2}s");
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);

            // Stop if attack was interrupted
            if (!isChargeSpinAttacking)
            {
                Debug.Log("[BrabCombatController] Charge spin attack interrupted");
                break;
            }
        }

        Debug.Log("[BrabCombatController] Charge spin attack hitbox deactivated");
        isHitboxActive = false;
        activeHitboxRadius = 0f;
    }

    /// <summary>
    /// Delayed hit detection that activates after a specific time and stays active for a duration
    /// </summary>
    private IEnumerator DelayedHitDetection(float radius, float damage, bool isAOE, float delay, float activeDuration)
    {
        // Wait for the initial delay (which can be 0)
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Activate hitbox
        isHitboxActive = true;
        activeHitboxRadius = radius;
        isActiveHitboxAOE = isAOE;
        Debug.Log($"[BrabCombatController] Attack hitbox activated - radius: {radius}, isAOE: {isAOE}");

        // Apply damage
        DetectHits(radius, damage, isAOE);

        // For AOE attacks, we might want multiple hits
        if (isAOE && activeDuration > 0.5f)
        {
            // Apply damage once more in the middle of the animation
            yield return new WaitForSeconds(activeDuration * 0.5f);

            // Only apply if the attack is still active (not interrupted)
            if ((isSpinAttacking && isAOE) || (isChargeSpinAttacking && isAOE))
            {
                DetectHits(radius, damage * 0.5f, isAOE);
            }
        }

        // Wait until end of active duration
        yield return new WaitForSeconds(activeDuration * 0.5f);

        // Deactivate hitbox
        isHitboxActive = false;
        activeHitboxRadius = 0f;
        Debug.Log("[BrabCombatController] Attack hitbox deactivated");
    }

    /// <summary>
    /// Detect and damage enemies within a radius
    /// </summary>
    /// <param name="radius">Detection radius</param>
    /// <param name="damage">Damage amount to apply</param>
    /// <param name="isAOE">Whether this is an area of effect attack (360 degrees)</param>
    /// <returns>True if any enemies were hit</returns>
    private bool DetectHits(float radius, float damage, bool isAOE = false)
    {
        // Try using sphere overlap method first
        bool hitDetected = DetectHitsUsingSphere(radius, damage, isAOE);

        // If no hits were detected with sphere overlap, try raycast method
        if (!hitDetected)
        {
            hitDetected = DetectHitsUsingRaycasts(radius, damage, isAOE);
        }

        return hitDetected;
    }

    /// <summary>
    /// Detect hits using sphere overlap method
    /// </summary>
    /// <returns>True if any enemy was hit</returns>
    private bool DetectHitsUsingSphere(float radius, float damage, bool isAOE = false)
    {
        // Find all enemies within the attack radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        // Debug info about colliders found
        Debug.Log($"Attack detection found {hitColliders.Length} potential targets within radius {radius}");
        foreach (var collider in hitColliders)
        {
            Debug.Log($"Found collider: {collider.gameObject.name}, Layer: {LayerMask.LayerToName(collider.gameObject.layer)}, Tag: {collider.gameObject.tag}");
        }

        bool anyHit = false;

        foreach (var hitCollider in hitColliders)
        {
            // If it's not AOE, check if enemy is in front of player
            if (!isAOE)
            {
                Vector3 directionToTarget = (hitCollider.transform.position - transform.position).normalized;
                float dotProduct = Vector3.Dot(transform.forward, directionToTarget);

                // Enemy must be in front (within ~60 degrees)
                if (dotProduct < 0.5f)
                {
                    Debug.Log($"Enemy {hitCollider.gameObject.name} not in front arc (dot: {dotProduct})");
                    continue;
                }
            }

            // Apply damage to enemy if it has a Health component
            Health enemyHealth = hitCollider.GetComponent<Health>();
            if (enemyHealth != null)
            {
                anyHit = true;
                Debug.Log($"Applying {damage} damage to {hitCollider.gameObject.name}");
                enemyHealth.TakeDamage(damage);
            }
            else
            {
                Debug.Log($"No Health component found on {hitCollider.gameObject.name}");
            }
        }

        return anyHit;
    }

    /// <summary>
    /// Alternative hit detection using raycasts in a cone pattern
    /// </summary>
    private bool DetectHitsUsingRaycasts(float radius, float damage, bool isAOE = false)
    {
        Debug.Log("Falling back to raycast detection method");

        bool anyHit = false;
        int rayCount = isAOE ? 36 : 12; // More rays for AOE, fewer for directional
        float angleStep = isAOE ? 360f / rayCount : 120f / rayCount;
        float startAngle = isAOE ? 0f : -60f; // Start at -60 degrees for directional (120-degree cone)

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (i * angleStep);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;

            // Draw debug ray
            Debug.DrawRay(transform.position, direction * radius, Color.red, 1f);

            // Cast ray
            RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, radius, enemyLayer);

            foreach (var hit in hits)
            {
                // Apply damage to enemy if it has a Health component
                Health enemyHealth = hit.collider.GetComponent<Health>();

                // If no Health on the hit object, try to find it on its parent
                if (enemyHealth == null)
                {
                    enemyHealth = hit.collider.GetComponentInParent<Health>();
                }

                if (enemyHealth != null)
                {
                    anyHit = true;
                    Debug.Log($"[Raycast] Applying {damage} damage to {hit.collider.gameObject.name}");
                    enemyHealth.TakeDamage(damage);
                }
                else
                {
                    Debug.Log($"[Raycast] No Health component found on or in parent of {hit.collider.gameObject.name}");
                }
            }
        }

        return anyHit;
    }

    /// <summary>
    /// Reset the normal attack state after animation completes
    /// </summary>
    private IEnumerator ResetAttackState(float duration)
    {
        yield return new WaitForSeconds(duration);
        isAttacking = false;
        animator.SetBool("IsAttacking", false);
    }

    /// <summary>
    /// Reset the spin attack state after animation completes
    /// </summary>
    private IEnumerator ResetSpinAttackState(float duration)
    {
        yield return new WaitForSeconds(duration);
        isSpinAttacking = false;
        animator.SetBool("IsIdle", true);
        animator.SetBool("IsSpinAttacking", false);
    }

    /// <summary>
    /// Reset the charge spin attack state after animation completes
    /// </summary>
    private IEnumerator ResetChargeSpinAttackState(float duration)
    {
        yield return new WaitForSeconds(duration);
        isChargeSpinAttacking = false;

        // Reset both the regular bool parameter and the trigger
        animator.SetBool("IsChargeSpinAttack", false);
        animator.ResetTrigger("StartChargeSpinAttack");

        // Ensure we transition back to idle animation state
        animator.SetBool("IsIdle", true);

        Debug.Log("[BrabCombatController] Charge spin attack completed and animation reset");
    }

    /// <summary>
    /// Handle cooldown timer for the charge spin attack
    /// </summary>
    private IEnumerator ChargeSpinAttackCooldown()
    {
        yield return new WaitForSeconds(chargeSpinAttackCooldown);
        _canChargeSpinAttack = true;
    }

    /// <summary>
    /// Draw attack range gizmo in editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showHitboxGizmos) return;

        // Draw normal attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red
        DrawAttackCone(transform.position, transform.forward, 120f, attackRadius, Gizmos.color);

        // Draw spin attack range
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f); // Semi-transparent blue
        Gizmos.DrawWireSphere(transform.position, spinAttackRadius);

        // Draw charge spin attack range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Semi-transparent orange
        Gizmos.DrawWireSphere(transform.position, chargeSpinAttackRadius);

        // Draw active hitbox (if any) in green
        if (isHitboxActive && Application.isPlaying)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Semi-transparent green
            if (isActiveHitboxAOE)
            {
                Gizmos.DrawSphere(transform.position, activeHitboxRadius);
            }
            else
            {
                DrawAttackCone(transform.position, transform.forward, 120f, activeHitboxRadius, Gizmos.color);
            }
        }
    }

    /// <summary>
    /// Draw a cone shape to represent directional attacks
    /// </summary>
    private void DrawAttackCone(Vector3 position, Vector3 direction, float angle, float radius, Color color)
    {
        int segments = 10;
        float angleStep = angle / segments;
        Vector3 lastPoint = position + Quaternion.Euler(0, -angle / 2, 0) * direction * radius;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -angle / 2 + angleStep * i;
            Vector3 currentPoint = position + Quaternion.Euler(0, currentAngle, 0) * direction * radius;

            Gizmos.color = color;
            if (i > 0)
            {
                Gizmos.DrawLine(lastPoint, currentPoint);
            }
            Gizmos.DrawLine(position, currentPoint);

            lastPoint = currentPoint;
        }

        // Draw the arc
        float arcSegments = 20;
        float arcStep = angle / arcSegments;
        lastPoint = position + Quaternion.Euler(0, -angle / 2, 0) * direction * radius;

        for (int i = 1; i <= arcSegments; i++)
        {
            float currentAngle = -angle / 2 + arcStep * i;
            Vector3 currentPoint = position + Quaternion.Euler(0, currentAngle, 0) * direction * radius;

            Gizmos.DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
    }

    /// <summary>
    /// Debug method to detect all colliders in range regardless of layer
    /// </summary>
    private void DebugDetectAllColliders()
    {
        float testRadius = 10f; // Large radius to ensure we find something

        // Find ALL colliders in range (ignore layer mask)
        Collider[] allColliders = Physics.OverlapSphere(transform.position, testRadius);

        Debug.Log($"[DEBUG] Found {allColliders.Length} total colliders within {testRadius} units");

        // Log all colliders found
        foreach (var collider in allColliders)
        {
            string healthStatus = collider.GetComponent<Health>() != null ? "HAS Health component" : "NO Health component";
            Debug.Log($"[DEBUG] Collider: {collider.gameObject.name}, Layer: {LayerMask.LayerToName(collider.gameObject.layer)}, " +
                      $"Tag: {collider.gameObject.tag}, IsTrigger: {collider.isTrigger}, {healthStatus}");
        }

        // Specifically check for objects with Enemy tag or on Enemy layer
        var enemyTagged = allColliders.Where(c => c.CompareTag("Enemy")).ToArray();
        var enemyLayered = allColliders.Where(c => c.gameObject.layer == LayerMask.NameToLayer("Enemy")).ToArray();

        Debug.Log($"[DEBUG] Found {enemyTagged.Length} objects with Enemy tag");
        Debug.Log($"[DEBUG] Found {enemyLayered.Length} objects on Enemy layer");
    }

    /// <summary>
    /// Debug method to apply damage to all enemy-tagged objects regardless of layer
    /// </summary>
    private void DebugAttackAllEnemies()
    {
        Debug.Log("[DEBUG ATTACK] Attempting to damage all enemy objects in range");

        // Find ALL objects in a large radius
        Collider[] allColliders = Physics.OverlapSphere(transform.position, 10f);

        int enemyCount = 0;
        foreach (var collider in allColliders)
        {
            // Check for Enemy tag
            if (collider.CompareTag("Enemy"))
            {
                enemyCount++;

                // Try to get Health component (direct or from parent)
                Health enemyHealth = collider.GetComponent<Health>();
                if (enemyHealth == null)
                {
                    enemyHealth = collider.GetComponentInParent<Health>();
                }

                if (enemyHealth != null)
                {
                    float debugDamage = 20f;
                    Debug.Log($"[DEBUG ATTACK] Found enemy with Health: {collider.gameObject.name}, applying {debugDamage} damage");
                    enemyHealth.TakeDamage(debugDamage);
                }
                else
                {
                    Debug.Log($"[DEBUG ATTACK] Found enemy without Health component: {collider.gameObject.name}");

                    // Try other paths to find the Health component
                    Transform current = collider.transform;
                    while (current != null)
                    {
                        Health health = current.GetComponent<Health>();
                        if (health != null)
                        {
                            float debugDamage = 20f;
                            Debug.Log($"[DEBUG ATTACK] Found Health on parent: {current.gameObject.name}, applying {debugDamage} damage");
                            health.TakeDamage(debugDamage);
                            break;
                        }
                        current = current.parent;
                    }
                }
            }
        }

        Debug.Log($"[DEBUG ATTACK] Found {enemyCount} enemy-tagged objects");
    }

    /// <summary>
    /// Cancel any ongoing attack animations and states
    /// Called when player is damaged to interrupt attacks
    /// </summary>
    public void CancelAttacks()
    {
        if (isAttacking)
        {
            isAttacking = false;
            animator.SetBool("IsAttacking", false);
        }

        if (isSpinAttacking)
        {
            isSpinAttacking = false;
            animator.SetBool("IsSpinAttacking", false);
        }

        if (isChargeSpinAttacking)
        {
            isChargeSpinAttacking = false;
            animator.SetBool("IsChargeSpinAttack", false);
        }

        isHitboxActive = false;
        isCharging = false;
    }
}
