using UnityEngine;
using UnityEngine.UI;

namespace NullProgrammer.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioButtonFeedback : MonoBehaviour
    {
        private Button button;
        private void Awake() { button = GetComponent<Button>(); if (button != null) button.onClick.AddListener(Click); }
        private void Click() { GameAudio.Play(SfxCue.Click); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Click); }
    }
}
