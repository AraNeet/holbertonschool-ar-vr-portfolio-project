using UnityEngine;
using ArGame.Player.Mobile;

namespace ArGame.Player
{
    public class MobileControlsManager : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TouchJoystick movementJoystick;
        [SerializeField] private TouchJoystick lookJoystick;
        [SerializeField] private TouchButton jumpButton;
        [SerializeField] private TouchButton interactButton;
        [SerializeField] private GameObject mobileControlsCanvas;
        
        [Header("Input Settings")]
        [SerializeField] private bool autoEnableOnMobile = true;
        
        // Component references
        private InputHandler inputHandler;
        
        private void Awake()
        {
            // Get references
            inputHandler = FindObjectOfType<InputHandler>();
            
            // Enable or disable based on platform
            if (autoEnableOnMobile)
            {
                EnableMobileControls(Application.isMobilePlatform);
            }
        }
        
        private void Update()
        {
            // Only update if we have both joystick and input handler
            if (inputHandler != null && mobileControlsCanvas.activeSelf)
            {
                // Pass joystick inputs to input handler
                UpdateInputsFromJoysticks();
            }
        }
        
        private void UpdateInputsFromJoysticks()
        {
            // Update movement directly from joystick
            if (movementJoystick != null)
            {
                // We can access the InputHandler's moveInput directly if needed
                // Or the joystick values can be read directly by the PlayerController
            }
            
            // Update jump input from button state
            if (jumpButton != null && jumpButton.IsPressed())
            {
                inputHandler.SetJumpInput(true);
            }
            
            // Update interact input from button state
            if (interactButton != null && interactButton.IsPressed())
            {
                inputHandler.SetInteractInput(true);
            }
        }
        
        public void EnableMobileControls(bool enable)
        {
            if (mobileControlsCanvas != null)
            {
                mobileControlsCanvas.SetActive(enable);
            }
            
            if (inputHandler != null)
            {
                inputHandler.EnableMobileControls(enable);
            }
        }
        
        public Vector2 GetMovementInput()
        {
            if (movementJoystick != null && mobileControlsCanvas.activeSelf)
            {
                return movementJoystick.InputDirection;
            }
            
            return Vector2.zero;
        }
        
        public Vector2 GetLookInput()
        {
            if (lookJoystick != null && mobileControlsCanvas.activeSelf)
            {
                return lookJoystick.InputDirection;
            }
            
            return Vector2.zero;
        }
        
        // Button event hooks
        public void OnJumpButtonPressed()
        {
            if (inputHandler != null)
            {
                inputHandler.OnJumpButtonPressed();
            }
        }
        
        public void OnInteractButtonPressed()
        {
            if (inputHandler != null)
            {
                inputHandler.OnInteractButtonPressed();
            }
        }
    }
} 