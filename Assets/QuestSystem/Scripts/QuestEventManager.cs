// 기존 코드 아래에 추가

using System;

public class QuestEventManager : Singleton<QuestEventManager> // 제네릭 싱글톤 적용
{
    public QuestEvents questEvents { get; private set; }

    public event Action<string> onMonsterKilled;
    public void MonsterKilled(string monsterId)
    {
        onMonsterKilled?.Invoke(monsterId);
    }

    public event Action<StatType, int> onStatChanged;
    public void StatChanged(StatType type, int newValue)
    {
        onStatChanged?.Invoke(type, newValue);
    }

    public void Init()
    {
        questEvents = new QuestEvents();
    }
}