using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class VideoDialogueCue
{
    [Tooltip("Timestamp in seconds where video seamlessly pauses")]
    public double pauseTimestampSeconds; 
    public DialogueLine dialogue;
}

public class ZoomScreenShareController : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private VideoPlayer screenShareVideoPlayer;
    
    [Header("Script Header / Sequence Configuration")]
    [Tooltip("Define all pauses and dialogue directly in the inspector")]
    public List<VideoDialogueCue> dialogueCues = new List<VideoDialogueCue>();

    private int currentCueIndex = 0;
    private bool isPausedForDialogue = false;

    private void Start()
    {
        if (screenShareVideoPlayer != null)
        {
            screenShareVideoPlayer.Play();
        }
    }

    private void Update()
    {
        if (isPausedForDialogue || currentCueIndex >= dialogueCues.Count) return;

        // Monitor video playback time
        if (screenShareVideoPlayer.time >= dialogueCues[currentCueIndex].pauseTimestampSeconds)
        {
            StartCoroutine(ExecutePauseAndDialogue(dialogueCues[currentCueIndex]));
        }
    }

    private IEnumerator ExecutePauseAndDialogue(VideoDialogueCue cue)
    {
        isPausedForDialogue = true;

        // 1. Pause video without visual indication (freezes current frame seamlessly)
        screenShareVideoPlayer.Pause();

        // 2. Trigger Dialogue via DialogueManager
        yield return StartCoroutine(DialogueManager.Instance.PlayDialogueLine(cue.dialogue));

        // 3. Resume video smoothly
        screenShareVideoPlayer.Play();
        
        currentCueIndex++;
        isPausedForDialogue = false;
    }
}
