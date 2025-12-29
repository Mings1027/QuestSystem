using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : Singleton<QuestManager>
{
    [Header("Managers")]
    [SerializeField] private QuestEventManager questEventManager;

    [SerializeField] private QuestSceneManager questSceneManager;

    [Header("Data Source")]
    [SerializeField] private QuestDatabaseSO questDatabase;

    [Header("Debug View")]
    [SerializeField] private List<Quest> debugQuestList = new List<Quest>();

    private Dictionary<string, Quest> questMap;

    protected override void Awake()
    {
        base.Awake();

        if (questEventManager != null) questEventManager.Init();
        if (questSceneManager != null) questSceneManager.Init();

        // 맵 생성 및 상태 초기화
        questMap = CreateQuestMap();
    }

    private void OnEnable()
    {
        QuestEventManager.Instance.questEvents.onStartQuest += StartQuest;
        QuestEventManager.Instance.questEvents.onAdvanceQuest += AdvanceQuest;
        QuestEventManager.Instance.questEvents.onFinishQuest += FinishQuest;
        QuestEventManager.Instance.questEvents.onQuestStepStateChange += QuestStepStateChange;
        QuestEventManager.Instance.onQuestConditionChanged += QuestConditionChangedQuests;
    }

    private void OnDisable()
    {
        if (QuestEventManager.Instance != null)
        {
            QuestEventManager.Instance.questEvents.onStartQuest -= StartQuest;
            QuestEventManager.Instance.questEvents.onAdvanceQuest -= AdvanceQuest;
            QuestEventManager.Instance.questEvents.onFinishQuest -= FinishQuest;
            QuestEventManager.Instance.questEvents.onQuestStepStateChange -= QuestStepStateChange;
            QuestEventManager.Instance.onQuestConditionChanged -= QuestConditionChangedQuests;
        }
    }

    private void Start()
    {
        // 1. 기존 진행 중인 퀘스트 복구
        foreach (Quest quest in questMap.Values)
        {
            if (quest.state == QuestState.IN_PROGRESS)
            {
                quest.InstantiateCurrentQuestStep(this.transform);
            }

            QuestEventManager.Instance.questEvents.QuestStateChange(quest);
        }

        // [추가됨] 게임 시작 시 자동 시작 가능한 퀘스트 체크
        QuestConditionChangedQuests();
    }

    // -----------------------------------------------------------------------
    // 가이드 버튼용: 상태에 따라 시작 또는 완료 처리
    // -----------------------------------------------------------------------
    public void TryStartNextQuest()
    {
        // 1. 다음에 진행해야 할 퀘스트의 인덱스 계산
        int nextIndex = DBPlayerGameData.Instance.completedQuestNumber + 1;

        // 2. DB 범위 확인
        if (nextIndex >= questDatabase.quests.Count)
        {
            Debug.Log("[QuestManager] 모든 퀘스트를 완료했습니다!");
            return;
        }

        // 3. 대상 퀘스트 가져오기
        QuestInfoSO nextQuestInfo = questDatabase.quests[nextIndex];
        if (nextQuestInfo == null) return;

        Quest nextQuest = GetQuestById(nextQuestInfo.ID);

        // ---------------------------------------------------------
        // [핵심 수정] 상태별 분기 처리 강화
        // ---------------------------------------------------------

        // Case A: 이미 모든 스텝을 완수하고 '보상 대기(CAN_FINISH)' 상태인 경우
        if (nextQuest.state == QuestState.CAN_FINISH)
        {
            QuestEventManager.Instance.questEvents.FinishQuest(nextQuest.info.ID);
            Debug.Log($"[QuestManager] 퀘스트 완료 및 보상 수령: {nextQuestInfo.displayName}");
            return;
        }

        // Case B: 이미 진행 중인 경우 (IN_PROGRESS)
        if (nextQuest.state == QuestState.IN_PROGRESS)
        {
            Debug.Log($"[QuestManager] '{nextQuestInfo.displayName}' 퀘스트는 이미 진행 중입니다.");
            return;
        }

        // Case C: 이미 완료된 경우 (FINISHED)
        if (nextQuest.state == QuestState.FINISHED)
        {
            // (이론상 completedQuestNumber + 1 로 가져오므로 여기 걸릴 일은 드뭅니다)
            return;
        }

        // Case D: 시작 가능한 경우 (REQUIREMENTS_NOT_MET 이지만 조건 체크 후 시작)
        if (CheckRequirementsMet(nextQuest))
        {
            QuestEventManager.Instance.questEvents.StartQuest(nextQuest.info.ID);
            Debug.Log($"[QuestManager] 가이드 퀘스트 시작: {nextQuestInfo.displayName}");
        }
        else
        {
            Debug.Log($"[QuestManager] '{nextQuestInfo.displayName}' 시작 불가: 조건이 부족합니다.");
        }
    }

    // -----------------------------------------------------------------------
    // 상태 변경 및 진행 로직
    // -----------------------------------------------------------------------

    private void ChangeQuestState(string id, QuestState state)
    {
        Quest quest = GetQuestById(id);
        quest.state = state;
        QuestEventManager.Instance.questEvents.QuestStateChange(quest);
    }

    private bool CheckRequirementsMet(Quest quest)
    {
        var requirements = quest.info.requirements;
        if (requirements == null || requirements.Count == 0) return true;

        foreach (var req in requirements)
        {
            if (!req.IsMet()) return false;
        }

        return true;
    }

    private void StartQuest(string id)
    {
        Quest quest = GetQuestById(id);
        quest.InstantiateCurrentQuestStep(transform);
        ChangeQuestState(quest.info.ID, QuestState.IN_PROGRESS);
    }

    private void AdvanceQuest(string id)
    {
        Quest quest = GetQuestById(id);

        quest.MoveToNextStep();

        if (quest.CurrentStepExists())
        {
            quest.InstantiateCurrentQuestStep(transform);

            if (questSceneManager != null)
            {
                questSceneManager.StartGuide(id);
            }
        }
        else
        {
            if (quest.info.autoComplete)
            {
                FinishQuest(quest.info.ID);
            }
            else
            {
                ChangeQuestState(quest.info.ID, QuestState.CAN_FINISH);
            }
        }
    }

    private void FinishQuest(string id)
    {
        Quest quest = GetQuestById(id);
        ClaimRewards(quest);
        ChangeQuestState(quest.info.ID, QuestState.FINISHED);

        // [중요] 완료된 퀘스트 번호 갱신
        int currentQuestIndex = questDatabase.quests.IndexOf(quest.info);
        if (currentQuestIndex > DBPlayerGameData.Instance.completedQuestNumber)
        {
            DBPlayerGameData.Instance.completedQuestNumber = currentQuestIndex;
            Debug.Log($"[Progress] 퀘스트 진행도 갱신: {currentQuestIndex}번 완료");
        }
    }

    private void ClaimRewards(Quest quest)
    {
        if (quest.info.rewards == null) return;
        foreach (var reward in quest.info.rewards) reward.GiveReward();
    }

    private void QuestStepStateChange(string id, int stepIndex, QuestStepState questStepState)
    {
        Quest quest = GetQuestById(id);
        quest.StoreQuestStepState(questStepState, stepIndex);
        ChangeQuestState(id, quest.state);
    }

    private void QuestConditionChangedQuests()
    {
        foreach (Quest quest in questMap.Values)
        {
            // 1. 아직 시작 안 된 퀘스트 중에서
            if (quest.state == QuestState.REQUIREMENTS_NOT_MET || quest.state == QuestState.CAN_START)
            {
                // 1. 이 퀘스트가 DB 리스트의 몇 번째인지 확인
                int myIndex = questDatabase.quests.IndexOf(quest.info);
                
                // 2. 지금 플레이어가 깨야 할 순서인지 확인 (완료된 번호 + 1)
                int nextSequenceIndex = DBPlayerGameData.Instance.completedQuestNumber + 1;

                // 3. 만약 내 차례가 아니라면? (아직 앞의 퀘스트를 안 깼거나, 이미 지나갔거나)
                if (myIndex != nextSequenceIndex) 
                {
                    continue; // 무시하고 다음 검사로 넘어감
                }
               
                // 2. AutoStart 옵션이 켜져 있고
                if (quest.info.autoStart)
                {
                    // 3. 조건이 충족되었다면
                    if (CheckRequirementsMet(quest))
                    {
                        QuestEventManager.Instance.questEvents.StartQuest(quest.info.ID);
                        Debug.Log($"[AutoStart] 순서 {myIndex}번 '{quest.info.displayName}' 자동 시작됨.");
                    }
                }
                else
                {
                    // AutoStart가 꺼져있어도 조건이 맞으면 CAN_START로 변경 (가이드 버튼용)
                    if (CheckRequirementsMet(quest) && quest.state != QuestState.CAN_START)
                    {
                        ChangeQuestState(quest.info.ID, QuestState.CAN_START);
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------
    // 초기화 로직 (JSON/PlayerPrefs 제거 -> DB 순서 기반)
    // -----------------------------------------------------------------------
    private Dictionary<string, Quest> CreateQuestMap()
    {
        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();

        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabaseSO가 할당되지 않았습니다!");
            return idToQuestMap;
        }

        // DB를 순회하며 퀘스트 생성 및 상태 설정
        for (int i = 0; i < questDatabase.quests.Count; i++)
        {
            QuestInfoSO questInfo = questDatabase.quests[i];
            if (questInfo == null) continue;

            if (idToQuestMap.ContainsKey(questInfo.ID))
            {
                Debug.LogWarning("중복된 ID 발견: " + questInfo.ID);
                continue;
            }

            // [핵심] 현재 인덱스(i)와 완료된 번호(completedQuestNumber) 비교
            Quest quest = new Quest(questInfo);

            if (i <= DBPlayerGameData.Instance.completedQuestNumber)
            {
                // 이미 완료한 순서라면 -> FINISHED 상태로 설정
                quest.state = QuestState.FINISHED;
            }
            else
            {
                // 아직 오지 않은 순서라면 -> 기본 상태 (REQUIREMENTS_NOT_MET)
                quest.state = QuestState.REQUIREMENTS_NOT_MET;
            }

            idToQuestMap.Add(questInfo.ID, quest);
        }

        // [추가됨] 딕셔너리 생성이 끝나면, 인스펙터용 리스트에도 담아줍니다.
        // 같은 객체를 참조하므로, 게임 중 상태가 변하면 인스펙터에서도 변합니다.
        debugQuestList = idToQuestMap.Values.ToList();

        return idToQuestMap;
    }

    public Quest GetQuestById(string id)
    {
        if (questMap.TryGetValue(id, out Quest quest))
            return quest;

        Debug.LogError("ID를 찾을 수 없음: " + id);
        return null;
    }
    
    public Quest GetQuestBySequenceIndex(int index)
    {
        // 1. DB가 없거나 인덱스가 범위를 벗어나면 null 반환
        if (questDatabase == null || index < 0 || index >= questDatabase.quests.Count)
        {
            return null;
        }

        // 2. 해당 순서의 퀘스트 ID를 가져옴
        string id = questDatabase.quests[index].ID;

        // 3. ID로 실제 진행 중인 퀘스트 객체(Runtime Quest)를 찾아서 반환
        return GetQuestById(id);
    }
}