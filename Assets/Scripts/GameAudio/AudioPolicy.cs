using System;

namespace NullProgrammer.Audio
{
    // Pure routing rules, independent of scene objects and audio playback.
    public static class AudioPolicy
    {
        public static bool IsDesktop(string name) => name != null &&
            (name.IndexOf("Desktop", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Monitor", StringComparison.OrdinalIgnoreCase) >= 0);
        public static bool IsNight(string name) => name != null &&
            (name.IndexOf("Night", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Sleep", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Weekend", StringComparison.OrdinalIgnoreCase) >= 0);
        public static MusicCue Resolve(string scene, bool activity, MusicCue manual)
        {
            if (manual != MusicCue.Auto && manual != MusicCue.Keep) return manual;
            if (IsNight(scene) || scene == "GameEndScreen") return MusicCue.Night;
            if (activity) return MusicCue.Activity;
            return MusicCue.Desktop;
        }
    }
}
