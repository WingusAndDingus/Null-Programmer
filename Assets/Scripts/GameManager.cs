using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsZoomedIn { get; private set; }

    // All persistent state belonging to one logical window.
    private class WindowState
    {
        public bool IsOpen = true;
        public bool IsMaximized = false;

        public bool HasPosition = false;
        public Vector2 Position;

        public bool HasStackOrder = false;
        public int StackOrder;
    }

    // One WindowState for each logical window ID.
    private readonly Dictionary<string, WindowState> windowStates =
        new Dictionary<string, WindowState>();

    // Used to determine which window was most recently brought forward.
    private int nextWindowStackOrder = 0;


    private void Awake()
    {
        // If another GameManager already exists,
        // destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep this GameManager when scenes change.
        DontDestroyOnLoad(gameObject);
    }


    // =========================================================
    // Camera Zoom
    // =========================================================

    public void SetZoomedIn(bool zoomedIn)
    {
        IsZoomedIn = zoomedIn;
    }


    // =========================================================
    // Window State
    // =========================================================

    private WindowState GetWindowState(string windowId)
    {
        if (!windowStates.TryGetValue(windowId, out WindowState state))
        {
            state = new WindowState();
            windowStates[windowId] = state;
        }

        return state;
    }


    // =========================================================
    // Maximized State
    // =========================================================

    public void SetWindowMaximized(string windowId, bool maximized)
    {
        GetWindowState(windowId).IsMaximized = maximized;
    }

    public bool IsWindowMaximized(string windowId)
    {
        return GetWindowState(windowId).IsMaximized;
    }


    // =========================================================
    // Open / Closed State
    // =========================================================

    public void SetWindowOpen(string windowId, bool isOpen)
    {
        GetWindowState(windowId).IsOpen = isOpen;
    }

    public bool IsWindowOpen(string windowId)
    {
        return GetWindowState(windowId).IsOpen;
    }


    // =========================================================
    // Window Position
    // =========================================================

    public void SetWindowPosition(string windowId, Vector2 normalizedPosition)
    {
        WindowState state = GetWindowState(windowId);

        state.Position = normalizedPosition;
        state.HasPosition = true;
    }

    public bool TryGetWindowPosition(string windowId, out Vector2 normalizedPosition)
    {
        WindowState state = GetWindowState(windowId);

        if (state.HasPosition)
        {
            normalizedPosition = state.Position;
            return true;
        }

        normalizedPosition = default;
        return false;
    }


    // =========================================================
    // Window Stack Order
    // =========================================================

    public int GetOrCreateWindowStackOrder(string windowId)
    {
        WindowState state = GetWindowState(windowId);

        if (!state.HasStackOrder)
        {
            state.StackOrder = nextWindowStackOrder;
            state.HasStackOrder = true;
            nextWindowStackOrder++;
        }

        return state.StackOrder;
    }

    public void BringWindowToFront(string windowId)
    {
        WindowState state = GetWindowState(windowId);
        state.StackOrder = nextWindowStackOrder;
        state.HasStackOrder = true;
        nextWindowStackOrder++;
    }
}