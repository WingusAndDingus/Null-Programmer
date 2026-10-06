using UnityEngine;
using UnityEngine.Video;

public class MonitorVideoRegister : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    private void OnEnable()
    {
        if (DialogueManager.Instance != null && videoPlayer != null)
        {
            DialogueManager.Instance.RegisterVideoPlayer(videoPlayer);
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null && videoPlayer != null)
        {
            DialogueManager.Instance.UnregisterVideoPlayer(videoPlayer);
        }
    }
}
