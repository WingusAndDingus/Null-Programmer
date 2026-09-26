using UnityEngine;

[DisallowMultipleComponent]
public class WindowIdentity : MonoBehaviour
{
    [SerializeField] private string windowId;

    public string Id => windowId;

    public bool HasValidId =>
        !string.IsNullOrWhiteSpace(windowId);

    private void OnValidate()
    {
        if (windowId != null)
            windowId = windowId.Trim();
    }
}