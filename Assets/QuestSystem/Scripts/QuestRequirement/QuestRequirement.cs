using UnityEngine;
using System;

[Serializable]
public abstract class QuestRequirement
{
    // 조건이 충족되었는지 확인하는 함수 (true면 통과)
    public abstract bool IsMet();

#if UNITY_EDITOR
    // 에디터 리스트에서 보여줄 이름
    public virtual string GetDisplayName()
    {
        return this.GetType().Name;
    }
#endif
}

[Serializable]
public class LevelRequirement : QuestRequirement
{
    [Min(1)] public int targetLevel;

    public override bool IsMet()
    {
        // 예시: PlayerStats 싱글톤에서 레벨 가져오기
        // return PlayerStats.instance.CurrentLevel >= targetLevel;
        return true; // 테스트용
    }

#if UNITY_EDITOR
    public override string GetDisplayName() => $"Level >= {targetLevel}";
#endif
}

[Serializable]
public class QuestFinishedRequirement : QuestRequirement
{
    public string requiredQuestId; // 또는 QuestInfoSO 참조

    public override bool IsMet()
    {
        // 퀘스트 매니저를 통해 해당 ID의 퀘스트가 끝났는지 확인
        // QuestManager가 싱글톤(instance)이라고 가정
        // return QuestManager.instance.GetQuestState(requiredQuestId) == QuestState.FINISHED;
        return true; // 테스트용
    }

#if UNITY_EDITOR
    public override string GetDisplayName() => $"Finish Quest: {requiredQuestId}";
#endif
}

[Serializable]
public class CurrencyRequirement : QuestRequirement
{
    public double requiredGold;

    public override bool IsMet()
    {
        // 예: CurrencyManager.instance.Gold >= requiredGold;
        return true; 
    }

#if UNITY_EDITOR
    public override string GetDisplayName() => $"Gold >= {requiredGold}";
#endif
}