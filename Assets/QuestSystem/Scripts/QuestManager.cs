using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private QuestDatabaseSO questDatabase; // 여기에 DB 연결

    private Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();
    private int currentGuideQuestIndex = 0;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // DB가 연결되어 있는지 확인
        if (questDatabase == null)
        {
            Debug.LogError("Quest Database is not assigned in QuestManager!");
            return;
        }

        // 저장된 데이터 로드 (여기선 예시로 인덱스 0부터 시작)
        // currentGuideQuestIndex = PlayerPrefs.GetInt("GuideIndex", 0);
        LoadGuideQuest(currentGuideQuestIndex);
    }

    private void Update()
    {
        foreach (var quest in activeQuests.Values)
        {
            quest.UpdateCurrentStep();
        }
    }

    public void LoadGuideQuest(int index)
    {
        if (questDatabase == null || index >= questDatabase.allQuests.Count) return;

        QuestInfoSO so = questDatabase.allQuests[index];

        if (activeQuests.ContainsKey(so.id)) return;

        Quest newQuest = new Quest(so);
        activeQuests.Add(so.id, newQuest);

        if (so.autoStart)
        {
            StartQuest(so.id);
        }
    }

    public void StartQuest(string id)
    {
        if (activeQuests.TryGetValue(id, out Quest quest))
        {
            quest.State = QuestState.IN_PROGRESS;
            quest.StartCurrentStep();
            Debug.Log($"퀘스트 시작: {quest.Info.title}");
        }
    }

    public void AdvanceQuest(string id)
    {
        if (activeQuests.TryGetValue(id, out Quest quest))
        {
            quest.AdvanceStep();

            if (quest.State == QuestState.CAN_FINISH)
            {
                if (quest.Info.autoComplete)
                {
                    FinishQuest(id);
                }
                else
                {
                    Debug.Log("퀘스트 완료 가능 상태. 보상을 받으세요.");
                    // UI 갱신 알림 등
                }
            }
        }
    }

    public void FinishQuest(string id)
    {
        if (activeQuests.TryGetValue(id, out Quest quest))
        {
            quest.State = QuestState.FINISHED;
            Debug.Log($"퀘스트 완료: {quest.Info.title}");

            activeQuests.Remove(id);

            // 다음 가이드 퀘스트 로드
            currentGuideQuestIndex++;
            LoadGuideQuest(currentGuideQuestIndex);
        }
    }

    public Quest GetQuestById(string id)
    {
        if (activeQuests.ContainsKey(id)) return activeQuests[id];
        return null;
    }
}