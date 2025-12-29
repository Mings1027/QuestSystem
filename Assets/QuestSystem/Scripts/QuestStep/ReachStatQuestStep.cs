using UnityEngine;

public class ReachStatQuestStep : QuestStep
{
    // 디버그용으로 SerializeField 유지 (런타임에 인스펙터에서 확인 가능)
    [Header("설정 (디버그용)")]
    [SerializeField] private StatType targetStat; 
    [SerializeField] private int targetValue;     

    private int currentValue = 0;

    // [핵심] 부모가 넘겨준 데이터를 받아 세팅
    protected override void InitData(QuestStepDataSO data)
    {
        // 형변환
        var myData = data as ReachStatStepDataSO;

        if (myData != null)
        {
            // 1. 공통 데이터 (어떤 스탯인지)
            this.targetStat = myData.targetStat;
            
            // 2. [핵심] 퀘스트별 고유 데이터 (목표 수치)
            // 부모 클래스(QuestStep)에 저장된 questId를 사용해 조회
            this.targetValue = myData.GetTargetValueForQuest(this.questId);
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
        }
        else
        {
            // 저장된 상태가 없으면 현재 스탯 값으로 초기화할 수도 있고, 0으로 할 수도 있음
            // 스탯 퀘스트는 보통 '현재 스탯'을 바로 반영해야 하므로 0보다는
            // DBPlayerGameData에서 가져오는 게 정확할 수 있으나,
            // 여기서는 단순하게 유지합니다.
            currentValue = 0; 
        }
        UpdateState();
    }
}