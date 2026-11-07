using UnityEngine;

namespace NullProgrammer.Audio
{
    public static class GameAudio
    {
        public static float MusicLevel => AudioDirector.Current != null ? AudioDirector.Current.Music : 1f;
        public static float EffectsLevel => AudioDirector.Current != null ? AudioDirector.Current.Effects : 1f;
        // Automatically starts in any playable scene; never creates a GameManager.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { AudioDirector.ClearStatic(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot() { AudioDirector.Ensure(); }

        public static void Play(SfxCue cue) { if (Application.isPlaying) AudioDirector.Ensure()?.Play(cue); }
        public static void BeginActivity(Component owner) { if (Application.isPlaying) AudioDirector.Ensure()?.Activity(owner, true); }
        public static void EndActivity(Component owner) { if (AudioDirector.Current != null) AudioDirector.Current.Activity(owner, false); }
        public static void Step(MusicCue music, SfxCue sound)
        {
            if (!Application.isPlaying) return;
            var director = AudioDirector.Ensure();
            if (director == null) return;
            if (music != MusicCue.Keep) director.SetMusic(music);
            if (sound != SfxCue.None) director.Play(sound);
        }
        public static void EmailAvailable(int day, bool read)
        {
            if (Application.isPlaying && !read) AudioDirector.Ensure()?.EmailAvailable(day);
        }
    }
}
