using UnityEngine;

/// <summary>Unlock only after an observed open -> closed transition for a read email.</summary>
public sealed class EmailReplyGate : MonoBehaviour
{
    [SerializeField] private IntegratedMailInbox inbox;
    private bool wasOpen;
    private GameManager state;
    public void Configure(IntegratedMailInbox value) { inbox = value; }
    private void Start()
    {
        state = GameManager.EnsureInstance();
        wasOpen = inbox != null && inbox.WindowObject != null && inbox.WindowObject.activeInHierarchy;
    }
    private void LateUpdate()
    {
        if (state == null || inbox == null || inbox.WindowObject == null) return;
        bool open = inbox.WindowObject.activeInHierarchy;
        if (inbox.IsRead) state.MarkEmailRead();
        if (wasOpen && !open && inbox.IsRead) state.MarkReadEmailClosed();
        wasOpen = open;
    }
}
