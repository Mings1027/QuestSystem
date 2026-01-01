using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Step_Stat_Shared", menuName = "Quest/Step Data/Reach Stat")]
public class ReachStatStepDataSO : QuestStepDataSO<StatStepInfo>, ICounterStepData
{
    public string GetCategory() => QuestCategory.Stat;
    public override Type GetQuestStepType() => typeof(CounterQuestStep);
    public string GetTargetId(string id) => GetEntry(id)?.targetStat.ToString();
    public int GetTargetValue(string id) => GetEntry(id)?.targetValue ?? 10;
    public QuestUpdateType GetUpdateType() => QuestUpdateType.Replace;
}