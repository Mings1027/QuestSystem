using UnityEngine;
using System;

[Serializable]
public abstract class QuestRequirement
{
    public string label = "조건 작성";

    // 조건이 충족되었는지 확인하는 함수 (true면 통과)
    public abstract bool IsMet();
}

[System.Serializable]
public class SharedRequirement : QuestRequirement
{
    [Tooltip("미리 설정해둔 조건 그룹 SO를 연결하세요.")] public QuestRequirementGroupSO requirementGroup;

    public override bool IsMet()
    {
        // 연결된 SO가 없으면 조건이 없는 것으로 간주 (통과)
        if (requirementGroup == null || requirementGroup.requirements == null)
        {
            return true;
        }

        // 그룹 내의 *모든* 조건이 만족되어야 함 (AND 조건)
        foreach (var req in requirementGroup.requirements)
        {
            if (!req.IsMet())
            {
                return false; // 하나라도 만족 안 하면 실패
            }
        }

        return true; // 모두 만족하면 통과
    }
}

[Serializable]
public class QuestFinishedRequirement : QuestRequirement
{
    public QuestInfoSO questInfoSo;

    public override bool IsMet()
    {
        // 1. 설정된 퀘스트가 없으면 그냥 통과 (설정 실수 방지)
        if (questInfoSo == null) return true;

        // 2. QuestManager에서 해당 퀘스트를 찾아 상태 확인
        // (QuestManager.Instance가 있는지 확인)
        if (QuestManager.Instance == null) return false;

        Quest quest = QuestManager.Instance.GetQuestById(questInfoSo.id);

        // 3. 퀘스트가 존재하고, 상태가 FINISHED여야만 true 반환
        return quest != null && quest.state == QuestState.FINISHED;
    }
}

[Serializable]
public class CastleLevelRequirement : QuestRequirement
{
    [Min(1)] public int targetCastleLevel;

    public override bool IsMet()
    {
        // DBPlayerGameData 싱글톤에서 현재 레벨을 가져와 비교
        return DBPlayerGameData.Instance.castleLevel >= targetCastleLevel;
    }
}