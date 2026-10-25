using System;
using UnityEngine;

[Serializable]
public sealed class NarrativeMessageData
{
    [HideInInspector] public string messageId;
    [Tooltip("Reuse the same key for future messages from this contact, e.g. friend.")]
    public string contactKey = "friend";
    public string contactName = "Friend";
    public Sprite portrait;
    [TextArea(3, 8)] public string text;
    public MessageConversation.Reply[] replies = new MessageConversation.Reply[0];
    [Tooltip("Keep this narrative step active until the player replies on the right monitor.")]
    public bool waitForReply;
}

// Reflection lets the additive files compile before the installer adds the message field.
public static class NarrativeMessageIds
{
    public static void Ensure(DaySequenceData day)
    {
        if (day == null || day.steps == null) return;
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var step in day.steps)
        {
            if (step == null) continue;
            var field = step.GetType().GetField("message");
            var message = field != null ? field.GetValue(step) as NarrativeMessageData : null;
            if (message == null) continue;
            if (string.IsNullOrEmpty(message.messageId) || !ids.Add(message.messageId))
            {
                message.messageId = Guid.NewGuid().ToString("N");
                ids.Add(message.messageId);
            }
        }
    }
}
