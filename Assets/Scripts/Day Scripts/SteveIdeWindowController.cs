using TMPro;
using UnityEngine;

public class SteveIdeWindowController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject idePanel;
    [SerializeField] private TMP_Text fileTab;
    [SerializeField] private DirectCodeTyping codeTyping;

    [Header("Display Settings")]
    [SerializeField] private string defaultFileName = "main.cpp";

    private void Start()
    {
        if (idePanel != null)
        {
            idePanel.SetActive(false);
        }

        if (codeTyping == null)
        {
            codeTyping = GetComponentInChildren<DirectCodeTyping>(true);
        }
    }

    public void OpenIde(SteveChallengeData challenge)
    {
        if (idePanel != null)
        {
            idePanel.SetActive(true);

            // 1. Force the Canvas sorting order to 10
            Canvas canvas = idePanel.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 9;
            }

            // 2. Force the Sorting Group order to 10 if it exists
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
            // Pass the challenge's custom starting template into the editor!
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
