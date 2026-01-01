using UnityEngine;

public class DBPlayerGameData : Singleton<DBPlayerGameData>
{
    public int completedQuestNumber = -1;
    
    public int castleLevel;
    public int attackPower;

    [ContextMenu("Level Up Castle")]
    public void LevelUpCastle()
    {
        castleLevel++;
        Debug.Log($"[Data] 성 레벨업! 현재 레벨: {castleLevel}");
    }
}