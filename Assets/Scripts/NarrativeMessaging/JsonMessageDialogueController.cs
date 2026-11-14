using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Loads every daily JSON conversation from Resources and runs each contact's
/// branching dialogue through the existing NarrativeMessageInbox/MessagingSession.
/// Attach this to the same GameObject as MonitorMessagingApp.
/// </summary>
public sealed class JsonMessageDialogueController : MonoBehaviour
{
    [Header("Automatic JSON Loading")]
    [Tooltip("Place the JSON files in Assets/Resources/Messages Data. Do not include 'Resources/' here.")]
    [SerializeField] private string resourcesFolder = "Messages Data";
    [Tooltip("Optional alternative to Resources: assign all conversation JSON TextAssets here once.")]
    [SerializeField] private TextAsset[] dialogueFileOverrides;

    [Header("Presentation")]
    [Min(0f)][SerializeField] private float messageIntervalSeconds = 1f;
    [SerializeField] private JsonMessageContactPortrait[] contactPortraits;

    [Header("Day 3 Testing Only")]
    [Tooltip("Temporarily sets C02_Birthday before Day 3 conversations start.")]
    [SerializeField] private bool debugBirthdayAlreadyKnown;
    [Tooltip("Temporarily sets C06_Intoxicated before Day 3 conversations start.")]
    [SerializeField] private bool debugIntoxicationAlreadyKnown;

    private sealed class RuntimeConversation
    {
        public JsonMessageDialogueFile data;
        public JsonMessageNode currentNode;
        public MessageConversation contact;
        public string contactId;
        public List<JsonMessageChoice> visibleChoices = new List<JsonMessageChoice>();
        public bool processing;
        public bool finished;
    }

    private readonly Dictionary<string, RuntimeConversation> activeConversations =
        new Dictionary<string, RuntimeConversation>(StringComparer.Ordinal);
    private readonly HashSet<string> startedConversationIds =
        new HashSet<string>(StringComparer.Ordinal);

    private List<JsonMessageDialogueFile> loadedFiles;
    private MessagingSession session;
    private int lastRequestedDay = -1;

    private void Awake()
    {
        session = MessagingSession.Get();
        LoadDialogueFiles();
    }

    public bool HandlesConversation(string conversationId)
    {
        return !string.IsNullOrEmpty(conversationId) &&
               activeConversations.ContainsKey(conversationId);
    }

    /// <summary>Called by MonitorMessagingApp when the current in-game day is known/changes.</summary>
    public void TryStartForDay(int currentDay)
    {
        if (lastRequestedDay == currentDay)
            return;

        lastRequestedDay = currentDay;
        LoadDialogueFiles();

        List<JsonMessageDialogueFile> todaysFiles = loadedFiles
            .Where(file => file != null && file.day == currentDay)
            .OrderBy(file => Mathf.Max(0f, file.delaySeconds))
            .ThenBy(file => file.contact, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (JsonMessageDialogueFile file in todaysFiles)
        {
            string id = string.IsNullOrWhiteSpace(file.conversationId)
                ? "Day" + currentDay + "_" + file.contact
                : file.conversationId;

            if (!startedConversationIds.Add(id))
                continue;

            StartCoroutine(StartConversationAfterDelay(file, currentDay));
        }

        Debug.Log("[JsonMessageDialogueController] Scheduled " + todaysFiles.Count +
                  " conversation(s) for Day " + currentDay + ".", this);
    }

    private void LoadDialogueFiles()
    {
        if (loadedFiles != null)
            return;

        loadedFiles = new List<JsonMessageDialogueFile>();
        TextAsset[] assets = dialogueFileOverrides != null && dialogueFileOverrides.Length > 0
            ? dialogueFileOverrides
            : Resources.LoadAll<TextAsset>(resourcesFolder);

        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("[JsonMessageDialogueController] No JSON TextAssets found. Move the dialogue files to " +
                           "Assets/Resources/" + resourcesFolder + " or assign Dialogue File Overrides.", this);
            return;
        }

        foreach (TextAsset asset in assets)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.text) ||
                !asset.text.Contains("\"conversationId\""))
                continue;

            try
            {
                JsonMessageDialogueFile parsed = JsonUtility.FromJson<JsonMessageDialogueFile>(asset.text);
                // manifest.json and the secret-ending file are intentionally skipped here:
                // they do not represent a normal day/contact conversation.
                if (parsed == null || parsed.day < 1 || parsed.day > 5 ||
                    string.IsNullOrWhiteSpace(parsed.contact) ||
                    string.IsNullOrWhiteSpace(parsed.startNode) || parsed.nodes == null || parsed.nodes.Length == 0 ||
                    (!string.IsNullOrWhiteSpace(parsed.conversationId) &&
                     parsed.conversationId.StartsWith("SecretEnding_", StringComparison.OrdinalIgnoreCase)))
                    continue;

                loadedFiles.Add(parsed);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[JsonMessageDialogueController] Skipping '" + asset.name + "': " +
                                 exception.Message, asset);
            }
        }

        Debug.Log("[JsonMessageDialogueController] Loaded " + loadedFiles.Count +
                  " day/contact dialogue file(s).", this);
    }

    private IEnumerator StartConversationAfterDelay(JsonMessageDialogueFile data, int currentDay)
    {
        // delaySeconds is the target arrival time of the first actual contact message.
        // On a brand-new thread, the system banner appears one message interval earlier,
        // so the first contact message still arrives at the requested timestamp.
        float delay = Mathf.Max(0f, data.delaySeconds);
        string contactId = "narrative_contact_" + NormalizeContactKey(data.contact);
        bool newThreadNeedsBanner = session.GetThread(contactId) == null &&
            !string.IsNullOrWhiteSpace(NormalizeSystemBanner(data.systemBanner));
        bool hasChatOpening = data.openingMessages != null &&
            data.openingMessages.Any(opening => opening != null &&
                !string.IsNullOrWhiteSpace(opening.text) && IsChatChannel(opening.channel));
        if (newThreadNeedsBanner && hasChatOpening)
            delay = Mathf.Max(0f, delay - messageIntervalSeconds);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // Do not deliver a delayed message if the player has already advanced to another day.
        if (GameManager.EnsureInstance().CurrentDay != currentDay)
            yield break;

        yield return StartCoroutine(StartConversation(data, currentDay));
    }

    private IEnumerator StartConversation(JsonMessageDialogueFile data, int currentDay)
    {
        session = MessagingSession.Get();
        string contactKey = NormalizeContactKey(data.contact);
        string contactId = "narrative_contact_" + contactKey;
        bool threadAlreadyExists = session.GetThread(contactId) != null;

        JsonMessageOpening[] openings = data.openingMessages ?? new JsonMessageOpening[0];
        int firstChatOpeningIndex = Array.FindIndex(openings, opening => opening != null &&
            !string.IsNullOrWhiteSpace(opening.text) && IsChatChannel(opening.channel));

        string firstChatText = firstChatOpeningIndex >= 0
            ? openings[firstChatOpeningIndex].text
            : "";
        string banner = NormalizeSystemBanner(data.systemBanner);
        bool showSystemBanner = !threadAlreadyExists && !string.IsNullOrWhiteSpace(banner);

        string firstDeliveredText = showSystemBanner
            ? banner
            : (!string.IsNullOrWhiteSpace(firstChatText) ? firstChatText : "...");

        string messageId = (string.IsNullOrWhiteSpace(data.conversationId)
            ? "Day" + currentDay + "_" + data.contact
            : data.conversationId) + "_opening";

        var openingMessage = new NarrativeMessageData
        {
            messageId = messageId,
            contactKey = contactKey,
            contactName = data.contact,
            portrait = FindPortrait(data.contact),
            text = firstDeliveredText,
            isSystem = showSystemBanner,
            replies = new MessageConversation.Reply[0],
            waitForReply = false
        };

        // The first banner is its own system-styled Entry. The contact's first real
        // message is delivered separately below, with the normal incoming-message sound.
        NarrativeMessageInbox inbox = NarrativeMessageInbox.Get();
        inbox.Deliver(openingMessage, currentDay);

        MessageConversation runtimeContact = inbox.Conversations.FirstOrDefault(c =>
            c != null && string.Equals(c.id, contactId, StringComparison.Ordinal));

        if (runtimeContact == null)
        {
            Debug.LogError("[JsonMessageDialogueController] Could not create contact '" + contactId + "'.", this);
            yield break;
        }

        var state = new RuntimeConversation
        {
            data = data,
            contactId = contactId,
            contact = runtimeContact
        };
        activeConversations[contactId] = state;

        GameManager game = GameManager.EnsureInstance();
        if (currentDay == 3)
        {
            if (debugBirthdayAlreadyKnown) game.SetStoryFlag("C02_Birthday", true);
            if (debugIntoxicationAlreadyKnown) game.SetStoryFlag("C06_Intoxicated", true);
        }

        state.currentNode = FindNode(state, data.startNode);
        if (state.currentNode == null)
        {
            Debug.LogError("[JsonMessageDialogueController] Start node '" + data.startNode +
                           "' was not found in " + data.conversationId + ".", this);
            yield break;
        }

        ApplyEffects(state, state.currentNode.onEnter);

        var linesToPlay = new List<JsonMessageLine>();

        // If the opening message itself was a real chat line, it was already delivered
        // through NarrativeMessageInbox; skip that one to avoid duplicating it.
        for (int i = 0; i < openings.Length; i++)
        {
            JsonMessageOpening opening = openings[i];
            if (opening == null || string.IsNullOrWhiteSpace(opening.text))
                continue;
            if (!showSystemBanner && i == firstChatOpeningIndex)
                continue;

            linesToPlay.Add(new JsonMessageLine
            {
                speaker = opening.speaker,
                text = opening.text,
                channel = opening.channel
            });
        }

        AddLines(linesToPlay, state.currentNode.lines);
        yield return StartCoroutine(PlayLines(state, linesToPlay));

        SetVisibleChoices(state, state.currentNode);
        session.SetAwaitingReply(contactId, state.visibleChoices.Count > 0);
        state.finished = state.visibleChoices.Count == 0;

        Debug.Log("[JsonMessageDialogueController] Started '" + data.conversationId +
                  "' for " + data.contact + " on Day " + currentDay + ".", this);
    }

    /// <summary>Called by MonitorMessagingApp instead of MessagingSession.Reply for JSON contacts.</summary>
    public bool TrySelectChoice(string conversationId, int choiceIndex)
    {
        if (!activeConversations.TryGetValue(conversationId, out RuntimeConversation state) ||
            state.finished || state.processing || choiceIndex < 0 ||
            choiceIndex >= state.visibleChoices.Count)
            return false;

        JsonMessageChoice choice = state.visibleChoices[choiceIndex];
        if (choice == null)
            return false;

        state.processing = true;
        state.visibleChoices.Clear();
        state.contact.replies = new MessageConversation.Reply[0];
        session.AppendPlayerMessage(conversationId, choice.text);
        session.SetAwaitingReply(conversationId, false);

        ApplyEffects(state, choice.effects);
        JsonMessageNode nextNode = FindNode(state, choice.nextNode);
        if (nextNode == null)
        {
            Debug.LogError("[JsonMessageDialogueController] Choice points to missing node '" +
                           choice.nextNode + "' in " + state.data.conversationId + ".", this);
            state.finished = true;
            state.processing = false;
            session.SetAwaitingReply(conversationId, false);
            return true;
        }

        state.currentNode = nextNode;
        ApplyEffects(state, state.currentNode.onEnter);
        StartCoroutine(ContinueAfterChoice(state));
        return true;
    }

    private IEnumerator ContinueAfterChoice(RuntimeConversation state)
    {
        yield return StartCoroutine(PlayLines(state, ToList(state.currentNode.lines)));
        SetVisibleChoices(state, state.currentNode);
        state.finished = state.visibleChoices.Count == 0;
        state.processing = false;
        session.SetAwaitingReply(state.contactId, state.visibleChoices.Count > 0);
    }

    private IEnumerator PlayLines(RuntimeConversation state, List<JsonMessageLine> lines)
    {
        if (lines == null)
            yield break;

        foreach (JsonMessageLine line in lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.text))
                continue;

            if (messageIntervalSeconds > 0f)
                yield return new WaitForSecondsRealtime(messageIntervalSeconds);

            string channel = (line.channel ?? "chat").Trim().ToLowerInvariant();
            if (channel == "internal")
            {
                DialogueManager manager = DialogueManager.Instance;
                if (manager == null)
                {
                    Debug.LogWarning("[JsonMessageDialogueController] Internal line skipped because DialogueManager.Instance is null: " + line.text, this);
                    continue;
                }

                var dialogueLine = new DialogueLine
                {
                    speakerName = string.IsNullOrWhiteSpace(line.speaker) ? "Inner Voice" : line.speaker,
                    personality = ParsePersonality(line.speaker),
                    text = line.text,
                    displayDuration = 3.5f
                };

                Task playback = manager.DisplayLineAndWaitAsync(dialogueLine);
                while (!playback.IsCompleted)
                    yield return null;

                if (playback.IsFaulted)
                    Debug.LogException(playback.Exception, this);
            }
            else if (channel == "system")
            {
                session.ReceiveSystem(state.contactId, StripSystemPrefix(line.text));
            }
            else
            {
                // MessagingSession plays the global Message SFX here. AudioDirector is
                // persistent and is not tied to which monitor is currently selected.
                session.Receive(state.contactId, line.text);
            }
        }
    }

    private void SetVisibleChoices(RuntimeConversation state, JsonMessageNode node)
    {
        state.visibleChoices.Clear();
        if (node != null && node.choices != null)
        {
            foreach (JsonMessageChoice choice in node.choices)
            {
                if (choice != null && EvaluateConditions(choice.conditions))
                    state.visibleChoices.Add(choice);
            }
        }

        state.contact.replies = state.visibleChoices.Select(choice => new MessageConversation.Reply
        {
            text = choice.text,
            response = "",
            connectednessDelta = 0,
            suspicionDelta = 0
        }).ToArray();
    }

    private bool EvaluateConditions(JsonMessageCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        GameManager game = GameManager.EnsureInstance();
        foreach (JsonMessageCondition condition in conditions)
        {
            if (condition == null) continue;
            bool passed;
            switch ((condition.type ?? "").Trim().ToLowerInvariant())
            {
                case "flag":
                    passed = game.HasStoryFlag(condition.key) == condition.boolValue;
                    break;
                default:
                    Debug.LogWarning("[JsonMessageDialogueController] Unsupported condition type '" +
                                     condition.type + "'. Choice hidden.", this);
                    passed = false;
                    break;
            }
            if (!passed) return false;
        }
        return true;
    }

    private void ApplyEffects(RuntimeConversation state, JsonMessageEffect[] effects)
    {
        if (effects == null) return;
        GameManager game = GameManager.EnsureInstance();
        foreach (JsonMessageEffect effect in effects)
        {
            if (effect == null || string.IsNullOrWhiteSpace(effect.type)) continue;
            switch (effect.type.Trim().ToLowerInvariant())
            {
                case "setflag":
                    game.SetStoryFlag(effect.key, effect.boolValue);
                    break;
                case "addstat":
                    game.AddStoryStat(effect.key, effect.intValue);
                    break;
                default:
                    Debug.LogWarning("[JsonMessageDialogueController] Unsupported effect type '" + effect.type + "'.", this);
                    break;
            }
        }
    }

    private static JsonMessageNode FindNode(RuntimeConversation state, string nodeId)
    {
        if (state == null || state.data == null || state.data.nodes == null || string.IsNullOrWhiteSpace(nodeId))
            return null;
        return state.data.nodes.FirstOrDefault(node => node != null &&
            string.Equals(node.id, nodeId, StringComparison.Ordinal));
    }

    private static List<JsonMessageLine> ToList(JsonMessageLine[] lines)
    {
        return lines == null ? new List<JsonMessageLine>() : lines.Where(line => line != null).ToList();
    }

    private static void AddLines(List<JsonMessageLine> destination, JsonMessageLine[] lines)
    {
        if (lines == null) return;
        destination.AddRange(lines.Where(line => line != null));
    }

    private Sprite FindPortrait(string contact)
    {
        if (contactPortraits == null) return null;
        JsonMessageContactPortrait match = contactPortraits.FirstOrDefault(entry => entry != null &&
            string.Equals(entry.contact, contact, StringComparison.OrdinalIgnoreCase));
        return match != null ? match.portrait : null;
    }

    private static bool IsChatChannel(string channel)
    {
        string value = (channel ?? "chat").Trim().ToLowerInvariant();
        return value != "internal" && value != "system";
    }

    private static string NormalizeContactKey(string contact)
    {
        return (contact ?? "contact").Trim().ToLowerInvariant().Replace(" ", "_");
    }

    private static string NormalizeSystemBanner(string banner)
    {
        return StripSystemPrefix(banner);
    }

    private static string StripSystemPrefix(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        string value = text.Trim();
        if (value.StartsWith("System:", StringComparison.OrdinalIgnoreCase))
            value = value.Substring("System:".Length).Trim();
        return value;
    }

    private static HeadPersonality ParsePersonality(string speaker)
    {
        string key = new string((speaker ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        switch (key)
        {
            case "grandarchitect": return HeadPersonality.GrandArchitect;
            case "pragmatist": return HeadPersonality.Pragmatist;
            case "detective": return HeadPersonality.Detective;
            case "sentinel": return HeadPersonality.Sentinel;
            case "genius": return HeadPersonality.Genius;
            default: return HeadPersonality.InternalMonologue;
        }
    }
}
