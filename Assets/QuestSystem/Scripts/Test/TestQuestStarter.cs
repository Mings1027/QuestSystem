using UnityEngine;

public class TestQuestStarter : MonoBehaviour
{
    [Header("시작할 퀘스트 ID 입력")]
    public string questIdToTest; // 예: "Quest_AttackUp"

    [ContextMenu("Start Quest Now")] // 컴포넌트 우클릭 메뉴 추가
    public void TestStart()
    {
        Debug.Log($"[Test] 퀘스트 시작 요청: {questIdToTest}");
        
        // 강제로 퀘스트 시작 이벤트를 발생시킵니다.
        QuestEventManager.instance.questEvents.StartQuest(questIdToTest);
    }
}