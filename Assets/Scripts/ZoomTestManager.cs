using UnityEngine;

public class ZoomTestManager : MonoBehaviour
{
    [Header("Testing Instructions")]
    [Tooltip("Enter Play Mode, then click the 3 dots (⋮) on this script's component in the Inspector to trigger test actions.")]
    public string note = "Use Context Menu (3 dots) during Play Mode";

    // ------------------------------------------------------------------------
    // CONTEXT MENU BUTTONS (Right-Click / 3 Dots in Inspector during Play Mode)
    // ------------------------------------------------------------------------

    [ContextMenu("1. Trigger Incoming Call")]
    public void TestTriggerIncomingCall()
    {
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.TriggerIncomingCall();
            Debug.Log("[ZoomTestManager] Triggered Incoming Call Popup.");
        }
    }

    [ContextMenu("2. Accept Call")]
    public void TestAcceptCall()
    {
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.AcceptZoomCall();
            Debug.Log("[ZoomTestManager] Accepted Call & Opened Zoom Panel.");
        }
    }

    [ContextMenu("3. Start Screen Share")]
    public void TestStartScreenShare()
    {
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.StartScreenShare();
            Debug.Log("[ZoomTestManager] Started Screen Share Sequence.");
        }
    }

    [ContextMenu("4. End Screen Share")]
    public void TestEndScreenShare()
    {
        if (ZoomScreenShareController.Instance != null)
        {
            ZoomScreenShareController.Instance.EndScreenShare();
            Debug.Log("[ZoomTestManager] Ended Screen Share.");
        }
    }
}
