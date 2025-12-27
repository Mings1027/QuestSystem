namespace QuestSystem
{
    public enum QuestState
    {
        REQUIREMENTS_NOT_MET,
        CAN_START,
        IN_PROGRESS,
        CAN_FINISH,
        FINISHED
    }

    public enum UI_TargetID
    {
        None = 0,

        // 필요한 UI ID들을 여기에 추가하세요
        MainMenu_Button,
        Inventory_Open_Button,
        Character_LevelUp_Button,
        Gacha_Button,
        Quest_Guide_Button
    }
}