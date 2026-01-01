using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName = "Quest/Reward Data/Gold Reward")]
public class GoldRewardDataSO : QuestRewardDataSO
{
    [Serializable]
    public class GoldRewardInfo
    {
        [HideInInspector] public string questId;
        
        public int questIndex;

        [Tooltip("지급할 골드 양")] public int goldAmount;

        public GoldRewardInfo(string id, int index, int amount)
        {
            questId = id;
            questIndex = index;
            goldAmount = amount;
        }
    }

    public List<GoldRewardInfo> questSpecificDatas = new();

    public override Type GetRewardType() => typeof(GoldReward);

    public int GetGoldAmount(string id)
    {
        var info = questSpecificDatas.Find(x => x.questId == id);
        return info != null ? info.goldAmount : 100;
    }

    public override void SyncQuestData(string questId, int questIndex)
    {
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null) info.questIndex = questIndex;
        else questSpecificDatas.Add(new GoldRewardInfo(questId, questIndex, 100));
        
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
    }
}