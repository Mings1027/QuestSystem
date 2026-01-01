// QuestRequirementDataSO.cs (제네릭 버전)

using System.Collections.Generic;
using UnityEditor;

public abstract class QuestRequirementDataSO<T> : QuestRequirementDataSO where T : QuestSpecificData, new()
{
    public List<T> questSpecificDatas = new();

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null) info.questIndex = questIndex;
        else
        {
            T newInfo = new T();
            newInfo.Init(questId, questIndex);
            questSpecificDatas.Add(newInfo);
        }

        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
        EditorUtility.SetDirty(this);
    }

    public void CleanUpUnusedData(HashSet<string> validQuestIds)
    {
        questSpecificDatas.RemoveAll(x => !validQuestIds.Contains(x.questId));
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
        EditorUtility.SetDirty(this);
    }
}

public abstract class QuestStepDataSO<T> : QuestStepDataSO where T : QuestSpecificData, new()
{
    public List<T> questSpecificDatas = new();

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null)
        {
            info.questIndex = questIndex;
        }
        else
        {
            T newInfo = new T();
            newInfo.Init(questId, questIndex);
            questSpecificDatas.Add(newInfo);
        }

        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
        EditorUtility.SetDirty(this);
    }

    public void CleanUpUnusedData(HashSet<string> validQuestIds)
    {
        questSpecificDatas.RemoveAll(x => !validQuestIds.Contains(x.questId));
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
        EditorUtility.SetDirty(this);
    }

    protected T GetEntry(string id) => questSpecificDatas.Find(x => x.questId == id);
}

// QuestRewardDataSO.cs (제네릭 버전)
public abstract class QuestRewardDataSO<T> : QuestRewardDataSO where T : QuestSpecificData, new()
{
    public List<T> questSpecificDatas = new();

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null) info.questIndex = questIndex;
        else
        {
            T newInfo = new T();
            newInfo.Init(questId, questIndex);
            questSpecificDatas.Add(newInfo);
        }

        // [핵심] 물리적 정렬
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
        EditorUtility.SetDirty(this);
    }

    public void CleanUpUnusedData(HashSet<string> validQuestIds)
    {
        questSpecificDatas.RemoveAll(x => !validQuestIds.Contains(x.questId));
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
        EditorUtility.SetDirty(this);
    }
}