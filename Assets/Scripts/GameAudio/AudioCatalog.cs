using System;
using UnityEngine;

namespace NullProgrammer.Audio
{
    public enum MusicCue { Keep = 0, Auto = 1, Desktop = 2, Activity = 3, Night = 4, Silence = 5 }
    public enum SfxCue
    {
        None = 0, Click = 1, WindowOpen = 2, WindowClose = 3, DialogueReveal = 4,
        DialogueAdvance = 5, Choice = 6, Email = 7, Message = 8, IncomingCall = 9,
        CallAccept = 10, DayStart = 11, Place = 12, Run = 13, Pass = 14, Fail = 15,
        Skill = 16, Suspicion = 17
    }

    [CreateAssetMenu(menuName = "Null Programmer/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Range(0f, 1f)]
        public float defaultMusicVolume = 0.20f;
        public AudioClip desktop, activity, night;
        [Range(.1f, 5f)] public float crossfadeSeconds = 1.5f;
        public bool showAudioButton = true;
        public Sound[] sounds = new Sound[0];
        [TextArea(6, 15)] public string credits;

        [Serializable] public sealed class Sound
        {
            public SfxCue cue;
            public AudioClip clip;
            [Range(0f, 1f)] public float gain = .6f;
            [Range(0f, 3f)] public float cooldown = .08f;
        }

        public Sound Find(SfxCue cue)
        {
            if (sounds == null) return null;
            foreach (var sound in sounds)
                if (sound != null && sound.cue == cue) return sound;
            return null;
        }
    }
}
