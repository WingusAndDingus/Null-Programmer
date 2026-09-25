using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsZoomedIn { get; private set; }
    private Dictionary<string, bool> windowMaximizedStates =
        new Dictionary<string, bool>();

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
}