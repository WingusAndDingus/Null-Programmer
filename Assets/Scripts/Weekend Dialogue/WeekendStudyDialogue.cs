using System.Collections;
using UnityEngine;

public class WeekendStudyDialogue : MonoBehaviour
{
    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] lines;

    private IEnumerator Start()
    {
        DialogueManager manager = DialogueManager.Instance;

        if (manager == null)
        {
            Debug.LogError("DialogueManager not found!");
            yield break;
        }

        foreach (DialogueLine line in lines)
        {
            if (line == null)
                continue;

            yield return manager.PlayDialogueLine(line);
        }

        Debug.Log("Weekend study dialogue finished.");
    }
}