using System;
using System.Collections.Generic;
using UnityEngine;

namespace NullProgrammer.Audio
{
    [DisallowMultipleComponent]
    public sealed class DialogueBlipPlayer : MonoBehaviour
    {
        private readonly DialogueBlipCadence cadence = new DialogueBlipCadence();
        private readonly Dictionary<string, AudioClip> defaults = new Dictionary<string, AudioClip>();
        private AudioCatalog catalog;
        private AudioSource source;
        private AudioClip clip;
        private bool hasRecordedVoice;

        public void Begin(DialogueLine line)
        {
            Stop();
            cadence.Reset();
            hasRecordedVoice = line == null || line.voiceClip != null;
            catalog = AudioDirector.Ensure()?.Catalog;
            clip = line != null && catalog != null ? ChooseClip(line) : null;
            if (hasRecordedVoice || clip == null || !catalog.dialogueBlipsEnabled) return;
            if (source == null)
            {
                var child = new GameObject("Dialogue Blips");
                child.transform.SetParent(transform, false);
                source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.priority = 100;
            }
            source.clip = clip;
        }

        private AudioClip ChooseClip(DialogueLine line)
        {
            string name = "Default";
            AudioClip custom = catalog.dialogueDefaultBlip;
            switch (line.personality)
            {
                case HeadPersonality.GrandArchitect: name = "Architect"; custom = catalog.dialogueArchitectBlip; break;
                case HeadPersonality.Sentinel: name = "Sentinel"; custom = catalog.dialogueSentinelBlip; break;
                case HeadPersonality.Pragmatist: name = "Pragmatist"; custom = catalog.dialoguePragmatistBlip; break;
                case HeadPersonality.Genius: name = "Genius"; custom = catalog.dialogueGeniusBlip; break;
                case HeadPersonality.Detective: name = "Detective"; custom = catalog.dialogueDetectiveBlip; break;
                case HeadPersonality.None:
                    if (string.Equals(line.speakerName?.Trim(), "Steve", StringComparison.OrdinalIgnoreCase))
                    { name = "Steve"; custom = catalog.dialogueSteveBlip; }
                    break;
            }
            if (custom != null) return custom;
            if (!defaults.TryGetValue(name, out var result))
            {
                result = Resources.Load<AudioClip>("NullProgrammerAudio/DialogueBlips/" + name);
                defaults[name] = result;
            }
            return result;
        }

        public void RevealCharacter(char character, bool allowed)
        {
            if (!allowed || hasRecordedVoice || catalog == null ||
                !catalog.dialogueBlipsEnabled || AudioSettingsPanel.IsOpen)
            {
                Stop();
                return;
            }
            if (source == null || clip == null) return;
            if (!cadence.ShouldPlay(character, Time.unscaledTime,
                catalog.dialogueBlipCharactersPerSound, catalog.dialogueBlipMinimumInterval)) return;

            // A dedicated source prevents blips from cutting off clicks/notifications.
            source.Stop();
            source.volume = GameAudio.EffectsLevel * Mathf.Clamp01(catalog.dialogueBlipGain);
            float variation = Mathf.Clamp(catalog.dialogueBlipPitchVariation, 0f, .25f);
            source.pitch = UnityEngine.Random.Range(1f - variation, 1f + variation);
            source.clip = clip;
            source.Play();
        }

        private void Update()
        {
            if (source == null || catalog == null) return;
            source.volume = GameAudio.EffectsLevel * Mathf.Clamp01(catalog.dialogueBlipGain);
            if (!catalog.dialogueBlipsEnabled || AudioSettingsPanel.IsOpen) Stop();
        }

        public void Stop()
        {
            if (source != null) source.Stop();
        }

        private void OnDisable() { Stop(); }
        private void OnDestroy() { Stop(); }
    }
}
