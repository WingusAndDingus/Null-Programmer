using UnityEngine;

public class DialogueTestBootstrap : MonoBehaviour
{
    [SerializeField]
    private DialogueManager dialogueManagerPrefab;

    private void Awake()
    {
        // If the DialogueManager already exists,
        // there is nothing we need to do.
        if (DialogueManager.Instance != null)
            return;

        if (dialogueManagerPrefab == null)
        {
            Debug.LogError(
                "DialogueSystem prefab has not been assigned!"
            );
            return;
        }

        // Create the DialogueManager and its UI.
        Instantiate(dialogueManagerPrefab);
    }
}