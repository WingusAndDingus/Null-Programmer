using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsZoomedIn { get; private set; }
    private Dictionary<string, bool> windowMaximizedStates =
        new Dictionary<string, bool>();
    private Dictionary<string, bool> windowOpenStates =
        new Dictionary<string, bool>();
    private Dictionary<string, Vector2> windowPositions =
        new Dictionary<string, Vector2>();
    private Dictionary<string, int> windowStackOrders =
        new Dictionary<string, int>();
    private int nextWindowStackOrder = 0;

    private void Awake()
    {
        // If another GameManager already exists, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep this GameManager when scenes change.
        DontDestroyOnLoad(gameObject);
    }

    public void SetZoomedIn(bool zoomedIn)
    {
        IsZoomedIn = zoomedIn;
    }

    public void SetWindowMaximized(string windowId, bool maximized)
    {
        windowMaximizedStates[windowId] = maximized;
    }

    public bool IsWindowMaximized(string windowId)
    {
        // If we've never seen this window before,
        // assume it starts unmaximized.
        if (windowMaximizedStates.TryGetValue(windowId, out bool maximized))
        {
            return maximized;
        }

        return false;
    }

    public void SetWindowOpen(string windowId, bool isOpen)
    {
        windowOpenStates[windowId] = isOpen;
    }

    public bool IsWindowOpen(string windowId)
    {
        if (windowOpenStates.TryGetValue(windowId, out bool isOpen))
        {
            return isOpen;
        }

        // If the GameManager has never seen this window before,
        // assume it should initially be open.
        return true;
    }

    public void SetWindowPosition(string windowId, Vector2 normalizedPosition)
    {
        windowPositions[windowId] = normalizedPosition;
    }

    public bool TryGetWindowPosition(string windowId, out Vector2 normalizedPosition)
    {
        return windowPositions.TryGetValue(windowId, out normalizedPosition);
    }

    public int GetOrCreateWindowStackOrder(string windowId)
    {
        if (windowStackOrders.TryGetValue(windowId, out int order))
        {
            return order;
        }

        order = nextWindowStackOrder;
        nextWindowStackOrder++;

        windowStackOrders[windowId] = order;

        return order;
    }

    public void BringWindowToFront(string windowId)
    {
        windowStackOrders[windowId] = nextWindowStackOrder;
        nextWindowStackOrder++;
    }
}