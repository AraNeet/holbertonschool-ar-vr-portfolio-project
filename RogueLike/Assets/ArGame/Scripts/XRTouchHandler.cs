using UnityEngine;
using UnityEngine.EventSystems;

public class XRTouchHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private CubeInteractionHandler cubeInteraction;
    [SerializeField] private CubeLayerPeeler layerPeeler;

    [Header("Touch Settings")]
    [SerializeField] private float swipeThreshold = 20f;
    [SerializeField] private float tapTimeThreshold = 0.3f;

    // Touch state tracking
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isTouching = false;
    private bool isPotentialSwipe = false;

    void Start()
    {
        // Auto-find components if not assigned
        if (xrCamera == null)
            xrCamera = Camera.main;

        if (cubeInteraction == null)
            cubeInteraction = GetComponent<CubeInteractionHandler>();

        if (layerPeeler == null)
            layerPeeler = GetComponent<CubeLayerPeeler>();
    }

    void Update()
    {
        // Don't process touches that are over UI elements
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        // Handle touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPos = touch.position;
                    touchStartTime = Time.time;
                    isTouching = true;
                    isPotentialSwipe = true;
                    break;

                case TouchPhase.Moved:
                    if (isTouching && isPotentialSwipe)
                    {
                        // Check if this is a swipe
                        float distance = Vector2.Distance(touch.position, touchStartPos);
                        if (distance > swipeThreshold)
                        {
                            HandleSwipe(touchStartPos, touch.position);
                            isPotentialSwipe = false;
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    if (isTouching)
                    {
                        // If touch ended quickly and didn't move much, it's a tap
                        float duration = Time.time - touchStartTime;
                        float distance = Vector2.Distance(touch.position, touchStartPos);

                        if (duration < tapTimeThreshold && distance < swipeThreshold)
                        {
                            HandleTap(touch.position);
                        }

                        isTouching = false;
                        isPotentialSwipe = false;
                    }
                    break;
            }
        }
    }

    private void HandleSwipe(Vector2 startPos, Vector2 endPos)
    {
        // Check if this is a swipe on the cube (for layer peeling)
        Ray ray = xrCamera.ScreenPointToRay(startPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // If we hit the cube, process the swipe for layer peeling
            if (hit.transform.CompareTag("CubeExterior") && layerPeeler != null)
            {
                layerPeeler.DetectSwipeAndPeel(startPos, endPos, xrCamera);
            }
        }
        else
        {
            // If we didn't hit anything, it might be a rotation swipe
            if (cubeInteraction != null)
            {
                Vector2 delta = endPos - startPos;
                cubeInteraction.HandleManualRotation(delta);
            }
        }
    }

    private void HandleTap(Vector2 position)
    {
        // Raycast to see what was tapped
        Ray ray = xrCamera.ScreenPointToRay(position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check what was hit and delegate to the appropriate handler
            if (hit.transform.CompareTag("Enemy"))
            {
                Debug.Log("Enemy tapped");
                // Handle enemy interaction
            }
            else if (hit.transform.CompareTag("Treasure"))
            {
                Debug.Log("Treasure tapped");
                // Handle treasure collection
            }
            else if (hit.transform.CompareTag("Room"))
            {
                Debug.Log("Room tapped");
                // Handle room navigation
            }
        }
    }
}

