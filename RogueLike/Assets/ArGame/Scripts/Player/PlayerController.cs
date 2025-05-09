using UnityEngine;

namespace ArGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float airControl = 0.5f;
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 2f;
        [SerializeField] private LayerMask interactionLayer;
        
        [Header("Component References")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private PlayerAnimator playerAnimator;
        [SerializeField] private Transform cameraTarget;

        // Component references
        private CharacterController characterController;
        private Transform playerTransform;
        
        // Movement state
        private Vector3 moveDirection;
        private Vector3 verticalVelocity;
        private bool isGrounded;
        private float currentMoveSpeed;
        private float turnSmoothVelocity;
        
        private void Awake()
        {
            // Get references
            characterController = GetComponent<CharacterController>();
            playerTransform = transform;
            
            // Auto-find components if not assigned
            if (inputHandler == null)
                inputHandler = FindObjectOfType<InputHandler>();
                
            if (playerAnimator == null)
                playerAnimator = GetComponentInChildren<PlayerAnimator>();
        }
        
        private void Update()
        {
            // Check if player is grounded
            CheckGroundStatus();
            
            // Handle movement input
            ProcessMovement();
            
            // Handle jump input
            ProcessJump();
            
            // Handle interaction input
            ProcessInteraction();
        }
        
        private void ProcessMovement()
        {
            // Get input from handler
            Vector2 moveInput = inputHandler.MoveInput;
            Vector2 lookInput = inputHandler.LookInput;
            
            // Only process movement if we have input
            if (moveInput.magnitude > 0.1f)
            {
                // Calculate move direction based on camera direction
                Camera mainCamera = Camera.main;
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;
                
                // Remove vertical component
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();
                
                // Calculate movement direction
                moveDirection = forward * moveInput.y + right * moveInput.x;
                
                // Only rotate if we're on the ground
                if (isGrounded)
                {
                    // Rotate player to face movement direction
                    float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                    float angle = Mathf.SmoothDampAngle(playerTransform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.1f);
                    playerTransform.rotation = Quaternion.Euler(0f, angle, 0f);
                    
                    // Set full movement speed on ground
                    currentMoveSpeed = moveSpeed;
                }
                else
                {
                    // Reduced air control
                    currentMoveSpeed = moveSpeed * airControl;
                }
                
                // Update animation speed
                if (playerAnimator != null)
                {
                    playerAnimator.UpdateMovementAnimation(moveInput.magnitude);
                }
            }
            else
            {
                // No input, stop horizontal movement
                moveDirection = Vector3.zero;
                
                // Update animation
                if (playerAnimator != null)
                {
                    playerAnimator.UpdateMovementAnimation(0);
                }
            }
            
            // Handle looking with camera
            if (lookInput.magnitude > 0.1f && cameraTarget != null)
            {
                cameraTarget.Rotate(Vector3.up, lookInput.x);
                
                // Limit vertical look angle
                float xRotation = cameraTarget.localEulerAngles.x - lookInput.y;
                
                // Clamp to avoid over-rotation
                if (xRotation > 180) xRotation -= 360;
                xRotation = Mathf.Clamp(xRotation, -80, 80);
                
                // Apply rotation
                cameraTarget.localEulerAngles = new Vector3(xRotation, cameraTarget.localEulerAngles.y, 0);
            }
            
            // Apply gravity
            if (isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f; // Small gravity to keep the character grounded
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }
            
            // Move character
            Vector3 movement = moveDirection * currentMoveSpeed + verticalVelocity;
            characterController.Move(movement * Time.deltaTime);
        }
        
        private void ProcessJump()
        {
            // Only allow jumping if grounded
            if (isGrounded && inputHandler.JumpInput)
            {
                // Calculate jump velocity from height
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                
                // Trigger jump animation
                if (playerAnimator != null)
                {
                    playerAnimator.TriggerJumpAnimation();
                }
            }
        }
        
        private void ProcessInteraction()
        {
            if (inputHandler.InteractInput)
            {
                // Cast ray to detect interactive objects
                Ray interactionRay = new Ray(playerTransform.position + Vector3.up * 0.5f, playerTransform.forward);
                if (Physics.Raycast(interactionRay, out RaycastHit hit, interactionDistance, interactionLayer))
                {
                    // Check if object has an IInteractable interface
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(this);
                        
                        // Trigger interaction animation
                        if (playerAnimator != null)
                        {
                            playerAnimator.TriggerInteractAnimation();
                        }
                    }
                }
            }
        }
        
        private void CheckGroundStatus()
        {
            // Use a raycast to determine if player is grounded
            bool wasGrounded = isGrounded;
            
            // Only perform ground check if falling
            if (characterController.isGrounded && verticalVelocity.y < 0)
            {
                isGrounded = true;
            }
            else
            {
                // Cast a ray towards the ground
                if (Physics.Raycast(playerTransform.position + (Vector3.up * 0.1f), Vector3.down, out RaycastHit hit, groundCheckDistance))
                {
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }
            }
            
            // Update animation if ground state changed
            if (wasGrounded != isGrounded && playerAnimator != null)
            {
                playerAnimator.SetGroundedState(isGrounded);
            }
        }
        
        // Public interface for external control
        public void TeleportTo(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        
        public void ApplyDamage(float damage)
        {
            // Handle damage here (e.g., decrease health)
            Debug.Log($"Player took {damage} damage");
            
            // Trigger hit animation
            if (playerAnimator != null)
            {
                playerAnimator.TriggerHitAnimation();
            }
        }
        
        // Add power-ups or temporary effect methods here
        public void ApplySpeedBoost(float multiplier, float duration)
        {
            // Implement speed boost logic
            StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
        }
        
        private System.Collections.IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
        {
            float originalSpeed = moveSpeed;
            moveSpeed *= multiplier;
            
            yield return new WaitForSeconds(duration);
            
            moveSpeed = originalSpeed;
        }
    }
    
    // Interface for interactive objects
    public interface IInteractable
    {
        void Interact(PlayerController player);
    }
} 