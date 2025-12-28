using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private QuestEventManager questEventManager; 
    [SerializeField] private QuestSceneManager questSceneManager;
    
    [Header("Data Source")]
    // [변경점 1] QuestDatabaseSO를 인스펙터에서 할당받도록 변수 추가
    [SerializeField] private QuestDatabaseSO questDatabase; 

    [Header("Config")]
    [SerializeField] private bool loadQuestState = true;

    private Dictionary<string, Quest> questMap;

    private void Awake()
    {
        if (questEventManager != null)
            questEventManager.Init();
        else
            Debug.LogError("QuestEventManager is missing in QuestManager!");

        if (questSceneManager != null)
            questSceneManager.Init();
        else
            Debug.LogError("QuestSceneManager is missing in QuestManager!");
        
        questMap = CreateQuestMap();
    }

    private void OnEnable()
    {
        QuestEventManager.instance.questEvents.onStartQuest += StartQuest;
        QuestEventManager.instance.questEvents.onAdvanceQuest += AdvanceQuest;
        QuestEventManager.instance.questEvents.onFinishQuest += FinishQuest;
        QuestEventManager.instance.questEvents.onQuestStepStateChange += QuestStepStateChange;
    }

    private void OnDisable()
    {
        if (QuestEventManager.instance != null)
        {
            QuestEventManager.instance.questEvents.onStartQuest -= StartQuest;
            QuestEventManager.instance.questEvents.onAdvanceQuest -= AdvanceQuest;
            QuestEventManager.instance.questEvents.onFinishQuest -= FinishQuest;
            QuestEventManager.instance.questEvents.onQuestStepStateChange -= QuestStepStateChange;
        }
    }

    private void Start()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.state == QuestState.IN_PROGRESS)
            {
                quest.InstantiateCurrentQuestStep(this.transform);
            }
            QuestEventManager.instance.questEvents.QuestStateChange(quest);
        }
    }

    private void ChangeQuestState(string id, QuestState state)
    {
        Quest quest = GetQuestById(id);
        quest.state = state;
        QuestEventManager.instance.questEvents.QuestStateChange(quest);
    }

    private bool CheckRequirementsMet(Quest quest)
    {
        // 1. 등록된 요구조건 리스트 가져오기
        var requirements = quest.info.requirements;

        // 요구조건이 하나도 없으면 바로 통과
        if (requirements == null || requirements.Count == 0) return true;

        // 2. 하나라도 만족하지 못하면 false 리턴
        foreach (var req in requirements)
        {
            if (!req.IsMet())
            {
                return false; 
            }
        }

        // 3. 모두 통과하면 true
        return true;
    }

    private void Update()
    {
        foreach (Quest quest in questMap.Values)
        {
            if (quest.state == QuestState.REQUIREMENTS_NOT_MET && CheckRequirementsMet(quest))
            {
                ChangeQuestState(quest.info.id, QuestState.CAN_START);
            }
        }
    }

    private void StartQuest(string id)
    {
        Quest quest = GetQuestById(id);
        quest.InstantiateCurrentQuestStep(this.transform);
        ChangeQuestState(quest.info.id, QuestState.IN_PROGRESS);
    }

    private void AdvanceQuest(string id)
    {
        Quest quest = GetQuestById(id);
        quest.MoveToNextStep();

        if (quest.CurrentStepExists())
        {
            quest.InstantiateCurrentQuestStep(this.transform);
        }
        else
        {
            ChangeQuestState(quest.info.id, QuestState.CAN_FINISH);
        }
    }

    private void FinishQuest(string id)
    {
        Quest quest = GetQuestById(id);
        
        // 1. 여기서 보상 지급 로직 호출
        ClaimRewards(quest);
        
        ChangeQuestState(quest.info.id, QuestState.FINISHED);
    }

    private void ClaimRewards(Quest quest)
    {
        // QuestInfoSO에 있는 rewards 리스트 가져오기
        List<QuestReward> rewards = quest.info.rewards;

        if (rewards == null || rewards.Count == 0) return;

        Debug.Log($"[Quest Manager] Claiming rewards for quest: {quest.info.id}");

        // 2. 리스트를 순회하며 각 보상의 GiveReward() 실행
        foreach (QuestReward reward in rewards)
        {
            // 각 보상 클래스(GoldReward, ItemReward 등)에 구현된 내용이 실행됨
            reward.GiveReward(); 
        }
    }

    private void QuestStepStateChange(string id, int stepIndex, QuestStepState questStepState)
    {
        Quest quest = GetQuestById(id);
        quest.StoreQuestStepState(questStepState, stepIndex);
        ChangeQuestState(id, quest.state);
    }

    private Dictionary<string, Quest> CreateQuestMap()
    {
        // [변경점 2] Resources.LoadAll 삭제 및 DatabaseSO 사용
        
        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();

        // 데이터베이스가 연결되어 있는지 확인
        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabaseSO is not assigned in QuestManager Inspector!");
            return idToQuestMap; // 빈 딕셔너리 반환
        }

        // DB에 있는 리스트를 순회
        foreach (QuestInfoSO questInfo in questDatabase.quests)
        {
            // 리스트 내에 비어있는 항목이 있을 수 있으므로 체크
            if (questInfo == null) continue;

            if (idToQuestMap.ContainsKey(questInfo.id))
            {
                Debug.LogWarning("Duplicate ID found when creating quest map: " + questInfo.id);
            }
            else
            {
                idToQuestMap.Add(questInfo.id, LoadQuest(questInfo));
            }
        }

        return idToQuestMap;
    }

    private Quest GetQuestById(string id)
    {
        Quest quest = questMap[id];
        if (quest == null)
        {
            Debug.LogError("ID not found in the Quest Map: " + id);
        }
        return quest;
    }

    private void OnApplicationQuit()
    {
        foreach (Quest quest in questMap.Values)
        {
            SaveQuest(quest);
        }
    }

    private void SaveQuest(Quest quest)
    {
        try
        {
            QuestData questData = quest.GetQuestData();
            string serializedData = JsonUtility.ToJson(questData);
            PlayerPrefs.SetString(quest.info.id, serializedData);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save quest with id " + quest.info.id + ": " + e);
        }
    }

    private Quest LoadQuest(QuestInfoSO questInfo)
    {
        Quest quest = null;
        try
        {
            if (PlayerPrefs.HasKey(questInfo.id) && loadQuestState)
            {
                string serializedData = PlayerPrefs.GetString(questInfo.id);
                QuestData questData = JsonUtility.FromJson<QuestData>(serializedData);
                quest = new Quest(questInfo, questData.state, questData.questStepIndex, questData.questStepStates);
            }
            else
            {
                quest = new Quest(questInfo);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load quest with id " + quest.info.id + ": " + e);
        }

        return quest;
    }
}