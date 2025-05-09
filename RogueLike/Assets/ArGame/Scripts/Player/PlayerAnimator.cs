using UnityEngine;

namespace ArGame.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float movementAnimationSmoothing = 0.1f;
        
        // Animator parameter names
        private readonly string speedParam = "Speed";
        private readonly string jumpParam = "Jump";
        private readonly string groundedParam = "Grounded";
        private readonly string interactParam = "Interact";
        
        // Component references
        private Animator animator;
        
        // Animation state
        private float currentSpeed;
        
        private void Awake()
        {
            // Get references
            animator = GetComponent<Animator>();
        }
        
        /// <summary>
        /// Update movement animation based on input speed value
        /// </summary>
        public void UpdateMovementAnimation(float speed)
        {
            // Smoothly transition the animation
            currentSpeed = Mathf.Lerp(currentSpeed, speed, movementAnimationSmoothing);
            animator.SetFloat(speedParam, currentSpeed);
        }
        
        /// <summary>
        /// Trigger jump animation
        /// </summary>
        public void TriggerJumpAnimation()
        {
            animator.SetTrigger(jumpParam);
        }
        
        /// <summary>
        /// Set grounded state for animation
        /// </summary>
        public void SetGroundedState(bool grounded)
        {
            animator.SetBool(groundedParam, grounded);
        }
        
        /// <summary>
        /// Trigger interact animation
        /// </summary>
        public void TriggerInteractAnimation()
        {
            animator.SetTrigger(interactParam);
        }
        
        /// <summary>
        /// Register a hit/damage animation
        /// </summary>
        public void TriggerHitAnimation()
        {
            animator.SetTrigger("Hit");
        }
        
        /// <summary>
        /// Play death animation
        /// </summary>
        public void TriggerDeathAnimation()
        {
            animator.SetTrigger("Death");
        }
        
        /// <summary>
        /// Reset all animator triggers
        /// </summary>
        public void ResetTriggers()
        {
            animator.ResetTrigger(jumpParam);
            animator.ResetTrigger(interactParam);
        }
        
        /// <summary>
        /// Check if a specific animation is playing
        /// </summary>
        public bool IsAnimationPlaying(string animName)
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsName(animName);
        }
        
        /// <summary>
        /// Get the progress of the current animation (0-1)
        /// </summary>
        public float GetCurrentAnimationProgress()
        {
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }
    }
} 