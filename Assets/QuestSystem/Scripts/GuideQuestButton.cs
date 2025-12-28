using UnityEngine;
using UnityEngine.UI;

public class GuideQuestButton : MonoBehaviour
{
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        myButton.onClick.AddListener(OnClickGuide);
    }

    private void OnDisable()
    {
        myButton.onClick.RemoveListener(OnClickGuide);
    }

    private void OnClickGuide()
    {
        if (QuestManager.Instance != null)
        {
            // 다음 순서의 퀘스트를 찾아 시작 시도
            QuestManager.Instance.TryStartNextQuest();
        }
    }
}