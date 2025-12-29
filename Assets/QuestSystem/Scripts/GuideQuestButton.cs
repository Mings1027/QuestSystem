using UnityEngine;
using UnityEngine.UI;

public class GuideQuestButton : MonoBehaviour
{
    private Button myButton;
    private RectTransform myRectTransform;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        myRectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        myButton.onClick.AddListener(OnClickGuide);

        if (QuestEventManager.Instance != null)
        {
            QuestEventManager.Instance.questEvents.onQuestStateChange += OnQuestStateChange;
            CheckPingState();
        }
    }

    private void OnDisable()
    {
        myButton.onClick.RemoveListener(OnClickGuide);

        if (QuestEventManager.Instance != null)
        {
            QuestEventManager.Instance.questEvents.onQuestStateChange -= OnQuestStateChange;
        }

        // 버튼이 꺼질 때 핑도 같이 꺼주는 센스
        if (QuestSceneManager.Instance != null)
        {
            QuestSceneManager.Instance.HidePing();
        }
    }

    private void OnClickGuide()
    {
        if (QuestManager.Instance != null)
        {
            // 다음 순서의 퀘스트를 찾아 시작 시도
            QuestManager.Instance.TryStartNextQuest();
        }
    }

    private void OnQuestStateChange(Quest quest)
    {
        CheckPingState();
    }

    private void CheckPingState()
    {
        if (QuestManager.Instance == null || QuestSceneManager.Instance == null) return;

        // 1. 현재 진행해야 할 퀘스트 순서 가져오기
        int currentSequenceIndex = DBPlayerGameData.Instance.completedQuestNumber + 1;
        
        // 2. 해당 퀘스트 정보 가져오기 (QuestManager에 이 함수가 있어야 합니다!)
        Quest currentQuest = QuestManager.Instance.GetQuestBySequenceIndex(currentSequenceIndex);
        
        if (currentQuest != null)
        {
            // [조건 A] 완료 가능할 때 (보상 받기 위해 핑 표시)
            bool canFinish = (currentQuest.state == QuestState.CAN_FINISH);
            
            // [조건 B] 시작 가능할 때 (수동 시작을 유도하기 위해 핑 표시)
            // 단, AutoStart가 켜져 있으면 자동으로 시작될 테니 굳이 핑을 찍지 않아도 됨(순식간에 넘어감).
            // AutoStart가 꺼져 있을 때만 유저가 눌러야 하므로 핑을 찍음.
            bool canStart = (currentQuest.state == QuestState.CAN_START && !currentQuest.info.autoStart);

            if (canFinish || canStart)
            {
                // 내 위치(버튼)에 핑을 찍어달라고 요청
                QuestSceneManager.Instance.ShowPing(myRectTransform);
            }
        }
    }
}