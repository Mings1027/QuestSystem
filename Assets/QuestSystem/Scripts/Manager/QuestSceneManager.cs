using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSceneManager : Singleton<QuestSceneManager>
{
    // [데이터 구조] 하나의 퀘스트 스텝 안에서 순서대로 눌러야 할 UI 리스트
    [System.Serializable]
    public class StepGuideSequence
    {
        [Tooltip("에디터 식별용 메모 (예: 인벤 열기 -> 장착)")]
        public string description;

        public List<RectTransform> uiTargets; // 순서대로 눌러야 할 버튼들
    }

    [System.Serializable]
    public class QuestSequence
    {
        public QuestInfoSO quest;

        // List Index = Quest Step Index 와 일치해야 함
        public List<StepGuideSequence> stepSequences;
    }

    [Header("Configuration")]
    public List<QuestSequence> questSequences = new();

    [Header("UI References")]
    [Tooltip("메인 화면에 있는 가이드 퀘스트 버튼 (시작/완료 시 핑 찍을 대상)")] [SerializeField]
    private GuideQuestButton guideButton;

    [Header("Ping Object")]
    [SerializeField] private GameObject pingPrefab;

    // 런타임용 맵: 퀘스트ID -> 해당 퀘스트의 모든 스텝 가이드 정보
    private Dictionary<string, List<StepGuideSequence>> sequenceMap;

    // 현재 핑 상태 관리 변수
    private RectTransform currentPing;
    private List<RectTransform> currentUiPath; // 현재 스텝에서 눌러야 할 UI 목록
    private int currentUiIndex = 0; // 목록 중 몇 번째를 눌러야 하는지

    public void Init()
    {
        InitializeMap();

        // [핵심] 모든 퀘스트 이벤트가 발생할 때마다 가이드 상태를 새로고침
        QuestEventManager.Instance.questEvents.onStartQuest += (id) => RefreshGuideState();
        QuestEventManager.Instance.questEvents.onAdvanceQuest += (id) => RefreshGuideState();
        QuestEventManager.Instance.questEvents.onFinishQuest += (id) => RefreshGuideState();
        QuestEventManager.Instance.questEvents.onQuestStateChange += (quest) => RefreshGuideState();

        // 초기화 시 1회 실행
        RefreshGuideState();
    }

    private void OnDestroy()
    {
        if (QuestEventManager.Instance != null)
        {
            // 람다식 구독 해제가 어려우므로, 실제 구현 시에는 메서드로 분리하는 것이 좋으나
            // 여기서는 싱글톤 생명주기를 따르므로 생략하거나 아래처럼 처리합니다.
            // (가장 깔끔한 방법은 RefreshGuideState를 래핑한 메서드를 등록하는 것입니다)
        }
    }

    private void InitializeMap()
    {
        sequenceMap = new Dictionary<string, List<StepGuideSequence>>();
        foreach (var seq in questSequences)
        {
            if (seq.quest != null)
            {
                sequenceMap.Add(seq.quest.ID, seq.stepSequences);
            }
        }
    }

    // --------------------------------------------------------------------------
    // [핵심 로직] 현재 상황에 맞춰 어디에 핑을 찍을지 결정하는 중앙 관제탑
    // --------------------------------------------------------------------------
    public void RefreshGuideState()
    {
        // 1. 매니저나 버튼이 준비되지 않았으면 중단
        if (QuestManager.Instance == null || guideButton == null) return;

        // 2. 현재 플레이어가 진행해야 할 퀘스트 순서 가져오기
        int currentSeqIndex = DBPlayerGameData.Instance.completedQuestNumber + 1;
        Quest currentQuest = QuestManager.Instance.GetQuestBySequenceIndex(currentSeqIndex);

        // 3. 퀘스트가 없거나(모두 완료), 가이드 버튼이 꺼져있다면 핑 제거
        if (currentQuest == null || !guideButton.gameObject.activeInHierarchy)
        {
            StopGuide();
            return;
        }

        // 4. 상태에 따른 분기 처리
        switch (currentQuest.state)
        {
            case QuestState.CAN_START:
                // 자동 시작이 아닐 때만 -> 가이드 버튼에 핑 (눌러서 시작하라고)
                if (!currentQuest.info.autoStart)
                {
                    StopStepGuide(); // 혹시 켜져있던 UI 가이드 끄기
                    ShowPing(guideButton.MyRect);
                }
                else
                {
                    // 자동 시작이면 곧 IN_PROGRESS로 바뀔 것이므로 잠시 대기(핑 끔)
                    StopGuide();
                }

                break;

            case QuestState.CAN_FINISH:
                // 완료 가능 -> 가이드 버튼에 핑 (눌러서 보상 받으라고)
                StopStepGuide();
                ShowPing(guideButton.MyRect);
                break;

            case QuestState.IN_PROGRESS:
                // 진행 중 -> 가이드 버튼 핑은 끄고, 퀘스트 내용(UI 버튼 등)에 핑 찍기
                // *주의* 가이드 버튼에는 핑을 찍지 않음!
                StartStepGuide(currentQuest);
                break;

            default:
                // 그 외(REQUIREMENTS_NOT_MET 등) -> 핑 없음
                StopGuide();
                break;
        }
    }

    // --------------------------------------------------------------------------
    // [내부 로직] 퀘스트 진행 단계(Step)에 따른 UI 가이드 표시
    // --------------------------------------------------------------------------
    // 기존 StartStepGuide 함수 수정
    private void StartStepGuide(Quest quest)
    {
        string questId = quest.info.ID;
        int stepIndex = quest.GetQuestData().questStepIndex;
    
        // [가이드 데이터 가져오기]
        if (!sequenceMap.ContainsKey(questId)) 
        {
            StopGuide(); // 데이터 없으면 초기화
            return;
        }
    
        List<StepGuideSequence> allSteps = sequenceMap[questId];
        if (stepIndex >= allSteps.Count)
        {
            StopGuide();
            return;
        }

        List<RectTransform> targetList = allSteps[stepIndex].uiTargets;

        // [핵심 변경점: 변수 없이 로직으로 판단]
        // 1. 지금 들어온 퀘스트가 현재 들고 있는 리스트와 같은 놈인가? (참조 비교)
        if (currentUiPath == targetList)
        {
            // 2. 그렇다면, 인덱스가 끝까지 갔는가? (이미 완료했는가?)
            if (currentUiIndex >= currentUiPath.Count)
            {
                // "이미 다 눌렀네? 핑 켜지 말고 그냥 있어."
                return;
            }
        
            // 3. 진행 중이라면? (핑이 꺼져있을 수도 있으니 다시 켜주기 확인)
            ActivateNextUiTarget();
            return;
        }

        // --- 여기 아래는 "완전히 새로운 퀘스트/스텝"일 때만 실행됨 ---

        // 새 리스트 장착
        currentUiPath = targetList;
        currentUiIndex = 0; // 새 거니까 0부터
        ActivateNextUiTarget();
    }

    private void ActivateNextUiTarget()
    {
        HidePing();

        if (currentUiPath == null || currentUiIndex >= currentUiPath.Count) return;

        RectTransform target = currentUiPath[currentUiIndex];

        if (target == null || !target.gameObject.activeInHierarchy) return;

        ShowPing(target);

        Button btn = target.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnUiClicked);
            btn.onClick.AddListener(OnUiClicked);
        }
    }

    // 유저가 핑이 찍힌 버튼을 눌렀을 때
    private void OnUiClicked()
    {
        if (currentUiPath == null || currentUiIndex >= currentUiPath.Count)
        {
            return;
        }
        
        // 현재 누른 버튼의 리스너 해제 (메모리 누수 및 중복 방지)
        if (currentUiIndex < currentUiPath.Count)
        {
            RectTransform target = currentUiPath[currentUiIndex];
            if (target != null)
            {
                Button btn = target.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveListener(OnUiClicked);
            }
        }

        // 다음 순서로 이동
        currentUiIndex++;

        if (currentUiIndex < currentUiPath.Count)
        {
            ActivateNextUiTarget();
        }
        else
        {
            HidePing();
        }
    }

    private void StopStepGuide()
    {
        currentUiPath = null;
        Debug.Log("StopStepGuide");
        currentUiIndex = 0;
        // HidePing은 호출하지 않음 (바로 가이드 버튼에 핑을 찍을 수도 있으므로)
    }

    public void StopGuide()
    {
        StopStepGuide();
        HidePing();
    }

    // --------------------------------------------------------------------------
    // [핑 시각화] 실제 프리팹 제어
    // --------------------------------------------------------------------------
    public void ShowPing(RectTransform target)
    {
        if (pingPrefab == null || target == null) return;

        if (currentPing == null)
        {
            GameObject obj = Instantiate(pingPrefab, transform); // 일단 매니저 자식으로 생성
            currentPing = obj.GetComponent<RectTransform>();
        }

        // 타겟의 자식으로 붙임 (UI 움직임 따라가도록)
        currentPing.SetParent(target, false);

        // 위치 초기화 (중앙)
        currentPing.anchoredPosition = Vector2.zero;

        // 맨 위에 보이게
        currentPing.SetAsLastSibling();

        currentPing.gameObject.SetActive(true);
    }

    public void HidePing()
    {
        if (currentPing != null)
        {
            currentPing.gameObject.SetActive(false);
            // 부모 관계를 끊어서 타겟 UI가 꺼질 때 같이 꺼지는 문제 방지 (옵션)
            currentPing.SetParent(transform, false);
        }
    }
}