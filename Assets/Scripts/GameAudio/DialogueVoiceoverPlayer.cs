using UnityEngine;

namespace NullProgrammer.Audio
{
    [DisallowMultipleComponent]
    public sealed class DialogueVoiceoverPlayer : MonoBehaviour
    {
        private AudioSource source;
        private AudioCatalog catalog;
        private bool active;
        private bool paused;
        private bool hidden;

        public void Begin(DialogueLine line, AudioSource assignedSource = null)
        {
            Stop();
            hidden = false;
            if (line == null || line.voiceClip == null) return;
            catalog = AudioDirector.Ensure()?.Catalog;
            if (assignedSource != null) source = assignedSource;
            if (source == null)
            {
                var child = new GameObject("Dialogue Voice Over");
                child.transform.SetParent(transform, false);
                source = child.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.pitch = 1f;
            source.priority = 64;
            source.clip = line.voiceClip;
            ApplyVolume();
            active = true;
            source.Play();
            RefreshPause();
        }

        private void ApplyVolume()
        {
            if (source != null)
                source.volume = GameAudio.EffectsLevel *
                    (catalog != null ? Mathf.Clamp01(catalog.dialogueVoiceoverGain) : 1f);
        }

        public void SetHidden(bool value)
        {
            hidden = value;
            RefreshPause();
        }

        private void Update()
        {
            if (!active || source == null) return;
            ApplyVolume();
            RefreshPause();
        }

        private void RefreshPause()
        {
            if (!active || source == null) return;
            bool shouldPause = hidden || AudioSettingsPanel.IsOpen;
            if (shouldPause && !paused && source.isPlaying)
            {
                source.Pause();
                paused = true;
            }
            else if (!shouldPause && paused)
            {
                source.UnPause();
                paused = false;
            }
        }

        public void Stop()
        {
            if (source != null && active) source.Stop();
            active = false;
            paused = false;
        }

        private void OnDisable() { Stop(); }
        private void OnDestroy() { Stop(); }
    }
}
