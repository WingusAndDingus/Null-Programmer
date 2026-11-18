using System.Collections;
using TMPro;
using UnityEngine;

public class SteveIdeWindowController : MonoBehaviour
{
    public static SteveIdeWindowController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject idePanel;
    [SerializeField] private TMP_Text fileTab;
    [SerializeField] private DirectCodeTyping codeTyping;

    [Header("Display Settings")]
    [SerializeField] private string defaultFileName = "main.cpp";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Never destroy the scene's IDE hierarchy because of a stale singleton.
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (SteveScreenShareManager.HasPendingChallenge && codeTyping != null)
                SteveScreenShareManager.SaveEditingState(codeTyping.CurrentCode, codeTyping.CursorIndex);
            Instance = null;
        }
    }

    private void Start()
    {
        if (idePanel != null) idePanel.SetActive(false);
        if (codeTyping == null)
            codeTyping = GetComponentInChildren<DirectCodeTyping>(true);
        StartCoroutine(RestoreDesktopWindowNextFrame());
    }

    private IEnumerator RestoreDesktopWindowNextFrame()
    {
        // Wait for the other scene components' Start methods to finish.
        yield return null;
        if (SteveScreenShareManager.HasPendingChallenge) yield break;

        var identity = GetComponent<WindowIdentity>();
        var manager = GameManager.Instance;
        if (identity != null && identity.HasValidId && manager != null &&
            manager.TryGetWindowOpen(identity.Id, out bool wasOpen) && wasOpen &&
            idePanel != null)
        {
            idePanel.SetActive(true);
        }
    }

    public DirectCodeTyping CodeTyping => codeTyping;

    public void OpenIde(SteveChallengeData challenge)
    {
        if (idePanel != null)
        {
            idePanel.SetActive(true);

            // 1. Force the Canvas sorting order to 9
            Canvas canvas = idePanel.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9;
            }

            // 2. Force the Sorting Group order to 9 if it exists
            UnityEngine.Rendering.SortingGroup sortingGroup = idePanel.GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = 9;
            }
        }

        if (fileTab != null)
        {
            fileTab.text = defaultFileName;
        }

        if (codeTyping != null && challenge != null)
        {
            codeTyping.StartEditing(challenge.startingCode);
        }
        else if (codeTyping != null)
        {
            codeTyping.StartEditing("");
        }
        else
        {
            Debug.LogError("[SteveIdeWindowController] DirectCodeTyping component not assigned or found!");
        }
    }

    public void CloseIde()
    {
        if (idePanel != null)
        {
            idePanel.SetActive(false);
        }
    }
}
