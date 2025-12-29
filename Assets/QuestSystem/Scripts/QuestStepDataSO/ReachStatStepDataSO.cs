using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "Step_Stat_Shared", menuName = "Quest/Step Data/Reach Stat (Shared)")]
public class ReachStatStepDataSO : QuestStepDataSO
{
    // [변경점 1] 통합 데이터 클래스
    [System.Serializable]
    public class StatStepInfo
    {
        [HideInInspector] public string questId;
        
        [Tooltip("퀘스트 순서 (자동 갱신됨)")]
        public int displayIndex;
        
        [Tooltip("목표 스탯 수치")]
        public int targetValue;

        public StatStepInfo(string id, int index, int value)
        {
            questId = id;
            displayIndex = index;
            targetValue = value;
        }
    }

    [Header("공통 설정")]
    public StatType targetStat;

    [Header("퀘스트별 목표 설정")]
    // [변경점 2] 통합 리스트
    public List<StatStepInfo> stepInfos = new List<StatStepInfo>();

    public override Type GetQuestStepType() => typeof(ReachStatQuestStep);

    public int GetTargetValueForQuest(string questId)
    {
        var info = stepInfos.Find(x => x.questId == questId);
        if (info != null) return info.targetValue;
        
        return 10; // 기본값
    }

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = stepInfos.Find(x => x.questId == questId);

        if (info != null)
        {
            info.displayIndex = questIndex;
        }
        else
        {
            stepInfos.Add(new StatStepInfo(questId, questIndex, 10));
        }

        // [핵심] 정렬
        stepInfos.Sort((a, b) => a.displayIndex.CompareTo(b.displayIndex));
    }
}