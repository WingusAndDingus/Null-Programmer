using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NullProgrammer.Day3Workshop.Editor
{
    public static class WorkshopTools
    {
        private const string DayPath = "Assets/Scripts/Day3BoardWorkshop/Day3_BoardWorkshop.asset";
        private const string PrefabPath = "Assets/Resources/Day3BoardWorkshop.prefab";

        [MenuItem("Tools/Null Programmer/Day 3 Board Workshop/1 - Register Day 3 and Create Prefab")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before registering assets.");
            var day = AssetDatabase.LoadAssetAtPath<DaySequenceData>(DayPath);
            if (day == null) throw new InvalidOperationException("Day 3 narrative asset is missing.");
            ValidateDay(day);
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                var go = new GameObject("Day3BoardWorkshop", typeof(WorkshopWindow));
                try { PrefabUtility.SaveAsPrefabAsset(go, PrefabPath); }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            string[] prefabs = { "Assets/Prefabs/NarrativeManager.prefab", "Assets/Prefabs/Day5_Prefabs/NarrativeManager.prefab" };
            foreach (string path in prefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var executor in root.GetComponentsInChildren<DayNarrativeExecutor>(true)) Assign(executor, day);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var executor in UnityEngine.Object.FindObjectsByType<DayNarrativeExecutor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!EditorUtility.IsPersistent(executor))
                {
                    Assign(executor, day);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(executor);
                    EditorSceneManager.MarkSceneDirty(executor.gameObject.scene);
                }
            AssetDatabase.SaveAssets();
            Selection.activeObject = day;
            Debug.Log("Registered Wednesday at index 2 (Day 3). Other day slots preserved. Save any open scene marked dirty. The prefab is " + PrefabPath);
        }
        private static void Assign(DayNarrativeExecutor executor, DaySequenceData day)
        {
            var serialized = new SerializedObject(executor);
            var list = serialized.FindProperty("daySequences");
            if (list.arraySize < 3) list.arraySize = 3;
            list.GetArrayElementAtIndex(2).objectReferenceValue = day;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("Tools/Null Programmer/Day 3 Board Workshop/2 - Start Wednesday in Desktop Play Mode")]
        public static void StartWednesday()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Open DesktopUI and enter Play Mode first, then run this command.");
            var day = AssetDatabase.LoadAssetAtPath<DaySequenceData>(DayPath);
            var executor = UnityEngine.Object.FindAnyObjectByType<DayNarrativeExecutor>();
            if (executor == null) throw new InvalidOperationException("Open DesktopUI with its NarrativeManager first.");
            var old = GameManager.EnsureInstance().GetComponent<WorkshopSession>();
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var gm = GameManager.EnsureInstance(); gm.setCurrentDay(3); gm.setStep("Start");
            if (DialogueManager.Instance != null) DialogueManager.Instance.HideDialogueUI();
            Assign(executor,day);
            executor.LoadAndExecuteDay(3);
            // Re-evaluate the existing inbox's labels and badge for Wednesday.
            var inbox = UnityEngine.Object.FindAnyObjectByType<IntegratedMailInbox>();
            if (inbox != null) inbox.RefreshForCurrentDay();
            Debug.Log("Wednesday started. Read Steve's email, close the inbox, and accept the call. No Play Mode changes are saved to scenes.");
        }
        [MenuItem("Tools/Null Programmer/Day 3 Board Workshop/3 - Validate Lesson")]
        public static void Validate()
        {
            ValidateDay(AssetDatabase.LoadAssetAtPath<DaySequenceData>(DayPath));
            for (int stage=0; stage<4; stage++)
                for (int choice=0; choice<3; choice++)
                {
                    int[] patches={1,1,1,1}; patches[stage]=choice;
                    bool passed=BoardSimulation.Passed(BoardSimulation.CheckPatch(stage,patches));
                    if (passed != (choice==1)) throw new InvalidOperationException("Unexpected grading for stage "+stage+", patch "+choice);
                }
            if(!BoardSimulation.Passed(BoardSimulation.CheckPatch(4,new[]{1,1,1,1}))) throw new InvalidOperationException("Repaired build failed.");
            Debug.Log("Wednesday validation passed: four distinct repairs, wrong-patch regressions, final build, and narrative links.");
        }
        private static void ValidateDay(DaySequenceData day)
        {
            if(day==null || day.dayNumber!=3) throw new InvalidOperationException("Expected Day 3 asset.");
            var ids=new HashSet<string>();
            foreach(var step in day.steps) if(string.IsNullOrWhiteSpace(step.stepID)||!ids.Add(step.stepID)) throw new InvalidOperationException("Duplicate/empty step ID.");
            foreach(var step in day.steps)
            {
                if(!string.IsNullOrEmpty(step.nextStepID)&&!ids.Contains(step.nextStepID)) throw new InvalidOperationException("Missing target "+step.nextStepID);
                foreach(var c in step.choices) if(!ids.Contains(c.targetStepOnClick)) throw new InvalidOperationException("Missing choice target.");
            }
        }
    }
}
