using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest/Quest Database")]
public class QuestDatabaseSO : ScriptableObject
{
    [Header("등록된 퀘스트 목록")]
    public List<QuestInfoSO> quests = new();

#if UNITY_EDITOR
    [Header("에셋 생성 경로 설정 (Asset Generation Settings)")]
    
    [Tooltip("새 퀘스트(QuestInfoSO)가 생성될 폴더")]
    [SerializeField] private UnityEditor.DefaultAsset questFolder;
    public UnityEditor.DefaultAsset QuestFolder => questFolder;

    [Tooltip("새 조건(Requirements)이 생성될 폴더")]
    [SerializeField] private UnityEditor.DefaultAsset requirementFolder;
    public UnityEditor.DefaultAsset RequirementFolder => requirementFolder;

    [Tooltip("새 단계(Steps)가 생성될 폴더")]
    [SerializeField] private UnityEditor.DefaultAsset stepFolder;
    public UnityEditor.DefaultAsset StepFolder => stepFolder;

    [Tooltip("새 보상(Rewards)이 생성될 폴더")]
    [SerializeField] private UnityEditor.DefaultAsset rewardFolder;
    public UnityEditor.DefaultAsset RewardFolder => rewardFolder;
#endif
}