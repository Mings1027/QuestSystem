using UnityEngine;
using System;

[Serializable]
public abstract class QuestReward
{
    // 보상 지급 로직
    public abstract void GiveReward();

#if UNITY_EDITOR
    // 에디터 리스트에서 타이틀로 보여줄 이름
    public virtual string GetDisplayName()
    {
        return this.GetType().Name; 
    }
#endif
}

[Serializable]
public class GoldReward : QuestReward
{
    public int amount;

    public override void GiveReward()
    {
        Debug.Log($"[Gold] {amount} 지급");
    }

#if UNITY_EDITOR
    public override string GetDisplayName()
    {
        return $"Gold ({amount})";
    }
#endif
}