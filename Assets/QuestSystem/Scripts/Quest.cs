using System;
using UnityEngine;

[Serializable]
public class Quest
{
    // static info
    public QuestInfoSO info;

    // state info
    public QuestState state;
    private int currentQuestStepIndex;
    private QuestStepState[] questStepStates;

    public Quest(QuestInfoSO questInfo)
    {
        info = questInfo;
        state = QuestState.Locked;
        currentQuestStepIndex = 0;

        questStepStates = new QuestStepState[info.steps.Count];
        for (int i = 0; i < questStepStates.Length; i++)
        {
            questStepStates[i] = new QuestStepState();
        }
    }

    public Quest(QuestInfoSO questInfo, QuestState questState, int currentQuestStepIndex,
        QuestStepState[] questStepStates)
    {
        info = questInfo;
        state = questState;
        this.currentQuestStepIndex = currentQuestStepIndex;
        this.questStepStates = questStepStates;

        // if the quest step states and prefabs are different lengths,
        // something has changed during development and the saved data is out of sync.
        if (this.questStepStates.Length != info.steps.Count)
        {
            Debug.LogWarning("Quest Step Prefabs and Quest Step States are "
                             + "of different lengths. This indicates something changed "
                             + "with the QuestInfo and the saved data is now out of sync. "
                             + "Reset your data - as this might cause issues. QuestId: " + info.ID);
        }
    }

    public void MoveToNextStep()
    {
        currentQuestStepIndex++;
    }

    public bool CurrentStepExists()
    {
        return currentQuestStepIndex < info.steps.Count;
    }

    public void InstantiateCurrentQuestStep(Transform parentTransform)
    {
        // 1. 범위 및 유효성 검사
        if (!CurrentStepExists())
        {
            Debug.LogWarning("Quest Step Index Out of Range: " + info.ID);
            return;
        }

        QuestStepDataSO stepData = info.steps[currentQuestStepIndex];
        if (stepData == null)
        {
            Debug.LogError($"Quest Step Data is null. QuestId: {info.ID}, StepIndex: {currentQuestStepIndex}");
            return;
        }

        // 2. 실행할 컴포넌트 타입 가져오기
        Type stepType = stepData.GetQuestStepType();

        GameObject stepGO = new GameObject($"{info.ID}_Step_{currentQuestStepIndex}_{stepType.Name}");
        stepGO.transform.SetParent(parentTransform);

        // 오브젝트 잠시 꺼놓고
        stepGO.SetActive(false);

        QuestStep questStep = stepGO.AddComponent(stepType) as QuestStep;

        if (questStep != null)
        {
            // 초기화해주고
            questStep.InitializeQuestStep(
                info.ID,
                currentQuestStepIndex,
                questStepStates[currentQuestStepIndex].state,
                stepData
            );

            // 오브젝트 켜서 OnEnable에서 QuestEventManager.Instance.onGenericEvent 구독
            stepGO.SetActive(true);
        }
    }

    public void StoreQuestStepState(QuestStepState questStepState, int stepIndex)
    {
        if (stepIndex < questStepStates.Length)
        {
            questStepStates[stepIndex].state = questStepState.state;
            questStepStates[stepIndex].status = questStepState.status;
        }
        else
        {
            Debug.LogWarning("Tried to access quest step data, but stepIndex was out of range: "
                             + "Quest Id = " + info.ID + ", Step Index = " + stepIndex);
        }
    }

    public QuestData GetQuestData()
    {
        return new QuestData(state, currentQuestStepIndex, questStepStates);
    }

    public string GetFullStatusText()
    {
        string fullStatus = "";

        if (state == QuestState.Locked)
        {
            fullStatus = "Requirements are not yet met to start this quest.";
        }
        else if (state == QuestState.CanStart)
        {
            fullStatus = "This quest can be started!";
        }
        else
        {
            for (int i = 0; i < currentQuestStepIndex; i++)
            {
                fullStatus += "<s>" + questStepStates[i].status + "</s>\n";
            }

            if (CurrentStepExists())
            {
                fullStatus += questStepStates[currentQuestStepIndex].status;
            }

            if (state == QuestState.CanFinish)
            {
                fullStatus += "The quest is ready to be turned in.";
            }
            else if (state == QuestState.Finished)
            {
                fullStatus += "The quest has been completed!";
            }
        }

        return fullStatus;
    }
}