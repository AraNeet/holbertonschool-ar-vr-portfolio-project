using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArGame.Player.Mobile
{
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private float movementRange = 50f;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private bool hideOnRelease = true;
        
        [Header("UI References")]
        [SerializeField] private RectTransform backgroundRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private Image handleImage;
        [SerializeField] private Image backgroundImage;
        
        // Output values
        private Vector2 input = Vector2.zero;
        
        // Internal state
        private Vector2 startPosition;
        private Vector2 pointerDownPosition;
        private Canvas parentCanvas;
        private bool isDragging = false;
        
        // Public accessor for input value
        public Vector2 InputDirection => input;
        
        private void Awake()
        {
            // Get the parent canvas
            parentCanvas = GetComponentInParent<Canvas>();
            
            // Get initial positions
            startPosition = backgroundRect.anchoredPosition;
            
            // Hide if needed
            if (hideOnRelease)
            {
                SetVisible(false);
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // Show the joystick
            SetVisible(true);
            
            // Position the joystick at the touch point
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                backgroundRect.anchoredPosition = localPoint;
                pointerDownPosition = localPoint;
            }
            
            // Reset handle position
            handleRect.anchoredPosition = Vector2.zero;
            
            // Begin tracking
            OnDrag(eventData);
            isDragging = true;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                // Calculate the input vector
                Vector2 offset = localPoint;
                
                // Normalize and clamp to circle
                input = Vector2.ClampMagnitude(offset / movementRange, 1f);
                
                // Apply deadzone
                if (input.magnitude < deadZone)
                {
                    input = Vector2.zero;
                }
                else
                {
                    // Rescale input to be smoother after deadzone
                    input = input.normalized * ((input.magnitude - deadZone) / (1 - deadZone));
                }
                
                // Update handle position
                handleRect.anchoredPosition = input * movementRange;
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            // Reset input and handle position
            input = Vector2.zero;
            handleRect.anchoredPosition = Vector2.zero;
            
            // Optionally reset background position
            if (hideOnRelease)
            {
                SetVisible(false);
            }
            else
            {
                backgroundRect.anchoredPosition = startPosition;
            }
            
            isDragging = false;
        }
        
        private void SetVisible(bool visible)
        {
            if (backgroundImage != null)
                backgroundImage.enabled = visible;
                
            if (handleImage != null)
                handleImage.enabled = visible;
        }
        
        public bool IsDragging()
        {
            return isDragging;
        }
    }
} 