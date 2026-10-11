using System.Collections;
using UnityEngine;

public class SceneDialoguePlayer : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private float startDelay = 3f;

    [Header("Dialogue Lines")]
    [SerializeField] private DialogueLine[] lines;

    private IEnumerator Start()
    {
        // Wait before displaying the first dialogue line.
        yield return new WaitForSeconds(startDelay);

        DialogueManager manager = DialogueManager.Instance;

        if (manager == null)
        {
            Debug.LogError("DialogueManager not found!");
            yield break;
        }

        // Play each dialogue line in sequence.
        foreach (DialogueLine line in lines)
        {
            if (line == null)
                continue;

            yield return manager.PlayDialogueLine(line);
        }

        Debug.Log("Scene dialogue finished.");
    }
}