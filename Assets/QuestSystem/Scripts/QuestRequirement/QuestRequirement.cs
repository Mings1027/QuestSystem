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
        // 퀘스트 매니저를 통해 해당 ID의 퀘스트가 끝났는지 확인
        // QuestManager가 싱글톤(instance)이라고 가정
        // return QuestManager.instance.GetQuestState(requiredQuestId) == QuestState.FINISHED;
        return true; // 테스트용
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