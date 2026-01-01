using System;
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
    [SerializeField] private List<Quest> debugQuestList = new();

    private Dictionary<string, Quest> questMap;

    protected override void Awake()
    {
        base.Awake();

        questMap = CreateQuestMap();

        if (questEventManager != null) questEventManager.Init();
        if (questSceneManager != null) questSceneManager.Init();
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
        int currentSeqIndex = DBPlayerGameData.Instance.completedQuestNumber + 1;

        // 1. 인덱스가 DB 범위 내에 있는지 확인
        if (questDatabase != null && currentSeqIndex < questDatabase.quests.Count)
        {
            // 해당 순서의 퀘스트 객체 가져오기
            Quest currentQuest = GetQuestBySequenceIndex(currentSeqIndex);

            if (currentQuest != null)
            {
                // 2. 만약 저장된 상태가 '진행 중(IN_PROGRESS)'이라면 -> 중단된 지점(Step) 복구
                if (currentQuest.state == QuestState.InProgress)
                {
                    currentQuest.InstantiateCurrentQuestStep(transform);

                    // QuestSceneManager에게 "이 퀘스트 진행 중이니 핑 찍어줘"라고 알림
                    QuestEventManager.Instance.questEvents.QuestStateChange(currentQuest);
                }
            }
        }

        // 2. 자동 시작 가능한 퀘스트 체크
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

        if (nextQuest.state == QuestState.CanFinish)
        {
            // 완료 가능 상태면 -> 완료 처리
            QuestEventManager.Instance.questEvents.FinishQuest(nextQuest.info.ID);
        }
        else if (nextQuest.state == QuestState.Locked || nextQuest.state == QuestState.CanStart)
        {
            // 조건이 맞으면 -> 시작 처리
            if (CheckRequirementsMet(nextQuest))
            {
                QuestEventManager.Instance.questEvents.StartQuest(nextQuest.info.ID);
            }
            else
            {
                Debug.Log($"[QuestManager] 조건 부족으로 시작 불가: {nextQuestInfo.displayName}");
            }
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
        // 조건이 없으면 통과
        if (quest.info.requirements == null || quest.info.requirements.Count == 0) return true;

        bool allMet = true;

        // 조건을 검사하기 위한 임시 게임오브젝트 생성
        // (최적화를 위해선 싱글톤에 미리 만들어둔 'CheckRunner' 같은 걸 쓰는 게 좋음)
        GameObject runner = new GameObject($"ReqCheck_{quest.info.ID}");
        runner.transform.SetParent(transform);

        foreach (var reqData in quest.info.requirements)
        {
            // 1. 컴포넌트 추가
            Type type = reqData.GetRequirementType();
            QuestRequirement reqComponent = runner.AddComponent(type) as QuestRequirement;

            // 2. 초기화 및 검사
            if (reqComponent != null)
            {
                reqComponent.Init(reqData, quest.info.ID);
                if (!reqComponent.IsMet())
                {
                    allMet = false;
                    break; // 하나라도 실패하면 끝
                }
            }
        }

        // 검사 끝났으니 파괴
        Destroy(runner);

        return allMet;
    }

    private void StartQuest(string id)
    {
        Quest quest = GetQuestById(id);
        // 상태를 IN_PROGRESS로 변경 -> 이 순간 가이드 버튼 핑은 꺼지고, 타겟 핑이 켜짐
        ChangeQuestState(quest.info.ID, QuestState.InProgress);
        quest.InstantiateCurrentQuestStep(transform);
    }

    private void AdvanceQuest(string id)
    {
        Quest quest = GetQuestById(id);

        quest.MoveToNextStep();

        if (quest.CurrentStepExists())
        {
            quest.InstantiateCurrentQuestStep(transform);

            QuestEventManager.Instance.questEvents.QuestStateChange(quest);
        }
        else
        {
            if (quest.info.autoComplete)
            {
                FinishQuest(quest.info.ID);
            }
            else
            {
                ChangeQuestState(quest.info.ID, QuestState.CanFinish);
            }
        }
    }

    private void FinishQuest(string id)
    {
        Quest quest = GetQuestById(id);
        ClaimRewards(quest);

        // FINISHED로 변경 -> 핑 모두 꺼짐
        ChangeQuestState(quest.info.ID, QuestState.Finished);

        int currentQuestIndex = questDatabase.quests.IndexOf(quest.info);
        if (currentQuestIndex > DBPlayerGameData.Instance.completedQuestNumber)
        {
            DBPlayerGameData.Instance.completedQuestNumber = currentQuestIndex;

            // 퀘스트 하나가 끝났으니, 다음 퀘스트의 자동 시작 여부를 체크
            QuestConditionChangedQuests();
        }
    }

    private void ClaimRewards(Quest quest)
    {
        if (quest.info.rewards == null) return;

        foreach (var rewardData in quest.info.rewards)
        {
            // 1. 보상용 게임오브젝트 생성
            GameObject rewardObj = new GameObject($"Reward_{rewardData.label}");
            rewardObj.transform.SetParent(transform); // 매니저 하위로 정리

            // 2. 컴포넌트 부착
            Type type = rewardData.GetRewardType();
            QuestReward rewardComponent = rewardObj.AddComponent(type) as QuestReward;

            // 3. 초기화 및 지급
            if (rewardComponent != null)
            {
                rewardComponent.Init(rewardData, quest.info.ID);
                rewardComponent.GiveReward();
                // GiveReward 내부에서 연출 후 Destroy(gameObject)를 호출해야 함
            }
        }
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
            if (quest.state == QuestState.Locked || quest.state == QuestState.CanStart)
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
                    if (CheckRequirementsMet(quest) && quest.state != QuestState.CanStart)
                    {
                        ChangeQuestState(quest.info.ID, QuestState.CanStart);
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
                quest.state = QuestState.Finished;
            }
            else
            {
                // 아직 오지 않은 순서라면 -> 기본 상태 (REQUIREMENTS_NOT_MET)
                quest.state = QuestState.Locked;
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