using UnityEngine;

public class GoldReward : QuestReward
{
    private int amount;

    public override void Init(QuestRewardDataSO data, string questId)
    {
        var myData = data as GoldRewardDataSO;
        if (myData != null)
        {
            // 공유 SO에서 내 퀘스트 ID에 맞는 금액 가져오기
            amount = myData.GetGoldAmount(questId);
        }
    }

    public override void GiveReward()
    {
        Debug.Log($"[Reward] {amount} 골드를 지급했습니다! (연출 가능)");
        DestroySelf();
    }
}