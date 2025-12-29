using UnityEngine;

public abstract class QuestStep : MonoBehaviour
{
    private bool isFinished = false;
    protected string questId; // 자식들이 사용할 수 있게 protected
    private int stepIndex;

    public void InitializeQuestStep(string questId, int stepIndex, string questStepState, QuestStepDataSO data)
    {
        // [중요] ID를 먼저 설정해야 InitData에서 GetKillCountForQuest(this.questId)를 쓸 수 있음
        this.questId = questId;
        this.stepIndex = stepIndex;

        // 데이터를 이용해 초기화 (자식 클래스 호출)
        InitData(data);
        
        if (!string.IsNullOrEmpty(questStepState))
        {
            SetQuestStepState(questStepState);
        }
    }

    protected abstract void InitData(QuestStepDataSO data);

    protected void FinishQuestStep()
    {
        if (!isFinished)
        {
            isFinished = true;
            // 이벤트 매니저 호출
            QuestEventManager.Instance.questEvents.AdvanceQuest(questId);
            Destroy(this.gameObject);
        }
    }

    protected void ChangeState(string newState, string newStatus)
    {
        QuestEventManager.Instance.questEvents.QuestStepStateChange(
            questId,
            stepIndex,
            new QuestStepState(newState, newStatus)
        );
    }

    protected abstract void SetQuestStepState(string state);
}