using UnityEngine;
using UnityEngine.UI;

public class CastleLevelUpButton : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() =>
        {
            DBPlayerGameData.Instance.LevelUpCastle();
            QuestEventManager.Instance.NotifyQuestConditionChanged();
        });
    }
}