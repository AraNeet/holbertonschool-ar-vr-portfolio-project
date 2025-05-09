using UnityEngine;

namespace ArGame.Player
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool mobileControlsEnabled = true;
        [SerializeField] private float joystickDeadZone = 0.1f;

        [Header("UI References")]
        [SerializeField] private RectTransform touchpadArea;
        [SerializeField] private RectTransform joystickKnob;
        [SerializeField] private GameObject mobileControlsUI;

        // Internal state
        private Vector2 moveInput;
        private Vector3 lastMousePosition;
        private Vector2 lookInput;
        private bool jumpInput;
        private bool interactInput;
        private bool touchActive;
        private Vector2 touchStartPos;
        private int touchId = -1;

        // Properties to access input values
        public Vector2 MoveInput => moveInput;
        public Vector2 LookInput => lookInput;
        public bool JumpInput => jumpInput;
        public bool InteractInput => interactInput;

        private void Start()
        {
            // Set up based on platform
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (!Application.isEditor && !Application.isConsolePlatform)
            {
                EnableMobileControls(true);
            }
            else
            {
                EnableMobileControls(false);
            }
            #else
            EnableMobileControls(true);
            #endif
            
            // Initialize for PC
            lastMousePosition = Input.mousePosition;
        }

        private void Update()
        {
            // Clear inputs
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            jumpInput = false;
            interactInput = false;

            // Process appropriate input method
            if (mobileControlsEnabled)
            {
                ProcessTouchInput();
            }
            else
            {
                ProcessKeyboardInput();
            }
        }

        private void ProcessKeyboardInput()
        {
            // Movement using WASD or arrow keys
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            moveInput = new Vector2(horizontal, vertical);

            // Look input using mouse
            if (Input.GetMouseButton(1)) // Right mouse button for look
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                lookInput = new Vector2(mouseDelta.x, mouseDelta.y) * 0.1f; // Adjust sensitivity as needed
            }
            lastMousePosition = Input.mousePosition;

            // Jump with spacebar
            jumpInput = Input.GetButtonDown("Jump");

            // Interact with E key
            interactInput = Input.GetKeyDown(KeyCode.E);
        }

        private void ProcessTouchInput()
        {
            // Virtual joystick for movement
            if (touchpadArea != null && joystickKnob != null)
            {
                // Process touches
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    // Check if touch is in touchpad area
                    if (RectTransformUtility.RectangleContainsScreenPoint(touchpadArea, touch.position))
                    {
                        // New touch started
                        if (touch.phase == TouchPhase.Began && touchId == -1)
                        {
                            touchId = touch.fingerId;
                            touchStartPos = touch.position;
                            touchActive = true;
                            
                            // Position the joystick knob at the touch point
                            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                touchpadArea, touch.position, null, out Vector2 localPoint);
                            joystickKnob.localPosition = localPoint;
                        }
                        // Update existing touch
                        else if (touch.phase == TouchPhase.Moved && touch.fingerId == touchId)
                        {
                            // Calculate movement direction and magnitude
                            Vector2 touchDelta = touch.position - touchStartPos;
                            float maxRadius = touchpadArea.rect.width * 0.5f;
                            touchDelta = Vector2.ClampMagnitude(touchDelta, maxRadius);
                            
                            // Convert to normalized input within range -1 to 1
                            moveInput = touchDelta / maxRadius;
                            
                            // Apply deadzone
                            if (moveInput.magnitude < joystickDeadZone)
                            {
                                moveInput = Vector2.zero;
                            }
                            
                            // Position joystick knob
                            joystickKnob.position = touchStartPos + touchDelta;
                        }
                        // Touch ended
                        else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && touch.fingerId == touchId)
                        {
                            touchActive = false;
                            touchId = -1;
                            moveInput = Vector2.zero;
                            
                            // Reset joystick position
                            joystickKnob.localPosition = Vector3.zero;
                        }
                    }
                    // Handle camera look with a second touch outside the touchpad
                    else if (touch.fingerId != touchId)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            lookInput = touch.deltaPosition * 10f * Time.deltaTime;
                        }
                    }
                }
                
                // Jump button is handled directly by UI Button events
                // Interact button is handled directly by UI Button events
            }
        }

        public void SetJumpInput(bool value)
        {
            jumpInput = value;
        }

        public void SetInteractInput(bool value)
        {
            interactInput = value;
        }

        public void EnableMobileControls(bool enable)
        {
            mobileControlsEnabled = enable;
            
            if (mobileControlsUI != null)
            {
                mobileControlsUI.SetActive(enable);
            }
        }
        
        // This can be called from a UI button press event
        public void OnJumpButtonPressed()
        {
            jumpInput = true;
        }
        
        // This can be called from a UI button press event
        public void OnInteractButtonPressed()
        {
            interactInput = true;
        }
    }
} 