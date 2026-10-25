using System.Collections.Generic;
using UnityEngine;

public sealed class NarrativeMessageInbox : MonoBehaviour
{
    private readonly Dictionary<string, MessageConversation> contacts = new Dictionary<string, MessageConversation>();
    private readonly HashSet<string> delivered = new HashSet<string>();
    private readonly HashSet<string> replied = new HashSet<string>();
    private readonly Dictionary<string, string> activeMessage = new Dictionary<string, string>();
    private MessagingSession session;
    public IEnumerable<MessageConversation> Conversations => contacts.Values;

    public static NarrativeMessageInbox Get()
    {
        var owner = GameManager.EnsureInstance();
        var inbox = owner.GetComponent<NarrativeMessageInbox>();
        return inbox != null ? inbox : owner.gameObject.AddComponent<NarrativeMessageInbox>();
    }

    private void Awake()
    {
        session = MessagingSession.Get();
        session.Changed += TrackReplies;
    }

    public bool HasReplied(string messageId) => replied.Contains(messageId);

    public void Deliver(NarrativeMessageData message, int day)
    {
        if (delivered.Contains(message.messageId)) return;
        string contactId = "narrative_contact_" + message.contactKey.Trim().ToLowerInvariant();
        if (activeMessage.TryGetValue(contactId, out string previous) && !replied.Contains(previous))
        {
            // Keep history, but only the latest message's reply options are active.
            Debug.LogWarning("A new message from " + message.contactName + " replaces that contact's unanswered reply options.");
        }
        if (!contacts.TryGetValue(contactId, out var conversation))
        {
            conversation = ScriptableObject.CreateInstance<MessageConversation>();
            conversation.name = message.contactName + " (Narrative)";
            conversation.hideFlags = HideFlags.DontSave;
            conversation.id = contactId;
            conversation.availableFromDay = 1;
            contacts.Add(contactId, conversation);
        }
        conversation.contactName = message.contactName;
        conversation.avatar = message.portrait;
        conversation.openingMessage = message.text;
        conversation.replies = message.replies ?? new MessageConversation.Reply[0];
        activeMessage[contactId] = message.messageId;
        delivered.Add(message.messageId);

        var thread = session.GetThread(contactId);
        if (thread == null) session.Unlock(conversation, day);
        else
        {
            thread.replied = false;
            thread.replyIndex = -1;
            session.Receive(contactId, message.text);
        }
    }

    private void TrackReplies()
    {
        foreach (var pair in activeMessage)
        {
            var thread = session.GetThread(pair.Key);
            if (thread != null && thread.replied) replied.Add(pair.Value);
        }
    }

    private void OnDestroy()
    {
        if (session != null) session.Changed -= TrackReplies;
        foreach (var conversation in contacts.Values)
            if (conversation != null) Destroy(conversation);
    }
}
