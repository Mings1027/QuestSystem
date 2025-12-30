using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRequirementGroup", menuName = "Quest/Requirement Group Preset")]
public class QuestRequirementGroupSO : ScriptableObject
{
    // 여러 조건을 리스트로 담아둠 (예: 레벨 10 이상 AND 퀘스트 A 완료)
    [SerializeReference]
    public List<QuestRequirement> requirements = new();
}