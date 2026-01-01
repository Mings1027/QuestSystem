using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Quest/Reward Data/Gold Reward")]
public class GoldRewardDataSO : QuestRewardDataSO<GoldRewardInfo>
{
    public override Type GetRewardType() => typeof(GoldReward);

    public int GetGoldAmount(string id)
    {
        var info = GetEntry(id);
        return info != null ? info.goldAmount : 100;
    }
}