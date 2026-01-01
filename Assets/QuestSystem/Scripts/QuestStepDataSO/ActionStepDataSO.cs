using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Step_Action_Shared", menuName = "Quest/Step Data/Simple Action")]
public class ActionStepDataSO : QuestStepDataSO<ActionStepInfo>, ICounterStepData
{
    public string GetCategory() => QuestCategory.Action;
    public override Type GetQuestStepType() => typeof(CounterQuestStep);
    public string GetTargetId(string id) => GetEntry(id)?.actionId;
    public int GetTargetValue(string id) => GetEntry(id)?.requiredCount ?? 1;
    public QuestUpdateType GetUpdateType() => QuestUpdateType.Add;
}