using UnityEngine;

/// <summary>
/// Handles character movement using Unity's CharacterController.
/// Manages movement, rotation, sprinting, and gravity.
/// Can be used for both playable characters and enemies.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;         // Base movement speed
    public float gravity = -9.81f;       // Gravity force applied to the character
    [Tooltip("The tag used to identify ground objects")]
    public string groundTag = "Ground";   // Tag for ground objects

    [Header("Sprint Settings")]
    public float sprintMultiplier = 1.5f; // Speed multiplier when sprinting
    public bool isPlayerControlled = true; // Whether this controller accepts player input
    [Tooltip("Force sprint state on for touch/UI controls")]
    public bool forceSprintOn = false;    // UI can set this to control sprint

    [Header("Rotation Settings")]
    public float rotationSpeed = 10f;     // How fast the character rotates to face movement direction

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.5f; // Distance to check for ground (increased)
    [SerializeField] private LayerMask groundMask = -1;       // Layer mask for ground (default to all)
    [SerializeField] private bool debugGroundCheck = false;   // Enable debug visualization
    [SerializeField] private int groundCheckRays = 5;         // Number of rays to cast for better detection
    [SerializeField] private float groundSnapForce = 0.5f;    // Force to snap to ground

    [Header("Collision Prevention")]
    [SerializeField] private float skinWidth = 0.08f;         // Skin width for character controller
    [SerializeField] private float stepOffset = 0.3f;         // Step offset for character controller
    [SerializeField] private float minMoveDistance = 0.001f;  // Min move distance for controller

    private CharacterController characterController; // Reference to Unity's CharacterController component
    private Vector3 velocity;                       // Current velocity vector (primarily for gravity)
    private Vector3 moveDirection;                  // Current movement direction
    private bool isGrounded;                        // Whether the character is currently on the ground
    private bool isOnTaggedGround;                  // Whether the character is on a tagged ground
    private float originalStepOffset;               // Original step offset value
    private float lastGroundedTime;                 // Time when last grounded
    private float lastGroundedY;                    // Y position when last grounded

    /// <summary>
    /// Initialize by getting required components
    /// </summary>
    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Store original values
        originalStepOffset = characterController.stepOffset;

        // Configure character controller to help prevent falling through
        characterController.skinWidth = skinWidth;
        characterController.stepOffset = stepOffset;
        characterController.minMoveDistance = minMoveDistance;

        lastGroundedTime = Time.time;
        lastGroundedY = transform.position.y;
    }

    /// <summary>
    /// Handle movement, rotation, and gravity each frame
    /// </summary>
    void Update()
    {
        // Check if the character is grounded using CharacterController
        isGrounded = characterController.isGrounded;

        // Perform additional ground check with tagged objects
        CheckForTaggedGround();

        // Track when we were last grounded and position
        if (isGrounded || isOnTaggedGround)
        {
            lastGroundedTime = Time.time;
            lastGroundedY = transform.position.y;
        }

        // If we're falling for too long or too far, force a ground check
        if (!isGrounded && !isOnTaggedGround)
        {
            float timeSinceGrounded = Time.time - lastGroundedTime;
            float fallDistance = lastGroundedY - transform.position.y;

            // If falling more than 10m or for more than 5 seconds, do an emergency ground check
            if (fallDistance > 10f || timeSinceGrounded > 5f)
            {
                ForceGroundCheck();
            }
        }

        if ((isGrounded || isOnTaggedGround) && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Calculate movement speed based on sprint state
        float currentSpeed = moveSpeed;
        if (isPlayerControlled && (Input.GetKey(KeyCode.LeftShift) || forceSprintOn))
        {
            currentSpeed *= sprintMultiplier; // Apply sprint multiplier
        }

        // Apply horizontal movement based on moveDirection and speed
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Rotate character to face movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            // Create rotation to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);

            // Smoothly rotate towards target direction
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Apply gravity to vertical movement (not when on tagged ground)
        if (!isOnTaggedGround)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else
        {
            // Reset vertical velocity when on tagged ground
            velocity.y = 0;
        }

        // Apply a downward force to help snap to ground
        if (isGrounded || isOnTaggedGround)
        {
            velocity.y = -groundSnapForce;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// For extreme cases, teleport the player upward a bit and check for ground
    /// </summary>
    private void ForceGroundCheck()
    {
        // Check directly below for any ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 50f, groundMask))
        {
            // Teleport slightly above the hit point
            transform.position = hit.point + Vector3.up * (characterController.height * 0.5f + 0.1f);
            velocity.y = 0;
            Debug.Log("Emergency ground correction applied");
        }
    }

    /// <summary>
    /// Performs a raycast check for ground objects with the specified tag
    /// </summary>
    private void CheckForTaggedGround()
    {
        isOnTaggedGround = false;

        // Calculate the ray origin at the center of the character
        Vector3 rayOrigin = transform.position + characterController.center;
        float rayDistance = characterController.height / 2f + groundCheckDistance;

        // Cast multiple rays in a circular pattern for better detection
        for (int i = 0; i < groundCheckRays; i++)
        {
            // Calculate ray direction with slight offset for each ray
            float angle = i * (360f / groundCheckRays);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * (characterController.radius * 0.5f);
            Vector3 rayStart = rayOrigin + offset;
            Vector3 rayDirection = Vector3.down;

            // Cast the ray
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, groundMask))
            {
                // Check if the hit object has the ground tag or any collider
                if (hit.collider.CompareTag(groundTag) || string.IsNullOrEmpty(groundTag))
                {
                    isOnTaggedGround = true;

                    // Position correction to prevent falling through
                    if (!isGrounded)
                    {
                        // Adjust position to stay on top of the ground
                        float penetrationCorrection = 0.05f; // Small offset to keep above ground

                        // Only adjust if we're close to the ground
                        if (hit.distance < characterController.height * 0.6f)
                        {
                            float targetY = hit.point.y + characterController.height / 2f + penetrationCorrection;

                            // Smoothly adjust position
                            transform.position = new Vector3(
                                transform.position.x,
                                Mathf.Lerp(transform.position.y, targetY, 0.25f),
                                transform.position.z
                            );
                        }
                    }

                    // We found ground, no need to check more rays
                    break;
                }
            }

            // Debug visualization
            if (debugGroundCheck)
            {
                Color rayColor = isOnTaggedGround ? Color.green : Color.red;
                Debug.DrawRay(rayStart, rayDirection * rayDistance, rayColor, 0.1f);
            }
        }
    }

    /// <summary>
    /// Sets the movement direction (normalized vector).
    /// Called by states or other controllers to direct movement.
    /// </summary>
    /// <param name="direction">Direction to move in (should be normalized).</param>
    public void SetMoveDirection(Vector3 direction)
    {
        moveDirection = direction;
    }

    /// <summary>
    /// Gets the current movement direction
    /// </summary>
    /// <returns>Current movement direction vector</returns>
    public Vector3 GetMoveDirection()
    {
        return moveDirection;
    }

    /// <summary>
    /// Sets sprint state directly (for UI control)
    /// </summary>
    public void SetSprinting(bool sprinting)
    {
        forceSprintOn = sprinting;
    }

    /// <summary>
    /// Checks if sprint is active (either through keyboard or UI)
    /// </summary>
    /// <returns>True if sprinting is active</returns>
    public bool IsSprinting()
    {
        return forceSprintOn || Input.GetKey(KeyCode.LeftShift);
    }

    /// <summary>
    /// Returns whether the character is currently on the ground
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded || isOnTaggedGround;
    }

    /// <summary>
    /// Called when the script is reset in the editor
    /// </summary>
    private void OnValidate()
    {
        // Update character controller settings when values change in editor
        if (characterController != null)
        {
            characterController.skinWidth = skinWidth;
            characterController.stepOffset = stepOffset;
            characterController.minMoveDistance = minMoveDistance;
        }
    }
}
