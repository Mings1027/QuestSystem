using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestSceneManager : Singleton<QuestSceneManager>
{
    // [변경 1] 하나의 퀘스트 스텝 안에서 순서대로 눌러야 할 UI 리스트들을 담는 클래스
    [System.Serializable]
    public class StepGuideSequence
    {
        [Tooltip("에디터 식별용 메모 (예: 인벤 열기 -> 장착)")] public string description;
        public List<RectTransform> uiTargets; // 순서대로 눌러야 할 버튼들
    }

    [System.Serializable]
    public class QuestSequence
    {
        public QuestInfoSO quest;
        // [변경 2] RectTransform 리스트 대신, 위에서 만든 StepGuideSequence의 리스트를 사용
        // List Index = Quest Step Index 와 일치해야 함
        public List<StepGuideSequence> stepSequences;
    }

    public List<QuestSequence> questSequences = new List<QuestSequence>();

    // 런타임용 맵: 퀘스트ID -> 해당 퀘스트의 모든 스텝 가이드 정보
    private Dictionary<string, List<StepGuideSequence>> sequenceMap;

    // 현재 진행 상태
    private List<RectTransform> currentUiPath; // 현재 스텝에서 눌러야 할 UI 목록
    private int currentUiIndex = 0; // 목록 중 몇 번째를 눌러야 하는지 (서브 인덱스)

    private RectTransform currentPing;
    [SerializeField] private GameObject pingPrefab;

    public void Init()
    {
        InitializeMap();
        QuestEventManager.Instance.questEvents.onStartQuest += StartGuide;
        QuestEventManager.Instance.questEvents.onFinishQuest += StopGuideByEvent;
    }

    private void OnDestroy()
    {
        if (QuestEventManager.Instance != null && QuestEventManager.Instance.questEvents != null)
        {
            QuestEventManager.Instance.questEvents.onStartQuest -= StartGuide;
            QuestEventManager.Instance.questEvents.onFinishQuest -= StopGuideByEvent;
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

    private void StopGuideByEvent(string id) => StopGuide();

    public void StartGuide(string questId)
    {
        if (sequenceMap.ContainsKey(questId))
        {
            // 1. 현재 퀘스트의 진행 단계(Step Index)를 가져옴
            Quest quest = QuestManager.Instance.GetQuestById(questId);
            int stepIndex = (quest != null) ? quest.GetQuestData().questStepIndex : 0;

            // 2. 전체 스텝 데이터 중 현재 단계에 해당하는 'UI 시퀀스'를 가져옴
            List<StepGuideSequence> allSteps = sequenceMap[questId];

            if (stepIndex < allSteps.Count)
            {
                // 이번 스텝에서 눌러야 할 UI 리스트 로드
                currentUiPath = allSteps[stepIndex].uiTargets;
                currentUiIndex = 0; // UI 순서는 처음부터

                // 첫 번째 UI 활성화
                ActivateUiTarget();
            }
            else
            {
                // 데이터가 부족하거나 범위 밖이면 가이드 종료
                StopGuide();
            }
        }
    }

    private void ActivateUiTarget()
    {
        HidePing();

        // 경로가 없거나, 현재 스텝의 UI 조작이 모두 끝났다면 리턴
        if (currentUiPath == null || currentUiIndex >= currentUiPath.Count) return;

        RectTransform target = currentUiPath[currentUiIndex];
        if (target == null) return;

        // 1. 핑 표시
        ShowPing(target);

        // 2. 버튼 이벤트 연결
        Button btn = target.GetComponent<Button>();
        if (btn != null)
        {
            // 중복 방지 후 연결
            btn.onClick.RemoveListener(OnUiClicked);
            btn.onClick.AddListener(OnUiClicked);
        }
    }

    // 버튼 클릭 시 호출 (서브 스텝 진행)
    private void OnUiClicked()
    {
        // 방금 누른 버튼의 리스너 해제
        if (currentUiPath != null && currentUiIndex < currentUiPath.Count)
        {
            Button btn = currentUiPath[currentUiIndex].GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnUiClicked);
        }

        // 다음 UI 순서로 이동
        currentUiIndex++;

        if (currentUiIndex < currentUiPath.Count)
        {
            // 아직 누를 버튼이 더 남았다면 다음 핑 표시
            ActivateUiTarget();
        }
        else
        {
            // 이번 스텝의 UI 가이드는 끝남 (핑 제거)
            // 실제 퀘스트 완료 이벤트(AdvanceQuest)가 발생할 때까지 대기
            StopGuide();
        }
    }

    public void StopGuide()
    {
        HidePing();
        currentUiPath = null;
        currentUiIndex = 0;
    }

    public void ShowPing(RectTransform target)
    {
        if (pingPrefab != null && target.gameObject.activeInHierarchy)
        {
            if (currentPing == null)
                currentPing = Instantiate(pingPrefab, target).GetComponent<RectTransform>();

            Debug.Log(target.name);
            currentPing.SetParent(target, false);
            currentPing.anchoredPosition = Vector2.zero;
            currentPing.gameObject.SetActive(true);
            currentPing.SetAsLastSibling();
            Debug.Log("ShowPing");
        }
    }

    public void HidePing()
    {
        if (currentPing != null)
        {
            currentPing.gameObject.SetActive(false);
            Debug.Log("HidePing");
        }
    }
}
