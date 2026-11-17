using UnityEngine;
using NullProgrammer.Audio;

/// <summary>
/// Put this on an object in the StartScreen scene (for example, Canvas).
/// Wire your scene-authored Settings/Credits Button OnClick events to these
/// public methods. The buttons remain fully editable in the Unity Editor.
/// </summary>
public sealed class TitleScreenSettingsButtons : MonoBehaviour
{
    public void OpenSettings()
    {
        AudioSettingsPanel panel = FindFirstObjectByType<AudioSettingsPanel>();
        if (panel != null)
            panel.OpenTitleSettings();
        else
            Debug.LogWarning("Settings UI: AudioDirector/AudioSettingsPanel not found. Ensure AudioDirector is present before opening Settings.");
    }

    public void OpenCredits()
    {
        AudioSettingsPanel panel = FindFirstObjectByType<AudioSettingsPanel>();
        if (panel != null)
            panel.OpenTitleCredits();
        else
            Debug.LogWarning("Settings UI: AudioDirector/AudioSettingsPanel not found. Ensure AudioDirector is present before opening Credits.");
    }
}
