using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Requirement/Level Requirement")]
public class CastleLevelRequirementDataSO : QuestRequirementDataSO<LevelReqInfo>
{
    public override Type GetRequirementType() => typeof(CastleLevelRequirement);

    public int GetTargetLevel(string id)
    {
        // 부모의 헬퍼 함수(GetEntry) 사용
        var info = GetEntry(id);
        return info != null ? info.targetLevel : 1;
    }
}