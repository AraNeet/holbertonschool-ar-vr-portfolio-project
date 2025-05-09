using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace ArGame.Player.Mobile
{
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button Settings")]
        [SerializeField] private float highlightScale = 1.1f;
        [SerializeField] private float animationDuration = 0.1f;
        [SerializeField] private Color pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        [Header("Button Events")]
        [SerializeField] private UnityEvent onButtonPressed;
        [SerializeField] private UnityEvent onButtonReleased;
        
        // Component references
        private Image buttonImage;
        private RectTransform rectTransform;
        
        // State tracking
        private Color defaultColor;
        private bool isPressed = false;
        private Vector3 defaultScale;
        
        private void Awake()
        {
            // Get references
            buttonImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            
            // Store default values
            if (buttonImage != null)
                defaultColor = buttonImage.color;
                
            defaultScale = rectTransform.localScale;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            
            // Visual feedback
            if (buttonImage != null)
                buttonImage.color = pressedColor;
                
            rectTransform.localScale = defaultScale * highlightScale;
            
            // Trigger event
            onButtonPressed?.Invoke();
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            
            // Reset visuals
            if (buttonImage != null)
                buttonImage.color = defaultColor;
                
            rectTransform.localScale = defaultScale;
            
            // Trigger event
            onButtonReleased?.Invoke();
        }
        
        public bool IsPressed()
        {
            return isPressed;
        }
        
        // Methods to connect to InputHandler
        public void NotifyJump()
        {
            if (FindObjectOfType<InputHandler>() is InputHandler inputHandler)
            {
                inputHandler.OnJumpButtonPressed();
            }
        }
        
        public void NotifyInteract()
        {
            if (FindObjectOfType<InputHandler>() is InputHandler inputHandler)
            {
                inputHandler.OnInteractButtonPressed();
            }
        }
    }
} 