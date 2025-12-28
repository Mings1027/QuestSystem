using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSceneManager : MonoBehaviour
{
    public static QuestSceneManager instance;

    [System.Serializable]
    public class QuestSequence
    {
        public QuestInfoSO quest; // 키(Key) 역할
        public List<RectTransform> uiSteps; // 값(Value) 역할: 순서대로 눌러야 할 UI들
    }

    // 이 리스트가 씬에 저장됩니다. (에디터에서 수정할 대상)
    public List<QuestSequence> questSequences = new List<QuestSequence>();

    // 런타임에 빠르게 찾기 위한 딕셔너리
    private Dictionary<string, List<RectTransform>> sequenceMap;

    // 현재 진행 상태 변수들
    private List<RectTransform> currentPath;
    private int currentStepIndex = 0;
    private GameObject currentPing;
    [SerializeField] private GameObject pingPrefab; // 핑 프리팹

    public void Init()
    {
        instance = this;
        InitializeMap();

        QuestEventManager.instance.questEvents.onStartQuest += StartGuide;
        QuestEventManager.instance.questEvents.onFinishQuest += StopGuideByEvent;
    }

    private void OnDestroy()
    {
        if (QuestEventManager.instance != null && QuestEventManager.instance.questEvents != null)
        {
            QuestEventManager.instance.questEvents.onStartQuest -= StartGuide;
            QuestEventManager.instance.questEvents.onFinishQuest -= StopGuideByEvent;
        }
    }

    private void InitializeMap()
    {
        sequenceMap = new Dictionary<string, List<RectTransform>>();
        foreach (var seq in questSequences)
        {
            if (seq.quest != null)
            {
                sequenceMap.Add(seq.quest.id, seq.uiSteps);
            }
        }
    }

    private void StopGuideByEvent(string id)
    {
        StopGuide();
    }

    // --- 퀘스트 시스템에서 호출 ---
    public void StartGuide(string questId)
    {
        if (sequenceMap.ContainsKey(questId))
        {
            currentPath = sequenceMap[questId];
            currentStepIndex = 0;

            // 첫 번째 UI에 리스너 연결 및 핑 표시
            ActivateStep(currentStepIndex);
        }
    }

    private void ActivateStep(int index)
    {
        HidePing();

        if (currentPath == null || index >= currentPath.Count) return;

        RectTransform target = currentPath[index];
        if (target == null) return;

        // 1. 핑 찍기
        ShowPing(target);

        // 2. 버튼 클릭 이벤트 동적 연결
        // (버튼에 별도 컴포넌트가 없어도 여기서 직접 연결합니다)
        Button btn = target.GetComponent<Button>();
        if (btn != null)
        {
            // 기존 리스너 중복 방지를 위해 remove 먼저
            btn.onClick.RemoveListener(OnTargetClicked);
            btn.onClick.AddListener(OnTargetClicked);
        }
    }

    // 버튼이 클릭되었을 때 호출됨
    private void OnTargetClicked()
    {
        // 방금 누른 버튼의 리스너 제거 (일회용)
        if (currentPath != null && currentStepIndex < currentPath.Count)
        {
            Button btn = currentPath[currentStepIndex].GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnTargetClicked);
        }

        // 다음 단계로
        currentStepIndex++;
        if (currentStepIndex < currentPath.Count)
        {
            ActivateStep(currentStepIndex);
        }
        else
        {
            StopGuide(); // 끝
        }
    }

    public void StopGuide()
    {
        HidePing();
        currentPath = null;
    }

    private void ShowPing(RectTransform target)
    {
        if (pingPrefab != null && target.gameObject.activeInHierarchy)
        {
            currentPing = Instantiate(pingPrefab, target);
        }
    }

    private void HidePing()
    {
        if (currentPing != null) Destroy(currentPing);
    }
}