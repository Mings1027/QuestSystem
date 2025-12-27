using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest System/Quest Database")]
public class QuestDatabaseSO : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField] private DefaultAsset questFolder;
    public DefaultAsset QuestFolder => questFolder;
#endif
    
    [Header("Quest List")]
    // 이 리스트에 프로젝트의 모든 QuestInfoSO를 드래그해서 넣어주세요.
    public List<QuestInfoSO> allQuests = new List<QuestInfoSO>();

    // 런타임에 ID로 빠르게 찾기 위한 헬퍼 함수
    public QuestInfoSO FindQuestById(string id)
    {
        return allQuests.Find(q => q.id == id);
    }
}