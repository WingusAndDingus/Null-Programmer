public static class NarrativeDialogueAudio
{
    public static DialogueLine Resolve(NarrativeStep step, int currentLevel)
    {
        if (step == null) return null;
        NarrativeLineVariant selected = null;
        int bestLevel = int.MinValue;
        if (step.lineVariants != null)
        {
            foreach (var variant in step.lineVariants)
            {
                if (variant == null || string.IsNullOrEmpty(variant.lineText)) continue;
                int required = (int)variant.requiredLevel;
                if (currentLevel >= required && required > bestLevel)
                {
                    selected = variant;
                    bestLevel = required;
                }
            }
        }

        return new DialogueLine
        {
            speakerName = step.speakerName,
            personality = step.personality,
            text = selected != null ? selected.lineText : step.lineText,
            // A selected alternate text uses its own recording, or blips if blank.
            // Never play the base recording against different variant text.
            voiceClip = selected != null ? selected.voiceClip : step.voiceClip,
            displayDuration = step.displayDuration
        };
    }
}
