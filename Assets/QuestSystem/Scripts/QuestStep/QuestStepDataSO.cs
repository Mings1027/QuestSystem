using UnityEngine;

public abstract class QuestStepDataSO : ScriptableObject
{
    [Header("실행할 로직 프리팹")]
    public GameObject stepPrefab;
}