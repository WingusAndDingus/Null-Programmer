using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runs one JSON-authored messaging conversation through the existing
/// NarrativeMessageInbox, MessagingSession, and MonitorMessagingApp.
/// Attach this component to the same GameObject as MonitorMessagingApp.
/// </summary>
public sealed class JsonMessageDialogueController : MonoBehaviour
{
    [Header("Dialogue JSON")]
    [SerializeField] private TextAsset dialogueJson;
    [SerializeField] private Sprite contactPortrait;

    [Header("Day 3 Test Options")]
    [Tooltip("Enable to test the branch where Mom has already told Bjarne about his birthday.")]
    [SerializeField] private bool debugBirthdayAlreadyKnown;
    [Tooltip("Enable to test the branch where Bjarne already knows he was intoxicated from Day 2.")]
    [SerializeField] private bool debugIntoxicationAlreadyKnown;

    private JsonMessageDialogueFile dialogue;
    private JsonMessageNode currentNode;
    private MessageConversation runtimeConversation;
    private MessagingSession session;
    private List<JsonMessageChoice> visibleChoices = new List<JsonMessageChoice>();
    private string runtimeConversationId;
    private bool started;
    private bool finished;

    public bool HandlesConversation(string conversationId)
    {
        return runtimeConversation != null &&
               string.Equals(runtimeConversation.id, conversationId, StringComparison.Ordinal);
    }

    /// <summary>Called by MonitorMessagingApp whenever the in-game day changes.</summary>
    public void TryStartForDay(int currentDay)
    {
        if (started || dialogueJson == null)
            return;

        JsonMessageDialogueFile parsed;
        try
        {
            parsed = JsonUtility.FromJson<JsonMessageDialogueFile>(dialogueJson.text);
        }
        catch (Exception exception)
        {
            Debug.LogError("[JsonMessageDialogueController] Could not parse dialogue JSON: " + exception.Message, this);
            return;
        }

        if (parsed == null || parsed.day != currentDay)
            return;

        if (string.IsNullOrWhiteSpace(parsed.contact) ||
            string.IsNullOrWhiteSpace(parsed.startNode) ||
            parsed.nodes == null || parsed.nodes.Length == 0)
        {
            Debug.LogError("[JsonMessageDialogueController] JSON is missing contact, startNode, or nodes.", this);
            return;
        }

        dialogue = parsed;
        session = MessagingSession.Get();

        // Optional test switches let you exercise both conditional branches without
        // needing to replay Days 1 and 2 every time.
        GameManager game = GameManager.EnsureInstance();
        if (debugBirthdayAlreadyKnown)
            game.SetStoryFlag("C02_Birthday", true);
        if (debugIntoxicationAlreadyKnown)
            game.SetStoryFlag("C06_Intoxicated", true);

        string contactKey = NormalizeContactKey(dialogue.contact);
        runtimeConversationId = "narrative_contact_" + contactKey;

        JsonMessageOpening[] openings = dialogue.openingMessages ?? new JsonMessageOpening[0];
        string firstOpening = openings.Length > 0 && openings[0] != null
            ? openings[0].text
            : "...";

        if (!string.IsNullOrWhiteSpace(dialogue.systemBanner))
            firstOpening = dialogue.systemBanner + "\n\n" + firstOpening;

        var openingMessage = new NarrativeMessageData
        {
            messageId = (string.IsNullOrWhiteSpace(dialogue.conversationId)
                ? "json_day_" + currentDay
                : dialogue.conversationId) + "_opening",
            contactKey = contactKey,
            contactName = dialogue.contact,
            portrait = contactPortrait,
            text = firstOpening,
            replies = new MessageConversation.Reply[0],
            waitForReply = false
        };

        // Reuse the project's existing narrative inbox. This also reuses the same
        // Mark thread created by earlier narrative messages (if one already exists).
        NarrativeMessageInbox inbox = NarrativeMessageInbox.Get();
        inbox.Deliver(openingMessage, currentDay);

        runtimeConversation = inbox.Conversations.FirstOrDefault(c =>
            c != null && c.id == runtimeConversationId);

        if (runtimeConversation == null)
        {
            Debug.LogError("[JsonMessageDialogueController] Could not find the runtime contact '" +
                           runtimeConversationId + "' after delivering its opening message.", this);
            return;
        }

        // Any additional opening messages become separate incoming bubbles.
        for (int i = 1; i < openings.Length; i++)
        {
            if (openings[i] == null || string.IsNullOrWhiteSpace(openings[i].text))
                continue;
            session.Receive(runtimeConversationId, FormatLine(
                new JsonMessageLine
                {
                    speaker = openings[i].speaker,
                    text = openings[i].text,
                    channel = openings[i].channel
                }));
        }

        currentNode = FindNode(dialogue.startNode);
        if (currentNode == null)
        {
            Debug.LogError("[JsonMessageDialogueController] Start node '" + dialogue.startNode + "' was not found.", this);
            return;
        }

        ApplyEffects(currentNode.onEnter);
        SetVisibleChoices(currentNode);

        // Usually the start node has no lines (the opening message is the first line),
        // but this supports start nodes that do contain additional dialogue.
        string[] startLines = GetLineTexts(currentNode.lines);
        session.AppendDialogueExchange(runtimeConversationId, null, startLines, visibleChoices.Count > 0);

        started = true;
        finished = visibleChoices.Count == 0;
        Debug.Log("[JsonMessageDialogueController] Started JSON conversation '" +
                  dialogue.conversationId + "' for " + dialogue.contact + ".", this);
    }

    /// <summary>
    /// Called by MonitorMessagingApp instead of MessagingSession.Reply for a
    /// conversation owned by this JSON controller.
    /// </summary>
    public bool TrySelectChoice(string conversationId, int choiceIndex)
    {
        if (!started || finished || !HandlesConversation(conversationId))
            return false;

        if (choiceIndex < 0 || choiceIndex >= visibleChoices.Count)
        {
            Debug.LogWarning("[JsonMessageDialogueController] Choice index is out of range.", this);
            return false;
        }

        JsonMessageChoice selectedChoice = visibleChoices[choiceIndex];
        if (selectedChoice == null)
            return false;

        // Apply the player's chosen response effects before entering the next node.
        ApplyEffects(selectedChoice.effects);

        JsonMessageNode nextNode = FindNode(selectedChoice.nextNode);
        if (nextNode == null)
        {
            Debug.LogError("[JsonMessageDialogueController] Choice points to missing node '" +
                           selectedChoice.nextNode + "'. Ending this conversation safely.", this);
            runtimeConversation.replies = new MessageConversation.Reply[0];
            visibleChoices.Clear();
            finished = true;
            session.AppendDialogueExchange(runtimeConversationId, selectedChoice.text,
                new string[0], false);
            return true;
        }

        currentNode = nextNode;
        ApplyEffects(currentNode.onEnter);
        SetVisibleChoices(currentNode);

        bool waitingForPlayer = visibleChoices.Count > 0;
        string[] incomingLines = GetLineTexts(currentNode.lines);

        // This appends the outgoing player response and all NPC/internal lines as one
        // session update, then tells the existing UI whether another choice is available.
        session.AppendDialogueExchange(runtimeConversationId, selectedChoice.text,
            incomingLines, waitingForPlayer);

        finished = !waitingForPlayer;
        return true;
    }

    private void SetVisibleChoices(JsonMessageNode node)
    {
        visibleChoices.Clear();

        if (node != null && node.choices != null)
        {
            foreach (JsonMessageChoice choice in node.choices)
            {
                if (choice != null && EvaluateConditions(choice.conditions))
                    visibleChoices.Add(choice);
            }
        }

        runtimeConversation.replies = visibleChoices
            .Select(choice => new MessageConversation.Reply
            {
                text = choice.text,
                response = "",
                connectednessDelta = 0,
                suspicionDelta = 0
            })
            .ToArray();
    }

    private bool EvaluateConditions(JsonMessageCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        GameManager game = GameManager.EnsureInstance();
        foreach (JsonMessageCondition condition in conditions)
        {
            if (condition == null)
                continue;

            bool passed;
            switch ((condition.type ?? "").Trim().ToLowerInvariant())
            {
                case "flag":
                    passed = game.HasStoryFlag(condition.key) == condition.boolValue;
                    break;

                default:
                    Debug.LogWarning("[JsonMessageDialogueController] Unsupported condition type '" +
                                     condition.type + "'. Choice will be hidden.", this);
                    passed = false;
                    break;
            }

            // All listed conditions must pass.
            if (!passed)
                return false;
        }

        return true;
    }

    private void ApplyEffects(JsonMessageEffect[] effects)
    {
        if (effects == null)
            return;

        GameManager game = GameManager.EnsureInstance();
        foreach (JsonMessageEffect effect in effects)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.type))
                continue;

            switch (effect.type.Trim().ToLowerInvariant())
            {
                case "setflag":
                    game.SetStoryFlag(effect.key, effect.boolValue);
                    break;

                case "addstat":
                    game.AddStoryStat(effect.key, effect.intValue);
                    break;

                default:
                    Debug.LogWarning("[JsonMessageDialogueController] Unsupported effect type '" +
                                     effect.type + "'.", this);
                    break;
            }
        }
    }

    private JsonMessageNode FindNode(string nodeId)
    {
        if (dialogue == null || dialogue.nodes == null || string.IsNullOrWhiteSpace(nodeId))
            return null;

        return dialogue.nodes.FirstOrDefault(node => node != null &&
            string.Equals(node.id, nodeId, StringComparison.Ordinal));
    }

    private static string[] GetLineTexts(JsonMessageLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return new string[0];

        return lines
            .Where(line => line != null && !string.IsNullOrWhiteSpace(line.text))
            .Select(FormatLine)
            .ToArray();
    }

    private static string FormatLine(JsonMessageLine line)
    {
        string channel = (line.channel ?? "chat").Trim().ToLowerInvariant();
        if (channel == "internal")
            return "[" + (string.IsNullOrWhiteSpace(line.speaker) ? "Internal" : line.speaker) + "] " + line.text;
        if (channel == "system")
            return "[System] " + line.text;
        return line.text;
    }

    private static string NormalizeContactKey(string contact)
    {
        return (contact ?? "contact").Trim().ToLowerInvariant().Replace(" ", "_");
    }
}
