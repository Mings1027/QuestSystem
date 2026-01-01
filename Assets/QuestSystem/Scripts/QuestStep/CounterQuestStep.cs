public class CounterQuestStep : QuestStep 
{
    protected ICounterStepData counterData;
    protected string targetId;
    protected int targetValue;
    protected int currentValue;

    protected override void InitData(QuestStepDataSO data)
    {
        counterData = data as ICounterStepData;
        if (counterData != null)
        {
            // 내 퀘스트 ID에 할당된 타겟 정보를 가져옴
            targetId = counterData.GetTargetId(questId); 
            targetValue = counterData.GetTargetValue(questId);
        }
    }

    private void OnEnable() => QuestEventManager.Instance.onGenericEvent += OnStepUpdate;
    private void OnDisable() => QuestEventManager.Instance.onGenericEvent -= OnStepUpdate;

    private void OnStepUpdate(string category, string id, int value)
    {
        // 1. 카테고리와 타겟 ID가 일치하는 신호만 처리
        if (category != counterData.GetCategory() || id != targetId) return;

        // 2. 데이터가 정의한 Add/Replace에 따라 수치 업데이트
        if (counterData.GetUpdateType() == QuestUpdateType.Replace)
            currentValue = value;
        else
            currentValue += value;

        UpdateState();
        if (currentValue >= targetValue) FinishQuestStep();
    }

    protected override void SetQuestStepState(string state)
    {
        int.TryParse(state, out currentValue);
        UpdateState();
    }

    private void UpdateState() => ChangeState(currentValue.ToString(), $"{targetId}: {currentValue} / {targetValue}");
}