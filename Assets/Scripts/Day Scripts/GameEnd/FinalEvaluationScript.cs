using System;
using UnityEngine;

[Serializable]
internal sealed class FinalEvaluationScript
{
    internal const string ResourcePath = "FinalEvaluation/EndingReview";

    public Line[] opening;
    public Route[] routes;

    [Serializable]
    internal sealed class Route
    {
        public string ending;
        public Beat[] beats;
    }

    [Serializable]
    internal sealed class Beat
    {
        // An empty view continues the current shot.
        public string view;
        public string title;
        public string detail;
        public Line[] lines;
    }

    [Serializable]
    internal sealed class Line
    {
        public string speaker;
        public string text;
        public string personality;

        public DialogueLine ToDialogueLine()
        {
            Enum.TryParse(personality, out HeadPersonality voice);
            return new DialogueLine { speakerName = speaker, text = text, personality = voice };
        }
    }

    internal Route FindRoute(GameEndingType ending)
    {
        string key = ending.ToString();
        return Array.Find(routes ?? Array.Empty<Route>(), route => route != null && route.ending == key);
    }

    internal static FinalEvaluationScript Load()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        return asset == null ? null : JsonUtility.FromJson<FinalEvaluationScript>(asset.text);
    }
}
