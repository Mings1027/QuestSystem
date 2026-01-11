using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class QuestSceneManager : Singleton<QuestSceneManager>
{
    [System.Serializable]
    public class StepGuideSequence
    {
        [Tooltip("에디터 식별용 메모 (예: 인벤 열기 -> 장착)")]
        public string description;

        [SerializeReference] public List<GuideAction> actions = new List<GuideAction>();
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
    private CancellationTokenSource _guideCts;

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
                StopGuide();
                ShowPing(guideButton.MyRect);
                break;

            case QuestState.InProgress:
                // 진행 중일 때 구체적인 UI 가이드 시작
                StartStepGuide(currentQuest).Forget();
                break;

            default:
                StopGuide();
                break;
        }
    }

    private async UniTaskVoid StartStepGuide(Quest quest)
    {
        string questId = quest.info.ID;
        int stepIndex = quest.GetQuestData().questStepIndex;

        // 데이터 검증
        if (!sequenceMap.TryGetValue(questId, out var allSteps) || stepIndex >= allSteps.Count)
        {
            StopGuide();
            return;
        }

        List<GuideAction> targetActions = allSteps[stepIndex].actions;

        // 기존 가이드 취소 및 리소스 정리
        CancelCurrentGuide();

        // 새 토큰 생성
        _guideCts = new CancellationTokenSource();
        var token = _guideCts.Token;

        try
        {
            // [정말 심플해진 실행 루프]
            foreach (var action in targetActions)
            {
                // 토큰이 취소되었는지 체크 (중간에 퀘스트 완료하거나 창 닫았을 때)
                if (token.IsCancellationRequested) break;

                // 액션 실행 및 대기 (클릭할 때까지, 혹은 시간이 지날 때까지 여기서 멈춰있음)
                await action.Execute(token);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 가이드가 취소됨 (정상적인 흐름)
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Guide Error: {e.Message}");
        }
        finally
        {
            // 모든 액션이 끝났거나 취소되었을 때 핑 제거
            HidePing();
        }
    }
  
    public void StopGuide()
    {
        CancelCurrentGuide();
        HidePing();
    }

    private void CancelCurrentGuide()
    {
        if (_guideCts != null)
        {
            _guideCts.Cancel();
            _guideCts.Dispose();
            _guideCts = null;
        }
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