using UnityEngine;

public abstract class QuestReward : MonoBehaviour
{
    // 데이터를 받아 초기화
    public abstract void Init(QuestRewardDataSO data, string questId);

    // 실제 보상 지급 로직
    public abstract void GiveReward();

    // 보상 지급 후 스스로 파괴 (연출이 필요하면 딜레이 후 파괴 가능)
    protected void DestroySelf()
    {
        Destroy(gameObject);
    }
}