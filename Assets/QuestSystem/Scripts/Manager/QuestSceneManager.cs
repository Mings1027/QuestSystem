using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSceneManager : Singleton<QuestSceneManager>
{
    [System.Serializable]
    public class StepGuideSequence
    {
        [Tooltip("에디터 식별용 메모 (예: 인벤 열기 -> 장착)")]
        public string description;

        [SerializeField] private List<RectTransform> uiTargets; // 순서대로 눌러야 할 버튼들
        public List<RectTransform> UiTargets => uiTargets;
    }

    [System.Serializable]
    public class QuestSequence
    {
        [SerializeField] private QuestInfoSO quest;
        [SerializeField] private List<StepGuideSequence> stepSequences; // Step Index와 일치
        
        public QuestInfoSO Quest => quest;
        public List<StepGuideSequence> StepSequences => stepSequences;

        public void Init(QuestInfoSO questInfoSo)
        {
            quest = questInfoSo;
            stepSequences = new List<StepGuideSequence>();
        }
    }

    [Header("Configuration")]
    public List<QuestSequence> questSequences = new();

    [Header("UI Components")]
    [SerializeField] private GuideQuestButton guideButton; // 메인 가이드 버튼
    [SerializeField] private GameObject pingPrefab; // 가이드용 핑 프리팹

    // 런타임 맵: 퀘스트ID -> 해당 퀘스트의 모든 스텝 가이드 정보
    private Dictionary<string, List<StepGuideSequence>> sequenceMap;

    private RectTransform currentPing;
    private List<RectTransform> currentUiPath; 
    private int currentUiIndex = 0; 

    public void Init()
    {
        InitializeMap();

        // 퀘스트 상태 변화 시 가이드 상태 새로고침 구독
        QuestEventManager.Instance.questEvents.onQuestStateChange += _ => RefreshGuideState();
        
        RefreshGuideState();
    }

    private void InitializeMap()
    {
        sequenceMap = new Dictionary<string, List<StepGuideSequence>>();
        foreach (var seq in questSequences)
        {
            if (seq.Quest != null && !sequenceMap.ContainsKey(seq.Quest.ID))
            {
                sequenceMap.Add(seq.Quest.ID, seq.StepSequences);
            }
        }
    }

    public void RefreshGuideState()
    {
        if (QuestManager.Instance == null || guideButton == null) return;

        // 현재 진행해야 할 퀘스트 가져오기 (DBPlayerGameData 연동)
        int currentSeqIndex = DBPlayerGameData.Instance.completedQuestNumber + 1;
        Quest currentQuest = QuestManager.Instance.GetQuestBySequenceIndex(currentSeqIndex);

        if (currentQuest == null)
        {
            StopGuide();
            return;
        }

        switch (currentQuest.state)
        {
            case QuestState.CanStart:
            case QuestState.CanFinish:
                // 시작/보상 수령 가능 시 가이드 버튼에 핑 표시
                StopStepGuide();
                ShowPing(guideButton.MyRect);
                break;

            case QuestState.InProgress:
                // 진행 중일 때 구체적인 UI 가이드 시작
                StartStepGuide(currentQuest);
                break;

            default:
                StopGuide();
                break;
        }
    }

    private void StartStepGuide(Quest quest)
    {
        string questId = quest.info.ID;
        int stepIndex = quest.GetQuestData().questStepIndex;

        if (!sequenceMap.TryGetValue(questId, out var allSteps) || stepIndex >= allSteps.Count)
        {
            StopGuide();
            return;
        }

        List<RectTransform> targetList = allSteps[stepIndex].UiTargets;

        // 이미 동일한 경로를 안내 중인지 확인
        if (currentUiPath == targetList)
        {
            if (currentUiIndex < currentUiPath.Count) ActivateNextUiTarget(quest);
            return;
        }

        currentUiPath = targetList;
        currentUiIndex = 0;
        ActivateNextUiTarget(quest);
    }

    private void ActivateNextUiTarget(Quest quest)
    {
        HidePing();

        if (currentUiPath == null || currentUiIndex >= currentUiPath.Count) return;

        RectTransform target = currentUiPath[currentUiIndex];

        if (target == null || !target.gameObject.activeInHierarchy) return;

        ShowPing(target);

        // [자동 주입] 리스트의 마지막 원소라면 트리거 주입
        if (currentUiIndex == currentUiPath.Count - 1)
        {
            InjectTrigger(target, quest);
        }

        // 핑 찍힌 버튼 클릭 시 다음 단계 가이드 유도
        Button btn = target.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnUiClicked);
            btn.onClick.AddListener(OnUiClicked);
        }
    }

    private void InjectTrigger(RectTransform target, Quest quest)
    {
        int stepIdx = quest.GetQuestData().questStepIndex;
        var stepData = quest.info.steps[stepIdx];

        if (stepData is ICounterStepData counterData)
        {
            if (counterData.GetCategory() != QuestCategory.Action) 
                return;

            var trigger = target.GetComponent<QuestButtonTrigger>() 
                          ?? target.gameObject.AddComponent<QuestButtonTrigger>();
            
            trigger.Setup(counterData.GetCategory(), counterData.GetTargetId(quest.info.ID));
        }
    }

    private void OnUiClicked()
    {
        if (currentUiPath != null && currentUiIndex < currentUiPath.Count)
        {
            RectTransform target = currentUiPath[currentUiIndex];
            if (target != null) target.GetComponent<Button>()?.onClick.RemoveListener(OnUiClicked);
        }

        currentUiIndex++;
        
        // 다음 UI 가이드로 넘어가거나 상태 갱신
        RefreshGuideState(); 
    }

    public void StopGuide()
    {
        StopStepGuide();
        HidePing();
    }

    private void StopStepGuide()
    {
        currentUiPath = null;
        currentUiIndex = 0;
    }

    public void ShowPing(RectTransform target)
    {
        if (pingPrefab == null || target == null) return;

        if (currentPing == null)
        {
            GameObject obj = Instantiate(pingPrefab, transform);
            currentPing = obj.GetComponent<RectTransform>();
        }

        currentPing.SetParent(target, false);
        currentPing.anchoredPosition = Vector2.zero;
        currentPing.SetAsLastSibling();
        currentPing.gameObject.SetActive(true);
    }

    public void HidePing()
    {
        if (currentPing != null)
        {
            currentPing.gameObject.SetActive(false);
            currentPing.SetParent(transform, false);
        }
    }
}