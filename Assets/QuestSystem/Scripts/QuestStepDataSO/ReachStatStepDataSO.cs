using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "Step_Stat_Shared", menuName = "Quest/Step Data/Reach Stat (Shared)")]
public class ReachStatStepDataSO : QuestStepDataSO
{
    [Serializable]
    public class StatStepInfo
    {
        [HideInInspector] public string questId;
        
        public int questIndex;
        
        [Tooltip("목표 스탯 수치")]
        public int targetValue;

        public StatStepInfo(string id, int index, int value)
        {
            questId = id;
            questIndex = index;
            targetValue = value;
        }
    }

    public StatType targetStat;

    [Header("퀘스트별 목표 설정")]
    public List<StatStepInfo> questSpecificDatas = new();

    public override Type GetQuestStepType() => typeof(ReachStatQuestStep);

    public int GetTargetValueForQuest(string questId)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null) return info.targetValue;
        
        return 10; // 기본값
    }

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);

        if (info != null)
        {
            info.questIndex = questIndex;
        }
        else
        {
            questSpecificDatas.Add(new StatStepInfo(questId, questIndex, 10));
        }

        // [핵심] 정렬
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
    }
}
