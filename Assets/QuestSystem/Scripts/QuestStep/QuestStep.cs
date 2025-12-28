using UnityEngine;

public abstract class QuestStep : MonoBehaviour
{
    private bool isFinished = false;
    private string questId;
    private int stepIndex;

    public void InitializeQuestStep(string questId, int stepIndex, string questStepState, QuestStepDataSO data)
    {
        this.questId = questId;
        this.stepIndex = stepIndex;

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