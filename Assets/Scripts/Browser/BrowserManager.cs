using UnityEngine;
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;

public class BrowserManager : MonoBehaviour
{
    public static BrowserManager Instance { get; private set; }

    [Header("UWB")]
    [SerializeField]
    private BaseUwbClientManager clientManager;

    [SerializeField]
    private WebBrowserUIBasic browserUI;

    [Header("Visibility")]
    [SerializeField]
    private CanvasGroup browserCanvasGroup;

    private string pendingUrl;

    public bool IsReady =>
        clientManager != null &&
        clientManager.browserClient != null &&
        clientManager.browserClient.IsConnected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad requires a root object.
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // A Day URL may be requested before CEF has finished starting.
        // If that happens, keep it queued until the browser is ready.
        if (!string.IsNullOrWhiteSpace(pendingUrl) && IsReady)
        {
            string url = pendingUrl;
            pendingUrl = null;

            clientManager.NavigateUrl(url);
        }
    }

    public void NavigateTo(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (IsReady)
        {
            clientManager.NavigateUrl(url);
        }
        else
        {
            pendingUrl = url;
        }
    }

    public void GoBack()
    {
        if (IsReady)
        {
            clientManager.GoBack();
        }
    }

    public void GoForward()
    {
        if (IsReady)
        {
            clientManager.GoForward();
        }
    }

    public void Reload()
    {
        if (IsReady)
        {
            clientManager.Refresh();
        }
    }

    public void SetVisible(bool visible)
    {
        if (browserCanvasGroup != null)
        {
            browserCanvasGroup.alpha = visible ? 1f : 0f;
            browserCanvasGroup.interactable = visible;
            browserCanvasGroup.blocksRaycasts = visible;
        }

        if (browserUI != null)
        {
            browserUI.disableMouseInputs = !visible;
            browserUI.disableKeyboardInputs = !visible;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}