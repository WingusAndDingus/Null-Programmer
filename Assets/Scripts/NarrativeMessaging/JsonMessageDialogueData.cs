using System;

// These classes mirror the structure of the JSON files in Assets/Scripts/Messages Data.
// The unique names avoid colliding with the existing DialogueLine / DialogueChoice classes.
[Serializable]
public sealed class JsonMessageDialogueFile
{
    public string conversationId;
    public int day;
    public string contact;
    public string description;
    public string systemBanner;
    public string startNode;
    public JsonMessageOpening[] openingMessages;
    public JsonMessageNode[] nodes;
}

[Serializable]
public sealed class JsonMessageOpening
{
    public string speaker;
    public string text;
    public string channel;
    public string timestamp;
}

[Serializable]
public sealed class JsonMessageNode
{
    public string id;
    public JsonMessageLine[] lines;
    public JsonMessageChoice[] choices;
    public JsonMessageEffect[] onEnter;
}

[Serializable]
public sealed class JsonMessageLine
{
    public string speaker;
    public string text;
    public string channel;
}

[Serializable]
public sealed class JsonMessageChoice
{
    public string text;
    public string nextNode;
    public JsonMessageCondition[] conditions;
    public JsonMessageEffect[] effects;
}

[Serializable]
public sealed class JsonMessageCondition
{
    public string type;
    public string key;
    public bool boolValue;
    public int intValue;
}

[Serializable]
public sealed class JsonMessageEffect
{
    public string type;
    public string key;
    public bool boolValue;
    public int intValue;
}
