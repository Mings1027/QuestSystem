using UnityEngine;
using UnityEngine.UI;

public class GuideQuestButton : MonoBehaviour
{
    private Button myButton;
    // QuestSceneManager가 이 버튼의 위치를 알 수 있도록 프로퍼티 제공
    public RectTransform MyRect => GetComponent<RectTransform>();

    private void Awake()
    {
        myButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        myButton.onClick.AddListener(OnClickGuide);
        
        // 버튼이 활성화될 때, 매니저에게 "나 켜졌어, 핑 상태 확인해줘"라고 요청
        if (QuestSceneManager.Instance != null)
        {
            QuestSceneManager.Instance.RefreshGuideState();
        }
    }

    private void OnDisable()
    {
        myButton.onClick.RemoveListener(OnClickGuide);
    }

    private void OnClickGuide()
    {
        if (QuestManager.Instance != null)
        {
            // 로직 실행 (핑 제어는 매니저가 알아서 함)
            QuestManager.Instance.TryStartNextQuest();
        }
    }
}