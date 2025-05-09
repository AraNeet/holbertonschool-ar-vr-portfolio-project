using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

public class XRTrackingOptimizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private GameObject anchoredObject;

    [Header("Tracking Quality")]
    [SerializeField] private GameObject trackingLostWarning;
    [SerializeField] private float trackingLostThreshold = 3.0f;

    private ARCameraManager cameraManager;
    private bool wasTrackingLost = false;
    private float trackingLostTime = 0;

    void Start()
    {
        // Auto-find components if not assigned
        if (arSession == null)
            arSession = FindObjectOfType<ARSession>();

        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();

        // Get camera manager from XR Origin
        if (xrOrigin != null)
            cameraManager = xrOrigin.Camera.GetComponent<ARCameraManager>();

        if (trackingLostWarning != null)
            trackingLostWarning.SetActive(false);
    }

    void Update()
    {
        // Monitor tracking state
        if (cameraManager != null)
        {
            bool isTracking = IsCurrentlyTracking();

            // Show/hide warning
            if (trackingLostWarning != null)
            {
                if (!isTracking)
                {
                    trackingLostTime += Time.deltaTime;

                    // Only show warning after threshold is passed to avoid flicker
                    if (trackingLostTime > trackingLostThreshold && !wasTrackingLost)
                    {
                        trackingLostWarning.SetActive(true);
                        wasTrackingLost = true;
                    }
                }
                else
                {
                    trackingLostTime = 0;

                    if (wasTrackingLost)
                    {
                        trackingLostWarning.SetActive(false);
                        wasTrackingLost = false;
                    }
                }
            }
        }
    }

    private bool IsCurrentlyTracking()
    {
        // Check if AR tracking is active
        return ARSession.state == ARSessionState.SessionTracking;
    }

    // Method that can be called to try and improve tracking
    public void AttemptTrackingRecovery()
    {
        // Reset AR session (sometimes helps when tracking is lost)
        if (arSession != null)
        {
            arSession.Reset();
        }
    }
}
