using UnityEngine;
using System;

[System.Serializable]
public abstract class QuestReward
{
    [Tooltip("에디터에서 알아보기 쉽게 메모하는 곳 (예: 500골드)")]
    public string label = "보상 이름";

    public abstract void GiveReward();
}

[Serializable]
public class LogReward : QuestReward
{
    [TextArea] public string message = "퀘스트 완료 보상!";

    public override void GiveReward()
    {
        Debug.Log($"<color=yellow>[Reward]</color> {message}");
    }
}

[System.Serializable]
public class SharedReward : QuestReward
{
    [Tooltip("미리 만들어둔 보상 그룹 SO를 연결하세요")] public QuestRewardGroupSO rewardGroup;

    public override void GiveReward()
    {
        if (rewardGroup != null)
        {
            // 그룹 안에 있는 보상들을 전부 지급
            foreach (var reward in rewardGroup.rewards)
            {
                reward.GiveReward();
            }
        }
    }
}

[Serializable]
public class GoldReward : QuestReward
{
    public int amount;

    public override void GiveReward()
    {
        Debug.Log($"[Gold] {amount} 지급");
    }
}