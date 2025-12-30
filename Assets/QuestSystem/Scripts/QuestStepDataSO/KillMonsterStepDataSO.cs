using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "Step_Monster_Shared", menuName = "Quest/Step Data/Kill Monster (Shared)")]
public class KillMonsterStepDataSO : QuestStepDataSO
{
    [Serializable]
    public class MonsterStepInfo
    {
        [HideInInspector] public string questId; // 데이터 식별용 (변경불가)
        
        public int questIndex; // 에디터 표시 및 정렬용
        
        [Tooltip("목표 처치 수")]
        public int killCount;

        public MonsterStepInfo(string id, int index, int count)
        {
            questId = id;
            questIndex = index;
            killCount = count;
        }
    }

    public string monsterID = "Slime";

    [Header("퀘스트별 목표 설정")]
    public List<MonsterStepInfo> questSpecificDatas = new();

    public override Type GetQuestStepType() => typeof(KillMonsterQuestStep);

    // 퀘스트 ID로 목표치 찾기
    public int GetKillCountForQuest(string questId)
    {
        // 리스트에서 ID가 일치하는 항목 검색
        var info = questSpecificDatas.Find(x => x.questId == questId);
        if (info != null) return info.killCount;
        
        return 1; // 기본값
    }

    // [에디터용] 데이터 동기화 및 정렬 로직
    public override void SyncQuestData(string questId, int questIndex)
    {
        // 1. 이미 있는 데이터인지 확인
        var info = questSpecificDatas.Find(x => x.questId == questId);

        if (info != null)
        {
            // 2-A. 이미 있다면 인덱스만 갱신 (순서가 바뀌었을 수 있으므로)
            info.questIndex = questIndex;
        }
        else
        {
            // 2-B. 없다면 새로 추가 (기본값 1마리)
            questSpecificDatas.Add(new MonsterStepInfo(questId, questIndex, 1));
        }

        // 3. 퀘스트 순서(displayIndex) 기준으로 오름차순 정렬
        // 이렇게 하면 가이드 퀘스트 0, 2, 5번 순서대로 리스트가 예쁘게 정리됨
        questSpecificDatas.Sort((a, b) => a.questIndex.CompareTo(b.questIndex));
    }
}
