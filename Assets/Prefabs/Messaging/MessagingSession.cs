using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Session state survives scene changes alongside the existing GameManager.</summary>
public sealed class MessagingSession : MonoBehaviour
{
    public sealed class Entry
    {
        public string text;
        public bool fromPlayer;
        public bool read;
        public string timeLabel;
    }
    public sealed class Thread
    {
        public readonly List<Entry> messages = new List<Entry>();
        public bool replied;
        public int replyIndex = -1;
    }
    private readonly Dictionary<string,Thread> threads = new Dictionary<string,Thread>();
    public event Action Changed;
    public string SelectedId;
    public static MessagingSession Get()
    {
        var gm = GameManager.EnsureInstance();
        var state = gm.GetComponent<MessagingSession>();
        return state != null ? state : gm.gameObject.AddComponent<MessagingSession>();
    }
    public Thread GetThread(string id) => threads.TryGetValue(id,out var thread) ? thread : null;
    public void Unlock(MessageConversation conversation, int day)
    {
        if (conversation == null || string.IsNullOrWhiteSpace(conversation.id) || day < conversation.availableFromDay || threads.ContainsKey(conversation.id)) return;
        var thread = new Thread();
        thread.messages.Add(new Entry { text = conversation.openingMessage, timeLabel = CurrentTimeLabel() });
        threads.Add(conversation.id,thread); Changed?.Invoke();
    }
    public int Unread(string id)
    {
        var thread = GetThread(id); if (thread == null) return 0;
        int count = 0; foreach (var entry in thread.messages) if (!entry.fromPlayer && !entry.read) count++;
        return count;
    }
    public void Read(string id)
    {
        var thread = GetThread(id); if (thread == null) return;
        bool changed = false;
        foreach (var entry in thread.messages)
            if (!entry.fromPlayer && !entry.read) { entry.read = true; changed = true; }
        SelectedId = id; if (changed) Changed?.Invoke();
    }
    public bool Reply(MessageConversation conversation, int index)
    {
        if (conversation == null || conversation.replies == null || index < 0 || index >= conversation.replies.Length) return false;
        var thread = GetThread(conversation.id); var reply = conversation.replies[index];
        if (thread == null || thread.replied || reply == null) return false;
        // Set the guard before invoking game events: repeated clicks never reapply rewards.
        thread.replied = true; thread.replyIndex = index;
        thread.messages.Add(new Entry { text = reply.text, fromPlayer = true, read = true, timeLabel = CurrentTimeLabel() });
        if (!string.IsNullOrEmpty(reply.response)) thread.messages.Add(new Entry { text = reply.response, timeLabel = CurrentTimeLabel() });
        var gm = GameManager.EnsureInstance();
        gm.ChangeConnectednessStat(reply.connectednessDelta);
        gm.ChangeSteveSuspicion(reply.suspicionDelta);
        Changed?.Invoke(); return true;
    }
    // Story scripts can deliver new authored messages to an already unlocked thread.
    public bool Receive(string id, string text)
    {
        var thread = GetThread(id); if (thread == null || string.IsNullOrEmpty(text)) return false;
        thread.messages.Add(new Entry { text = text, timeLabel = CurrentTimeLabel() }); Changed?.Invoke(); return true;
    }

    private static string CurrentTimeLabel() => DateTime.Now.ToString("h:mm tt");
}

