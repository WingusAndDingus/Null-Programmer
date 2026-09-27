using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image dialogueBackground;
    [SerializeField] private GameObject internalMonologueOverlay; // Blurs/Fades screen
    [SerializeField] private AudioSource speechAudioSource;

    [Header("Personality Colors")]
    [SerializeField] private Color defaultTextColor = Color.white;
    [SerializeField] private Color architectColor = new Color(0.2f, 0.8f, 1.0f); // Cyan
    [SerializeField] private Color pragmatistColor = new Color(1.0f, 0.6f, 0.2f); // Orange
    [SerializeField] private Color sentinelColor = new Color(0.3f, 0.9f, 0.3f);   // Green
    [SerializeField] private Color geniusColor = new Color(0.9f, 0.8f, 0.2f);     // Yellow
    [SerializeField] private Color detectiveColor = new Color(0.8f, 0.4f, 1.0f);  // Purple

    private bool isDialogueActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator PlayDialogueLine(DialogueLine line)
    {
        isDialogueActive = true;

        // 1. Configure Colors & Speaker Name
        speakerText.text = line.speakerName;
        SetPersonalityStyle(line.personality);

        // 2. Play Audio if available
        if (line.voiceClip != null)
        {
            speechAudioSource.clip = line.voiceClip;
            speechAudioSource.Play();
        }

        // 3. Typewriter Effect
        dialogueText.text = "";
        foreach (char c in line.text.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.02f);
        }

        // 4. Hold dialogue for duration or wait for keypress
        yield return new WaitForSeconds(line.displayDuration);

        // Reset Overlay
        if (line.personality != HeadPersonality.None)
        {
            internalMonologueOverlay.SetActive(false);
            Time.timeScale = 1.0f; // Resume game
        }

        isDialogueActive = false;
    }

    private void SetPersonalityStyle(HeadPersonality personality)
    {
        bool isInternal = (personality != HeadPersonality.None);
        
        // Show/Hide blurred overlay and pause background game mechanics
        internalMonologueOverlay.SetActive(isInternal);
        Time.timeScale = isInternal ? 0.0f : 1.0f; 

        // Set Text Color based on personality
        switch (personality)
        {
            case HeadPersonality.GrandArchitect: dialogueText.color = architectColor; break;
            case HeadPersonality.Pragmatist:     dialogueText.color = pragmatistColor; break;
            case HeadPersonality.Sentinel:       dialogueText.color = sentinelColor; break;
            case HeadPersonality.Genius:         dialogueText.color = geniusColor; break;
            case HeadPersonality.Detective:      dialogueText.color = detectiveColor; break;
            default:                             dialogueText.color = defaultTextColor; break;
        }
    }
}
