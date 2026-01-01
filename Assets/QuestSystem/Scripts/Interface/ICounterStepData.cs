public enum QuestState
{
    Locked,
    CanStart,
    InProgress,
    CanFinish,
    Finished
}

public enum QuestUpdateType
{
    Add,
    Replace
}

public interface ICounterStepData
{
    string GetCategory(); // "Monster", "Stat", "Item" 등
    string GetTargetId(string questId); // 해당 퀘스트의 구체적 타겟(Slime, Attack 등)
    int GetTargetValue(string questId);
    QuestUpdateType GetUpdateType();
}