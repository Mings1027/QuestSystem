using UnityEngine;
using System;

public abstract class QuestRequirementDataSO : ScriptableObject
{
    [Tooltip("에디터 표시 이름")]
    public string label;

    public abstract Type GetRequirementType();
    
    public abstract void SyncQuestData(string questId, int questIndex);
    
    public abstract void CleanUpUnusedData(System.Collections.Generic.HashSet<string> validQuestIds);
}