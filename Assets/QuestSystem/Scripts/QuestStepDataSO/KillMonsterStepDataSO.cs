using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Step_Monster_Shared", menuName = "Quest/Step Data/Kill Monster")]
public class KillMonsterStepDataSO : QuestStepDataSO<MonsterStepInfo>, ICounterStepData
{
    public string GetCategory() => QuestCategory.Monster;
    public override Type GetQuestStepType() => typeof(CounterQuestStep);
    public string GetTargetId(string id) => GetEntry(id)?.monsterID;
    public int GetTargetValue(string id) => GetEntry(id)?.killCount ?? 1;
    public QuestUpdateType GetUpdateType() => QuestUpdateType.Add;
}