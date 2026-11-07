using UnityEngine;
using UnityEngine.EventSystems;

namespace NullProgrammer.Audio
{
    // Feedback only; never changes or replaces an icon's existing EventTrigger.
    [DisallowMultipleComponent]
    public sealed class AudioIconFeedback : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData data)
        {
            var trigger = GetComponent<EventTrigger>();
            if (trigger != null && trigger.enabled && data.button == PointerEventData.InputButton.Left) GameAudio.Play(SfxCue.Click);
        }
    }
}
