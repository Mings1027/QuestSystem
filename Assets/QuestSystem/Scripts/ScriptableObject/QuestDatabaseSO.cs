using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Quest/Quest Database")]
public class QuestDatabaseSO : ScriptableObject
{
    [Header("등록된 퀘스트 목록")]
    public List<QuestInfoSO> quests = new();
}