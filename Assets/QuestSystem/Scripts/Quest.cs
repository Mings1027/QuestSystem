using System.Collections.Generic;
using UnityEngine;

// 런타임에 돌아가는 퀘스트 인스턴스
public class Quest
{
    public QuestInfoSO Info { get; private set; }
    public QuestState State { get; set; }
    public int CurrentStepIndex { get; private set; }

    // 현재 실행 중인 스텝 객체 (SO에 있는 원본이 아니라 복제본을 사용하는 게 안전하지만, 
    // 여기서는 메모리 절약을 위해 SO의 설정을 참조하되 상태는 별도 관리하거나
    // SO 데이터를 기반으로 런타임에 초기화합니다. 
    // *주의*: SerializeReference된 객체는 런타임에 수정하면 SO가 바뀔 수 있으므로 
    // JSON 직렬화/역직렬화 기법으로 깊은 복사를 하거나, 
    // 간단하게는 스텝 내부 변수를 초기화하며 사용해야 합니다.)
    
    // 이 예제에서는 간단함을 위해 SO의 참조를 가져오되, QuestStep.Initialize에서 상태를 덮어씌웁니다.
    private QuestStep currentStepInstance; 

    public Quest(QuestInfoSO info)
    {
        Info = info;
        State = QuestState.REQUIREMENTS_NOT_MET;
        CurrentStepIndex = 0;
    }

    // 데이터 로드용 생성자
    public Quest(QuestInfoSO info, QuestState state, int stepIndex)
    {
        Info = info;
        State = state;
        CurrentStepIndex = stepIndex;
    }

    public void StartCurrentStep()
    {
        if (CurrentStepIndex < Info.steps.Count)
        {
            // 리스트에 있는 객체를 그대로 쓰면 상태 공유 문제가 생길 수 있으므로
            // 실제 상용 게임에선 Deep Copy를 권장합니다. 
            // 여기선 편의상 바로 사용합니다.
            currentStepInstance = Info.steps[CurrentStepIndex];
            
            // 저장된 상태가 있다면 로드 (여기선 빈 문자열 가정)
            currentStepInstance.Initialize(Info.id, ""); 
            currentStepInstance.OnStart();
        }
    }

    public void UpdateCurrentStep()
    {
        if (State == QuestState.IN_PROGRESS && currentStepInstance != null)
        {
            currentStepInstance.OnUpdate();
        }
    }

    public void AdvanceStep()
    {
        // 현재 스텝 정리
        if (currentStepInstance != null)
        {
            currentStepInstance.OnEnd();
            currentStepInstance = null;
        }

        CurrentStepIndex++;

        // 다음 스텝이 있으면 시작, 없으면 완료 대기
        if (CurrentStepIndex < Info.steps.Count)
        {
            StartCurrentStep();
        }
        else
        {
            State = QuestState.CAN_FINISH;
        }
    }

    public QuestStep GetCurrentStep() => currentStepInstance;
}