using UnityEngine;

public class ReachStatQuestStep : QuestStep
{
    // 인스펙터 변수 삭제 (이제 데이터 SO에서 받음)
    private StatType targetStat; 
    private int targetValue;     

    private int currentValue = 0;

    // [핵심] 부모가 넘겨준 데이터를 받아서 세팅
    protected override void InitData(QuestStepDataSO data)
    {
        // 형변환 (Casting)
        var myData = data as ReachStatStepDataSO;

        if (myData != null)
        {
            this.targetStat = myData.targetStat;
            this.targetValue = myData.targetValue;
        }
        else
        {
            Debug.LogError("데이터 타입이 맞지 않습니다! ReachStatStepDataSO가 필요합니다.");
        }
        
        // 데이터 세팅 후 초기 상태 업데이트
        UpdateState();
    }

    private void OnEnable()
    {
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.onStatChanged += OnStatChanged;
    }

    private void OnDisable()
    {
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.onStatChanged -= OnStatChanged;
    }

    private void OnStatChanged(StatType type, int newValue)
    {
        // 받아온 데이터와 비교
        if (type != targetStat) return;

        currentValue = newValue;
        UpdateState();

        if (currentValue >= targetValue)
        {
            FinishQuestStep();
        }
    }

    private void UpdateState()
    {
        string state = currentValue.ToString();
        string status = $"{targetStat} 달성: {currentValue} / {targetValue}";
        ChangeState(state, status);
    }

    protected override void SetQuestStepState(string state)
    {
        if (!string.IsNullOrEmpty(state))
        {
            int.TryParse(state, out currentValue);
            UpdateState();
        }
    }
}