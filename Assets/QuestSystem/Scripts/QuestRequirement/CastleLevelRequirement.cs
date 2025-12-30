public class CastleLevelRequirement : QuestRequirement
{
    private int targetLevel;

    public override void Init(QuestRequirementDataSO data, string questId)
    {
        var myData = data as CastleLevelRequirementDataSO;
        if (myData != null)
        {
            targetLevel = myData.GetTargetLevel(questId);
        }
    }

    public override bool IsMet()
    {
        return DBPlayerGameData.Instance.castleLevel >= targetLevel;
    }
}