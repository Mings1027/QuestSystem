namespace QuestSystem
{
    using UnityEngine;
    using System;

    [Serializable]
    public abstract class QuestReward
    {
        // 보상 지급 로직
        public abstract void GiveReward();

        // UI 표시용 (예: "100 Gold", "Iron Sword x1")
        public abstract string GetDescription();

        // UI 아이콘 (필요하다면)
        // public abstract Sprite GetIcon();
    }

// 에디터 메뉴용 어트리뷰트 (QuestMenuAttribute와 동일한 원리)
    [AttributeUsage(AttributeTargets.Class)]
    public class RewardMenuAttribute : Attribute
    {
        public string Path { get; private set; }

        public RewardMenuAttribute(string path)
        {
            Path = path;
        }
    }


// 1. 재화 보상 (골드, 다이아 등)
    [Serializable]
    [RewardMenu("Currency/Gold")]
    public class GoldReward : QuestReward
    {
        public int amount;

        public override void GiveReward()
        {
            // 실제 게임의 재화 매니저 호출
            // 예: CurrencyManager.Instance.AddGold(amount);
            Debug.Log($"[Reward] 골드 {amount} 획득!");
        }

        public override string GetDescription() => $"{amount} Gold";
    }

// 2. 경험치 보상
    [Serializable]
    [RewardMenu("Stat/Experience")]
    public class ExpReward : QuestReward
    {
        public int amount;

        public override void GiveReward()
        {
            // 예: PlayerManager.Instance.AddExp(amount);
            Debug.Log($"[Reward] 경험치 {amount} 획득!");
        }

        public override string GetDescription() => $"{amount} EXP";
    }

// 3. 아이템 보상 (ItemSO가 있다고 가정)
    [Serializable]
    [RewardMenu("Item/General Item")]
    public class ItemReward : QuestReward
    {
        public string itemName; // 혹은 public ItemSO itemData;
        public int count;

        public override void GiveReward()
        {
            // 예: InventoryManager.Instance.AddItem(itemData, count);
            Debug.Log($"[Reward] 아이템 {itemName} {count}개 획득!");
        }

        public override string GetDescription() => $"{itemName} x{count}";
    }
}