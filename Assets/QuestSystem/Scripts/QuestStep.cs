using System;
using QuestSystem;
using UnityEngine;

[System.Serializable]
public abstract class QuestStep
{
    protected bool isFinished = false;
    protected string questId;

    // 초기화 (매니저가 호출)
    public virtual void Initialize(string questId, string savedState)
    {
        this.questId = questId;
        this.isFinished = false;
        RestoreState(savedState);
    }

    // 퀘스트 시작 시 1회 호출
    public abstract void OnStart();

    // 매 프레임 호출 (필요한 경우)
    public virtual void OnUpdate() { }

    // 퀘스트 종료/중단 시 호출 (이벤트 구독 해제 필수)
    public abstract void OnEnd();

    // 퀘스트 완료 처리
    protected void FinishQuestStep()
    {
        if (!isFinished)
        {
            isFinished = true;
            QuestManager.Instance.AdvanceQuest(questId);
        }
    }

    // 상태 저장/로드
    public abstract string GetState();
    protected abstract void RestoreState(string state);
}

[AttributeUsage(AttributeTargets.Class)]
public class QuestMenuAttribute : Attribute
{
    public string Path { get; private set; }
    public QuestMenuAttribute(string path)
    {
        Path = path;
    }
}

// 1. 몬스터 처치 스텝
[System.Serializable]
[QuestMenu("Combat/Monster Kill")]
public class KillingQuestStep : QuestStep
{
    public int targetMonsterId;

    public int targetCount;

    private int currentCount;

    public override void OnStart()
    {
        QuestEventBus.OnMonsterKilled += HandleMonsterKilled;
        Debug.Log($"[Quest] 몬스터 {targetMonsterId} 처치 시작: {currentCount}/{targetCount}");
    }

    public override void OnEnd()
    {
        QuestEventBus.OnMonsterKilled -= HandleMonsterKilled;
    }

    private void HandleMonsterKilled(int id)
    {
        if (id == targetMonsterId)
        {
            currentCount++;
            Debug.Log($"[Quest] 몬스터 처치 진행: {currentCount}/{targetCount}");
            if (currentCount >= targetCount)
            {
                FinishQuestStep();
            }
        }
    }

    public override string GetState() => currentCount.ToString();

    protected override void RestoreState(string state)
    {
        if (int.TryParse(state, out int result)) currentCount = result;
        else currentCount = 0;
    }
}

// 2. UI 상호작용 스텝 (핑 찍기 포함)
[System.Serializable]
[QuestMenu("UI Interaction")]
public class UIInteractionQuestStep : QuestStep
{
    public UI_TargetID targetUI;

    public override void OnStart()
    {
        // 핑 띄우기
        TutorialOverlayManager.Instance.ShowPing(targetUI);
        // 클릭 이벤트 구독
        QuestEventBus.OnUIClicked += HandleUIClick;
    }

    public override void OnEnd()
    {
        // 핑 제거
        TutorialOverlayManager.Instance.HidePing();
        QuestEventBus.OnUIClicked -= HandleUIClick;
    }

    private void HandleUIClick(UI_TargetID id)
    {
        if (id == targetUI)
        {
            FinishQuestStep();
        }
    }

    public override string GetState() => ""; // 저장할 상태 없음
    protected override void RestoreState(string state) { }
}