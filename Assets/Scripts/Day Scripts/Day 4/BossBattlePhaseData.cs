using System;
using System.Collections.Generic;
using UnityEngine;

public enum RhythmLane { LaneD = 0, LaneF = 1, LaneJ = 2, LaneK = 3 }

[Serializable]
public struct NoteData
{
    public float hitTime;        // Exact second in the music when note reaches the hit line
    public RhythmLane lane;     // Which key (D, F, J, K)
}

[Serializable]
public struct OOPQuestion
{
    public string questionText;
    public string[] options;
    public int correctAnswerIndex;
    public string explanationText;
}

[CreateAssetMenu(fileName = "NewPhaseData", menuName = "RefactoFight/Phase Data")]
public class BossBattlePhaseData : ScriptableObject
{
    public int phaseNumber;
    public string phaseTitle;
    public float bpm = 120f;
    public List<NoteData> notes = new List<NoteData>();
    public OOPQuestion intermissionQuestion;

    // This creates a button in the Inspector's 3-dot context menu
    [ContextMenu("Regenerate Rhythm Notes")]
    private void RegenerateNotes()
    {
        // Clear any existing data
        if (notes == null) notes = new List<NoteData>();
        notes.Clear();

        // Match original generator note counts and speeds based on phase number
        int noteCount = 40;
        if (phaseNumber == 2) noteCount = 60;
        else if (phaseNumber >= 3) noteCount = 80;

        float effectiveBpm = bpm > 0 ? bpm : 120f;
        float beatInterval = 60f / effectiveBpm;
        float currentTime = 2.0f; // Give the player 2 seconds before notes start

        for (int i = 0; i < noteCount; i++)
        {
            RhythmLane targetLane = (RhythmLane)UnityEngine.Random.Range(0, 4);
            notes.Add(new NoteData { hitTime = currentTime, lane = targetLane });

            bool isLastNote = (i == noteCount - 1);

            // Phase 4 Note Layering (Triple, Quadruple chords)
            if (phaseNumber == 4)
            {
                if (isLastNote)
                {
                    // Force a massive quadruple chord on the very last note
                    for (int l = 0; l < 4; l++)
                    {
                        if ((RhythmLane)l != targetLane)
                            notes.Add(new NoteData { hitTime = currentTime, lane = (RhythmLane)l });
                    }
                }
                else if (i % 16 == 0 && i != 0)
                {
                    // Occasional quadruple note
                    for (int l = 0; l < 4; l++)
                    {
                        if ((RhythmLane)l != targetLane)
                            notes.Add(new NoteData { hitTime = currentTime, lane = (RhythmLane)l });
                    }
                }
                else if (i % 8 == 0 && i != 0)
                {
                    // Triple note (one lane randomly left empty)
                    int missingLane = UnityEngine.Random.Range(0, 4);
                    for (int l = 0; l < 4; l++)
                    {
                        if ((RhythmLane)l != targetLane && l != missingLane)
                            notes.Add(new NoteData { hitTime = currentTime, lane = (RhythmLane)l });
                    }
                }
            }
            // Phase 2 and 3 Note Layering (Double notes)
            else if (phaseNumber >= 2 && i % 8 == 0)
            {
                RhythmLane doubleLane = (RhythmLane)(((int)targetLane + 2) % 4);
                notes.Add(new NoteData { hitTime = currentTime, lane = doubleLane });
            }

            // Rhythm variation (eighth notes on higher phases)
            float step = (phaseNumber >= 3 && i % 4 == 0) ? beatInterval / 2f : beatInterval;
            currentTime += step;
        }

        // Force Unity to save the newly generated data directly to the hard drive
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        #endif

        Debug.Log($"[RefactoFight] Safely generated {notes.Count} notes at {effectiveBpm} BPM for Phase {phaseNumber}!");
    }
}