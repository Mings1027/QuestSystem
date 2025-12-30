using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quest/Reward Group Preset")]
public class QuestRewardGroupSO : ScriptableObject
{
    [SerializeReference]
    public List<QuestReward> rewards = new();
}