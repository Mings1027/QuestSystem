using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Requirement/Level Requirement")]
public class CastleLevelRequirementDataSO : QuestRequirementDataSO
{
    [Serializable]
    public class LevelReqInfo
    {
        [HideInInspector] public string questId;
        
        public int questIndex;

        [Tooltip("목표 레벨")] public int targetLevel;

        public LevelReqInfo(string id, int index, int level)
        {
            questId = id;
            questIndex = index;
            targetLevel = level;
        }
    }

    public List<LevelReqInfo> questSpecificDatas = new();

    public override Type GetRequirementType() => typeof(CastleLevelRequirement);

    public int GetTargetLevel(string id)
    {
        var info = questSpecificDatas.Find(x => x.questId == id);
        return info != null ? info.targetLevel : 1; // 기본값
    }

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null) info.questIndex = questIndex;
        else questSpecificDatas.Add(new LevelReqInfo(questId, questIndex, 1));
        
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
    }
}