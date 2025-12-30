using UnityEngine;

public abstract class QuestRequirement : MonoBehaviour
{
    public abstract void Init(QuestRequirementDataSO data, string questId);
    public abstract bool IsMet();
}