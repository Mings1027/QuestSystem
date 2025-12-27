using System;
using QuestSystem;

// GameEventsManager 대신 사용할 정적 이벤트 버스입니다.
public static class QuestEventBus
{
    // 몬스터 처치 이벤트 (몬스터ID)
    public static event Action<int> OnMonsterKilled;
    public static void RaiseMonsterKilled(int monsterId) => OnMonsterKilled?.Invoke(monsterId);

    // UI 클릭 이벤트 (TargetID)
    public static event Action<UI_TargetID> OnUIClicked;
    public static void RaiseUIClicked(UI_TargetID id) => OnUIClicked?.Invoke(id);
}