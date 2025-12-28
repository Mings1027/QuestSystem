using UnityEngine;

[CreateAssetMenu(fileName = "Step_Monster", menuName = "Quest/Step Data/Kill Monster")]
public class KillMonsterStepDataSO : QuestStepDataSO
{
    [Header("몬스터 퀘스트 전용 설정")]
    public string monsterID;
    public int killCount;
}