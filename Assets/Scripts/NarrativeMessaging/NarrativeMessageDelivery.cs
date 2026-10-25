using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class NarrativeMessageDelivery
{
    public static async Task RunAsync(NarrativeMessageData message, int day, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (message == null || string.IsNullOrWhiteSpace(message.messageId) ||
            string.IsNullOrWhiteSpace(message.contactKey) || string.IsNullOrWhiteSpace(message.text))
            throw new InvalidOperationException("Receive Message needs a contact key and message text. Save the day asset to generate its message ID.");
        if (message.waitForReply && (message.replies == null ||
            !Array.Exists(message.replies, reply => reply != null)))
            throw new InvalidOperationException("Wait For Reply needs at least one reply option.");
        var inbox = NarrativeMessageInbox.Get();
        inbox.Deliver(message, day);
        while (message.waitForReply && !inbox.HasReplied(message.messageId))
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
        token.ThrowIfCancellationRequested();
    }

    public static MessageConversation[] GetConversations(MessageConversation[] authored)
    {
        var all = new List<MessageConversation>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var conversation in authored ?? new MessageConversation[0])
            if (conversation != null && ids.Add(conversation.id)) all.Add(conversation);
        foreach (var conversation in NarrativeMessageInbox.Get().Conversations)
            if (conversation != null && ids.Add(conversation.id)) all.Add(conversation);
        return all.ToArray();
    }
}

