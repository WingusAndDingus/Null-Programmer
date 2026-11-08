using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneDialoguePlayer : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private float startDelay = 3f;

    [Header("Scene Background")]
    [InspectorName("Background Display (UI Image)")]
    [Tooltip("Assign the Image component that is actually displaying the scene background. Leave blank if the scene uses a SpriteRenderer instead.")]
    [SerializeField] private Image backgroundImage;

    [InspectorName("Background Display (Sprite Renderer)")]
    [Tooltip("Assign the SpriteRenderer that is actually displaying the scene background. Leave blank if the scene uses a UI Image instead.")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;

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

            ApplyBackgroundForLine(line);

            yield return manager.PlayDialogueLine(line);
        }

        Debug.Log("Scene dialogue finished.");
    }

    private void ApplyBackgroundForLine(DialogueLine line)
    {
        if (line.backgroundImage == null)
            return;

        bool applied = false;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = line.backgroundImage;
            applied = true;
        }

        if (backgroundSpriteRenderer != null)
        {
            backgroundSpriteRenderer.sprite = line.backgroundImage;
            applied = true;
        }

        if (!applied)
        {
            Debug.LogWarning(
                $"[SceneDialoguePlayer] Dialogue line '{line.speakerName}' requests background " +
                $"'{line.backgroundImage.name}', but no Background Display target is assigned. " +
                "Assign the scene's visible UI Image or SpriteRenderer on SceneDialoguePlayer.",
                this);
        }
    }
}
