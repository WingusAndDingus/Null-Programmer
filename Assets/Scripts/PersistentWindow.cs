using System;
using UnityEngine;

public class PersistentWindow : MonoBehaviour
{
    [SerializeField] private WindowIdentity windowIdentity;
    [SerializeField] private GameObject windowRoot;

    [Header("Day 3 IDE Routing")]
    [Tooltip("Enable on the desktop IDE launcher only. Keeps Wednesday routing working if the legacy IDE window is destroyed.")]
    [SerializeField] private bool routesToDay3Workshop;

    private string cachedWindowId;

    private void Awake()
    {
        CacheWindowDetails();
    }

    private void CacheWindowDetails()
    {
        if (windowIdentity == null && windowRoot != null)
            windowIdentity = windowRoot.GetComponent<WindowIdentity>();

        if (windowIdentity != null && windowIdentity.HasValidId)
            cachedWindowId = windowIdentity.Id;

        // Preserve the old IDE-name routing rule, but remember the result while
        // the scene object still exists. Other windows do not become IDEs.
        if (windowRoot != null &&
            windowRoot.name.IndexOf("IDE", StringComparison.OrdinalIgnoreCase) >= 0)
            routesToDay3Workshop = true;
    }

    private void Start()
    {
        CacheWindowDetails();
        if (string.IsNullOrWhiteSpace(cachedWindowId))
            Debug.LogWarning("PersistentWindow needs a valid WindowIdentity for normal window persistence.", this);
        // Existing windows remain closed initially. Opening is user-driven.
    }

    public void CloseWindow()
    {
        CacheWindowDetails();
        var manager = GameManager.Instance;
        if (manager != null && !string.IsNullOrWhiteSpace(cachedWindowId))
        {
            manager.SetWindowOpen(cachedWindowId, false);
            manager.SetWindowMaximized(cachedWindowId, false);
        }

        if (windowRoot == null) return;
        NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.WindowClose);
        windowRoot.SetActive(false);
    }

    public void OpenWindow()
    {
        CacheWindowDetails();

        // Route before touching the legacy window. Unity considers a destroyed
        // GameObject null, even though a managed reference may still remain.
        if (routesToDay3Workshop &&
            NullProgrammer.Day3Workshop.WorkshopSession.TryOpenFromIDE())
            return;

        if (windowRoot == null)
        {
            Debug.LogWarning(
                "The window assigned to this launcher is missing or was destroyed. " +
                "For the desktop IDE, enable Routes To Day 3 Workshop on this component. " +
                "For other windows, assign a live Window Root in this scene.", this);
            return;
        }

        var manager = GameManager.Instance;
        if (manager != null && !string.IsNullOrWhiteSpace(cachedWindowId))
        {
            manager.SetWindowOpen(cachedWindowId, true);
            manager.BringWindowToFront(cachedWindowId);
        }

        NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.WindowOpen);
        windowRoot.SetActive(true);
    }
}
