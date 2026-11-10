using System;

namespace NullProgrammer.Audio
{
    // Independent of Unity so the cadence can be checked without playing audio.
    public sealed class DialogueBlipCadence
    {
        private int letters;
        private float lastSound = float.NegativeInfinity;

        public void Reset()
        {
            letters = 0;
            lastSound = float.NegativeInfinity;
        }

        public bool ShouldPlay(char character, float time, int every, float interval)
        {
            if (!char.IsLetterOrDigit(character)) return false;
            letters++;
            every = Math.Max(1, every);
            if ((letters - 1) % every != 0) return false;
            if (time - lastSound < Math.Max(0f, interval)) return false;
            lastSound = time;
            return true;
        }
    }
}
