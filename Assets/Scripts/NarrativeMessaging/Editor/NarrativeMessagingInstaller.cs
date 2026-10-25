#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class NarrativeMessagingInstaller
{
    private const string Menu = "Tools/Null Programmer/Narrative Messaging/";

    [MenuItem(Menu + "1 - Enable Day Data Messages")]
    public static void Enable()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        string dataPath = SourcePath("DaySequenceData");
        string executorPath = SourcePath("DayNarrativeExecutor");
        string monitorPath = SourcePath("MonitorMessagingApp");
        string data = File.ReadAllText(dataPath);
        string executor = File.ReadAllText(executorPath);
        string monitor = File.ReadAllText(monitorPath);

        if (!Regex.IsMatch(data, @"\bNarrativeMessageData\s+message\b"))
            data = InsertAfterOpening(data, @"\bclass\s+NarrativeStep\b[^\{]*\{",
                "\n    [Header(\"Receive Message\")]\n    public NarrativeMessageData message = new NarrativeMessageData();\n", "NarrativeStep");

        var enumMatch = Regex.Match(data, @"\benum\s+NarrativeStepType\b[^\{]*\{(?<body>[^}]*)\}");
        if (!enumMatch.Success) throw new InvalidOperationException("Could not locate NarrativeStepType; no files changed.");
        if (!Regex.IsMatch(enumMatch.Groups["body"].Value, @"\bReceiveMessage\b"))
        {
            if (Regex.IsMatch(enumMatch.Groups["body"].Value, @"=\s*1000\b"))
                throw new InvalidOperationException("Enum value 1000 is already used; no files changed.");
            // Put a comma on its own line so a trailing comment cannot swallow it.
            string body = enumMatch.Groups["body"].Value;
            string tokens = Regex.Replace(body, @"//[^\r\n]*|/\*[\s\S]*?\*/", "").TrimEnd();
            string suffix = tokens.EndsWith(",", StringComparison.Ordinal) ? "" : ",";
            int end = enumMatch.Groups["body"].Index + body.Length;
            data = data.Insert(end, "\n" + suffix + "\n    ReceiveMessage = 1000\n");
        }
        if (!data.Contains("NarrativeMessageIds.Ensure(this)"))
        {
            var classMatch = Regex.Match(data, @"\bclass\s+DaySequenceData\b[^\{]*\{");
            if (!classMatch.Success) throw new InvalidOperationException("DaySequenceData class missing; no files changed.");
            var validate = Regex.Match(data.Substring(classMatch.Index), @"\bvoid\s+OnValidate\s*\(\s*\)\s*\{");
            if (validate.Success)
                data = data.Insert(classMatch.Index + validate.Index + validate.Length, "\n        NarrativeMessageIds.Ensure(this);\n");
            else
            {
                if (Regex.IsMatch(data.Substring(classMatch.Index), @"\bOnValidate\s*\("))
                    throw new InvalidOperationException("Existing OnValidate needs manual integration; no files changed.");
                data = data.Insert(classMatch.Index + classMatch.Length,
                    "\n    private void OnValidate() { NarrativeMessageIds.Ensure(this); }\n");
            }
        }
        if (!executor.Contains("case NarrativeStepType.ReceiveMessage:"))
            executor = InsertAfterOpening(executor, @"switch\s*\(\s*stepData\.stepType\s*\)\s*\{",
                "\n            case NarrativeStepType.ReceiveMessage:\n                await NarrativeMessageDelivery.RunAsync(stepData.message, currentDayData.dayNumber, cancellationToken);\n                break;\n", "narrative executor switch");

        if (!monitor.Contains("NarrativeMessageDelivery.GetConversations(conversations)"))
        {
            string old = "conversations ?? new MessageConversation[0]";
            if (!monitor.Contains(old)) throw new InvalidOperationException("Monitor controller has a different conversation loop; no files changed.");
            monitor = monitor.Replace(old, "NarrativeMessageDelivery.GetConversations(conversations)");
        }

        string backup = Path.Combine("Library", "NarrativeMessagingBackups", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(backup);
        var paths = new[] { dataPath, executorPath, monitorPath };
        var contents = new[] { data, executor, monitor };
        for (int i = 0; i < paths.Length; i++) File.Copy(paths[i], Path.Combine(backup, Path.GetFileName(paths[i])), true);
        for (int i = 0; i < paths.Length; i++) File.WriteAllText(paths[i], contents[i]);
        AssetDatabase.Refresh();
        Debug.Log("Day Data messaging enabled. Wait for compilation, then select your Day 3 asset and run step 2. Source backups: " + backup);
    }

    private static string InsertAfterOpening(string source, string pattern, string addition, string label)
    {
        var match = Regex.Match(source, pattern);
        if (!match.Success) throw new InvalidOperationException("Could not find " + label + "; no files changed.");
        return source.Insert(match.Index + match.Length, addition);
    }

    private static string SourcePath(string name)
    {
        var paths = AssetDatabase.FindAssets(name + " t:MonoScript")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileNameWithoutExtension(path) == name).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("Expected exactly one " + name + ".cs, found " + paths.Length + ". No files changed.");
        return paths[0];
    }

    [MenuItem(Menu + "2 - Add Friend Example To Selected Day 3")]
    public static void AddExample()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        var day = Selection.activeObject as DaySequenceData;
        if (day == null || day.dayNumber != 3) throw new InvalidOperationException("Select your Day 3 DaySequenceData asset in the Project panel first.");
        var so = new SerializedObject(day);
        var steps = so.FindProperty("steps");
        int startIndex = -1;
        for (int i = 0; i < steps.arraySize; i++)
        {
            var step = steps.GetArrayElementAtIndex(i);
            string id = step.FindPropertyRelative("stepID").stringValue;
            if (id == "Day3_FriendMessage") { Debug.Log("Friend example is already present.", day); return; }
            if (string.Equals(id, "Start", StringComparison.OrdinalIgnoreCase)) startIndex = i;
        }
        if (startIndex < 0) throw new InvalidOperationException("Day 3 needs a Start step; asset unchanged.");
        var probe = steps.GetArrayElementAtIndex(startIndex).FindPropertyRelative("message");
        if (probe == null || !Enum.IsDefined(typeof(NarrativeStepType), "ReceiveMessage"))
            throw new InvalidOperationException("Run step 1 and wait for compilation first.");
        var start = steps.GetArrayElementAtIndex(startIndex);
        if (start.FindPropertyRelative("choices").arraySize != 0)
            throw new InvalidOperationException("Start uses branching choices. Insert a ReceiveMessage step manually so its branches are preserved.");
        string next = start.FindPropertyRelative("nextStepID").stringValue;
        Undo.RecordObject(day, "Add Day 3 Friend Message");
        steps.InsertArrayElementAtIndex(startIndex + 1);
        // Insertion duplicates an entry in Unity; reset the example's ordinary narrative fields.
        var added = steps.GetArrayElementAtIndex(startIndex + 1);
        Set(added, "stepID", "Day3_FriendMessage");
        Set(added, "speakerName", "");
        Set(added, "lineText", "");
        Set(added, "awardSkillKey", "");
        Set(added, "nextStepID", next);
        added.FindPropertyRelative("personality").intValue = 0;
        added.FindPropertyRelative("choices").ClearArray();
        added.FindPropertyRelative("stepType").intValue = Convert.ToInt32(Enum.Parse(typeof(NarrativeStepType), "ReceiveMessage"));
        added.FindPropertyRelative("suspicionChange").intValue = 0;
        added.FindPropertyRelative("exerciseIndex").intValue = 0;
        added.FindPropertyRelative("delayDuration").floatValue = 0;
        var message = added.FindPropertyRelative("message");
        Set(message, "messageId", Guid.NewGuid().ToString("N"));
        Set(message, "contactKey", "friend");
        Set(message, "contactName", "Friend");
        Set(message, "text", "Hey! How's the game coming along? You've been pretty quiet lately. Want to catch up this weekend?");
        message.FindPropertyRelative("waitForReply").boolValue = false;
        var portraitPath = AssetDatabase.FindAssets("FriendAvatar t:Sprite").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        message.FindPropertyRelative("portrait").objectReferenceValue = string.IsNullOrEmpty(portraitPath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
        var replies = message.FindPropertyRelative("replies");
        replies.arraySize = 2;
        FillReply(replies.GetArrayElementAtIndex(0), "Yeah, that would be nice. It's been a stressful week.", "Of course. Let me know when you're free!", 5);
        FillReply(replies.GetArrayElementAtIndex(1), "Pretty busy right now. Maybe later.", "No worries. Just checking in!", 0);
        steps.GetArrayElementAtIndex(startIndex).FindPropertyRelative("nextStepID").stringValue = "Day3_FriendMessage";
        so.ApplyModifiedProperties();
        NarrativeMessageIds.Ensure(day);
        EditorUtility.SetDirty(day);
        AssetDatabase.SaveAssets();
        Debug.Log("Friend message added after Day 3's opening Start step, before its original next step. Edit the inline Message fields in this day asset.", day);
    }

    private static void Set(SerializedProperty owner, string field, string text)
    {
        owner.FindPropertyRelative(field).stringValue = text;
    }
    private static void FillReply(SerializedProperty reply, string text, string response, int connectedness)
    {
        Set(reply, "text", text);
        Set(reply, "response", response);
        reply.FindPropertyRelative("connectednessDelta").intValue = connectedness;
        reply.FindPropertyRelative("suspicionDelta").intValue = 0;
    }
}
#endif
