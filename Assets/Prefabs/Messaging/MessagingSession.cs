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
        public bool isSystem;
        public bool read;
        public string timeLabel;
    }

    public sealed class Thread
    {
        public readonly List<Entry> messages = new List<Entry>();
        // True means there are currently no reply choices for the player.
        public bool replied;
        public int replyIndex = -1;
    }

    private readonly Dictionary<string, Thread> threads = new Dictionary<string, Thread>();
    public event Action Changed;
    public string SelectedId;

    public static MessagingSession Get()
    {
        var gm = GameManager.EnsureInstance();
        var state = gm.GetComponent<MessagingSession>();
        return state != null ? state : gm.gameObject.AddComponent<MessagingSession>();
    }

    public Thread GetThread(string id) => threads.TryGetValue(id, out var thread) ? thread : null;

    public void Unlock(MessageConversation conversation, int day, bool isSystem = false)
    {
        if (conversation == null || string.IsNullOrWhiteSpace(conversation.id) ||
            day < conversation.availableFromDay || threads.ContainsKey(conversation.id))
            return;

        var thread = new Thread();
        thread.messages.Add(new Entry
        {
            text = conversation.openingMessage,
            isSystem = isSystem,
            timeLabel = CurrentTimeLabel()
        });
        threads.Add(conversation.id, thread);

        if (!isSystem)
            NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.Message);
        Changed?.Invoke();
    }

    public int Unread(string id)
    {
        var thread = GetThread(id);
        if (thread == null) return 0;
        int count = 0;
        foreach (var entry in thread.messages)
            if (!entry.fromPlayer && !entry.isSystem && !entry.read) count++;
        return count;
    }

    public void Read(string id)
    {
        var thread = GetThread(id);
        if (thread == null) return;
        bool changed = false;
        foreach (var entry in thread.messages)
        {
            if (!entry.fromPlayer && !entry.read)
            {
                entry.read = true;
                changed = true;
            }
        }
        SelectedId = id;
        if (changed) Changed?.Invoke();
    }

    public bool Reply(MessageConversation conversation, int index)
    {
        if (conversation == null || conversation.replies == null ||
            index < 0 || index >= conversation.replies.Length)
            return false;

        var thread = GetThread(conversation.id);
        var reply = conversation.replies[index];
        if (thread == null || thread.replied || reply == null) return false;

        thread.replied = true;
        thread.replyIndex = index;
        thread.messages.Add(new Entry
        {
            text = reply.text,
            fromPlayer = true,
            read = true,
            timeLabel = CurrentTimeLabel()
        });
        if (!string.IsNullOrEmpty(reply.response))
            AddIncomingEntry(thread, reply.response, false, false);

        var gm = GameManager.EnsureInstance();
        gm.ChangeConnectednessStat(reply.connectednessDelta);
        gm.ChangeSteveSuspicion(reply.suspicionDelta);
        Changed?.Invoke();
        return true;
    }

    /// <summary>Appends an incoming chat message and plays the global notification SFX.</summary>
    public bool Receive(string id, string text)
    {
        return Receive(id, text, false, true);
    }

    public bool Receive(string id, string text, bool isSystem, bool playSound)
    {
        var thread = GetThread(id);
        if (thread == null || string.IsNullOrEmpty(text)) return false;
        AddIncomingEntry(thread, text, isSystem, false);
        if (playSound && !isSystem)
            NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.Message);
        Changed?.Invoke();
        return true;
    }

    public bool ReceiveSystem(string id, string text)
    {
        return Receive(id, text, true, false);
    }

    /// <summary>Appends a player response immediately while the next NPC lines are being paced out.</summary>
    public bool AppendPlayerMessage(string id, string text)
    {
        var thread = GetThread(id);
        if (thread == null || string.IsNullOrWhiteSpace(text)) return false;
        thread.messages.Add(new Entry
        {
            text = text,
            fromPlayer = true,
            read = true,
            timeLabel = CurrentTimeLabel()
        });
        Changed?.Invoke();
        return true;
    }

    /// <summary>Controls whether the contact currently has an outstanding response for the player.</summary>
    public void SetAwaitingReply(string id, bool awaitingPlayerChoice)
    {
        var thread = GetThread(id);
        if (thread == null) return;
        thread.replied = !awaitingPlayerChoice;
        thread.replyIndex = -1;
        Changed?.Invoke();
    }

    /// <summary>Legacy helper retained for any existing code that used the first integration pass.</summary>
    public bool AppendDialogueExchange(string id, string playerText, string[] incomingLines, bool awaitingPlayerChoice)
    {
        var thread = GetThread(id);
        if (thread == null) return false;

        if (!string.IsNullOrWhiteSpace(playerText))
            thread.messages.Add(new Entry { text = playerText, fromPlayer = true, read = true, timeLabel = CurrentTimeLabel() });

        if (incomingLines != null)
        {
            foreach (string line in incomingLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                AddIncomingEntry(thread, line, false, false);
                NullProgrammer.Audio.GameAudio.Play(NullProgrammer.Audio.SfxCue.Message);
            }
        }

        thread.replied = !awaitingPlayerChoice;
        thread.replyIndex = -1;
        Changed?.Invoke();
        return true;
    }

    private static void AddIncomingEntry(Thread thread, string text, bool isSystem, bool fromPlayer)
    {
        thread.messages.Add(new Entry
        {
            text = text,
            fromPlayer = fromPlayer,
            isSystem = isSystem,
            read = false,
            timeLabel = CurrentTimeLabel()
        });
    }

    private static string CurrentTimeLabel() => DateTime.Now.ToString("h:mm tt");
}
