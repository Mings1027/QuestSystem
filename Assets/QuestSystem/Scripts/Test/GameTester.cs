using UnityEngine;

public class GameTester : MonoBehaviour
{
    [Header("성 레벨 테스트")]
    public int levelToSet = 5;

    [Header("공격력 테스트")]
    public int attackPower;

    [Header("몬스터 사냥 테스트")]
    public string monsterIdToKill = "Slime";

    [ContextMenu("1. 성 레벨 설정")]
    public void SetCastleLevel()
    {
        DBPlayerGameData.Instance.castleLevel = levelToSet;
        QuestEventManager.Instance.NotifyQuestConditionChanged();
        Debug.Log($"[Test] 성 레벨을 {levelToSet}로 설정함. (조건 충족 확인용)");
    }

    [ContextMenu("2. 몬스터 1마리 처치")]
    public void KillMonster()
    {
        Debug.Log($"[Test] 몬스터 처치: {monsterIdToKill}");
        QuestEventManager.Instance.MonsterKilled(monsterIdToKill);
    }

    [ContextMenu("공격력 설정")]
    public void AttackPower()
    {
        DBPlayerGameData.Instance.attackPower = attackPower;
    }
    
    [ContextMenu("3. 몬스터 10마리 한번에 처치")]
    public void KillTenMonsters()
    {
        for (int i = 0; i < 10; i++)
        {
            KillMonster();
        }
    }
}