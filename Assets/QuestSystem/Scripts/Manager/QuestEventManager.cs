using System;

public class QuestEventManager : Singleton<QuestEventManager>
{
    public QuestEvents questEvents { get; private set; }

    public event Action<string, string, int> onGenericEvent;
    public event Action onQuestConditionChanged;

    public void Init() => questEvents = new QuestEvents();

    // 몬스터 처치 시 호출
    public void MonsterKilled(string monsterId) => NotifyEvent(QuestCategory.Monster, monsterId);

    // 스탯 변경 시 호출
    public void StatChanged(StatType type, int newValue) => NotifyEvent(QuestCategory.Stat, type.ToString(), newValue);

    // 버튼 누르는 퀘스트 시 호출
    public void QuestButtonClicked(string questId) => NotifyEvent(QuestCategory.Action, questId);

    public void NotifyEvent(string category, string targetId, int value = 1)
    {
        onGenericEvent?.Invoke(category, targetId, value);

        // 퀘스트 시작/완료 조건도 함께 체크하게 함 (편의성)
        NotifyQuestConditionChanged();
    }

    public void NotifyQuestConditionChanged() => onQuestConditionChanged?.Invoke();
}