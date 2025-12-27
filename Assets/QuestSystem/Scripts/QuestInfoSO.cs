using System.Collections.Generic;
using QuestSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest System/Quest Info")]
public class QuestInfoSO : ScriptableObject
{
    [field: SerializeField] public string id { get; private set; }
    
    [Header("Display")]
    public string title;
    [TextArea] public string description;

    [Header("Automation")]
    public bool autoStart = true;
    public bool autoComplete = true;

    [Header("Steps Logic")]
    [SerializeReference] public List<QuestStep> steps = new();

    [Header("Rewards")]
    // [변경] 단순 int 대신 보상 클래스 리스트 사용
    [SerializeReference] public List<QuestReward> rewards = new(); 

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(id)) id = this.name;
#endif
    }
}