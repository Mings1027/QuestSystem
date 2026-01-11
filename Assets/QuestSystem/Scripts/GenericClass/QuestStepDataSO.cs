using System.Collections.Generic;
using UnityEngine;

public abstract class QuestStepDataSO<T> : QuestStepDataSO where T : QuestSpecificData, new()
{
    [SerializeField] private List<T> questSpecificDatas = new();

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
    }

    public override void CleanUpUnusedData(HashSet<string> validQuestIds)
    {
        questSpecificDatas.RemoveAll(x => !validQuestIds.Contains(x.questId));
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
    }

    protected T GetEntry(string id) => questSpecificDatas.Find(x => x.questId == id);
}