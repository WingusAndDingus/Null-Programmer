using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsZoomedIn { get; private set; }

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
}