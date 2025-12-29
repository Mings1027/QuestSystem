using UnityEngine;

public class KillMonsterQuestStep : QuestStep
{
    [Header("설정 (디버그용)")]
    [SerializeField] private string targetMonsterId; 
    [SerializeField] private int targetKillCount;

    private int currentKillCount;

    protected override void InitData(QuestStepDataSO data)
    {
        var myData = data as KillMonsterStepDataSO;
        if (myData != null)
        {
            // 1. 공통 데이터 (몬스터 종류)
            this.targetMonsterId = myData.monsterID;
            
            // 2. [핵심] 퀘스트별 고유 데이터 (마릿수)
            // 부모 클래스(QuestStep)에 저장된 questId를 사용해 조회
            this.targetKillCount = myData.GetKillCountForQuest(this.questId);
        }
    }

    private void OnEnable()
    {
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.onMonsterKilled += OnMonsterKilled;
    }

    private void OnDisable()
    {
        if (QuestEventManager.Instance != null)
            QuestEventManager.Instance.onMonsterKilled -= OnMonsterKilled;
    }

    private void OnMonsterKilled(string monsterId)
    {
        if (monsterId == targetMonsterId && currentKillCount < targetKillCount)
        {
            currentKillCount++;
            UpdateState();
        }

        if (currentKillCount >= targetKillCount)
        {
            FinishQuestStep();
        }
    }

    private void UpdateState()
    {
        string state = currentKillCount.ToString();
        string status = $"{targetMonsterId} 처치 {currentKillCount} / {targetKillCount}";
        ChangeState(state, status);
    }

    protected override void SetQuestStepState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            currentKillCount = 0;
        }
        else
        {
            int.TryParse(state, out currentKillCount);
        }
        UpdateState();
    }
}