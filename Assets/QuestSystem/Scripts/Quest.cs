using System;
using UnityEngine;
using Object = UnityEngine.Object;

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
        state = QuestState.REQUIREMENTS_NOT_MET;
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
        if (!CurrentStepExists())
        {
            Debug.LogWarning("Quest Step Index Out of Range: " + info.ID);
            return;
        }

        // 1. 현재 단계의 데이터 SO 가져오기
        QuestStepDataSO stepData = info.steps[currentQuestStepIndex];

        if (stepData == null)
        {
            Debug.LogError($"Quest Step Data is null. QuestId: {info.ID}, StepIndex: {currentQuestStepIndex}");
            return;
        }

        // 2. SO에게 어떤 컴포넌트 타입이 필요한지 물어보기
        Type stepType = stepData.GetQuestStepType();

        // 3. 빈 게임오브젝트 생성 ("QuestStep_0_몬스터처치" 같은 이름 부여)
        GameObject stepGO = new GameObject($"{info.ID}_Step_{currentQuestStepIndex}_{stepType.Name}");
        stepGO.transform.SetParent(parentTransform);

        // 4. 해당 컴포넌트 부착 (AddComponent)
        QuestStep questStep = stepGO.AddComponent(stepType) as QuestStep;

        // 5. 초기화
        if (questStep != null)
        {
            questStep.InitializeQuestStep(
                info.ID,
                currentQuestStepIndex,
                questStepStates[currentQuestStepIndex].state,
                stepData // 데이터 SO 주입
            );
        }
        else
        {
            Debug.LogError($"Failed to add QuestStep component: {stepType.Name} does not inherit from QuestStep.");
        }
    }

    // private GameObject GetCurrentQuestStepPrefab()
    // {
    //     GameObject questStepPrefab = null;
    //     if (CurrentStepExists())
    //     {
    //         questStepPrefab = info.steps[currentQuestStepIndex].stepPrefab;
    //     }
    //     else
    //     {
    //         Debug.LogWarning("Tried to get quest step prefab, but stepIndex was out of range indicating that "
    //                          + "there's no current step: QuestId=" + info.id + ", stepIndex=" + currentQuestStepIndex);
    //     }
    //
    //     return questStepPrefab;
    // }

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

        if (state == QuestState.REQUIREMENTS_NOT_MET)
        {
            fullStatus = "Requirements are not yet met to start this quest.";
        }
        else if (state == QuestState.CAN_START)
        {
            fullStatus = "This quest can be started!";
        }
        else
        {
            // display all previous quests with strikethroughs
            for (int i = 0; i < currentQuestStepIndex; i++)
            {
                fullStatus += "<s>" + questStepStates[i].status + "</s>\n";
            }

            // display the current step, if it exists
            if (CurrentStepExists())
            {
                fullStatus += questStepStates[currentQuestStepIndex].status;
            }

            // when the quest is completed or turned in
            if (state == QuestState.CAN_FINISH)
            {
                fullStatus += "The quest is ready to be turned in.";
            }
            else if (state == QuestState.FINISHED)
            {
                fullStatus += "The quest has been completed!";
            }
        }

        return fullStatus;
    }
}