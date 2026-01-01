using System;
using UnityEngine;

[Serializable]
public abstract class QuestSpecificData
{
    [HideInInspector] public string questId; // 데이터 식별용 (변경불가)
    public int questIndex;

    public virtual void Init(string id, int index)
    {
        questId = id;
        questIndex = index;
    }
}

[Serializable]
public class MonsterStepInfo : QuestSpecificData
{
    public string monsterID; // 퀘스트마다 다르게 설정 가능
    public int killCount = 1;
}

[Serializable]
public class StatStepInfo : QuestSpecificData
{
    public StatType targetStat; // 퀘스트마다 다르게 설정 가능
    public int targetValue = 10;
}

[Serializable]
public class ActionStepInfo : QuestSpecificData
{
    public string actionId; // 버튼 스크립트의 actionId와 일치해야 함
    public int requiredCount = 1;
}

[Serializable]
public class LevelReqInfo : QuestSpecificData
{
    [Tooltip("목표 레벨")] public int targetLevel = 1;
}

[Serializable]
public class GoldRewardInfo : QuestSpecificData
{
    [Tooltip("지급할 골드 양")] public int goldAmount = 100;
}