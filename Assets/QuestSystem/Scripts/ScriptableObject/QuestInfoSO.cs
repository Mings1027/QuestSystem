using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "QuestInfoSO", menuName = "ScriptableObjects/QuestInfoSO", order = 1)]
public class QuestInfoSO : ScriptableObject
{
    [SerializeField] private string id;
    public string ID => id;

    [Header("General")]
    [SerializeField] private string displayName;
    public string DisplayName
    {
        get => displayName;
        set=> displayName = value;
    }

    [Tooltip("체크 시 조건(Requirements)이 충족되는 즉시 퀘스트가 시작됩니다.")]
    public bool autoStart;
    
    [Tooltip("체크 시 모든 스텝을 완수하면 즉시 보상을 받고 퀘스트가 완료됩니다.")]
    public bool autoComplete;
    
    [Header("Requirements")]
    public List<QuestRequirementDataSO> requirements = new();
    
    [Header("Steps")]
    public List<QuestStepDataSO> steps = new();
    
    [Header("Rewards")]
    public List<QuestRewardDataSO> rewards = new();
    
    // ensure the id is always the name of the Scriptable Object asset
    private void OnValidate()
    {
#if UNITY_EDITOR
        id = name;
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}