using UnityEngine;

[CreateAssetMenu(fileName = "Step_Stat", menuName = "Quest/Step Data/Reach Stat")]
public class ReachStatStepDataSO : QuestStepDataSO
{
    [Header("스탯 퀘스트 전용 설정")]
    public StatType targetStat;
    public int targetValue;
}