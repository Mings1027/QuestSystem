using UnityEngine;

public class KillMonsterQuestStep : QuestStep
{
    [Header("설정")]
    [SerializeField] private string targetMonsterId = "Slime"; // 잡아야 할 몬스터 ID
    [SerializeField] private int targetKillCount = 10;         // 목표 처치 수

    private int currentKillCount;        // 현재 처치 수

    private void OnEnable()
    {
        // 몬스터 처치 이벤트 구독
        if (QuestEventManager.Instance != null)
        {
            QuestEventManager.Instance.onMonsterKilled += OnMonsterKilled;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (QuestEventManager.Instance != null)
        {
            QuestEventManager.Instance.onMonsterKilled -= OnMonsterKilled;
        }
    }

    private void OnMonsterKilled(string monsterId)
    {
        // 1. 잡은 몬스터가 목표 몬스터인지 확인
        // 2. 이미 목표를 달성했으면 무시
        if (monsterId == targetMonsterId && currentKillCount < targetKillCount)
        {
            currentKillCount++;
            UpdateState(); // 상태 저장 및 UI 갱신
        }

        // 목표 달성 시 퀘스트 스텝 완료 처리
        if (currentKillCount >= targetKillCount)
        {
            FinishQuestStep();
        }
    }

    private void UpdateState()
    {
        // 상태 저장용 문자열 (예: "5")
        string state = currentKillCount.ToString();
        
        // UI에 표시될 문자열 (예: "슬라임 처치 5 / 10")
        string status = $"{targetMonsterId} 처치 {currentKillCount} / {targetKillCount}";
        
        ChangeState(state, status);
    }

    // 저장된 상태(잡은 마릿수)를 불러오는 함수
    protected override void InitData(QuestStepDataSO data)
    {
        var myData = data as KillMonsterStepDataSO;
        if (myData != null)
        {
            targetMonsterId = myData.monsterID;
            targetKillCount = myData.killCount;
        }
    }

    protected override void SetQuestStepState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            currentKillCount = 0;
        }
        else
        {
            currentKillCount = int.Parse(state);
        }
        UpdateState(); // 불러온 상태로 UI 업데이트
    }
}