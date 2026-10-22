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

[CreateAssetMenu(fileName = "BossBattlePhase", menuName = "NullProgrammer/BossBattlePhase")]
public class BossBattlePhaseData : ScriptableObject
{
    public int phaseNumber;
    public string phaseTitle;
    public AudioClip phaseMusic;
    public float bpm = 120f;
    public List<NoteData> notes = new List<NoteData>();
    public OOPQuestion intermissionQuestion;
}