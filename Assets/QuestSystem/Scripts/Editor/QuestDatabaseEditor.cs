using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(QuestDatabaseSO))]
public class QuestDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. 기본 인스펙터 그리기 (리스트와 폴더 필드 표시)
        base.OnInspectorGUI();

        QuestDatabaseSO database = (QuestDatabaseSO)target;

        EditorGUILayout.Space(); // 여백 추가

        // 2. 버튼 생성
        if (GUILayout.Button("Load Quests from Folder", GUILayout.Height(40)))
        {
            LoadQuestsFromFolder(database);
        }
    }

    private void LoadQuestsFromFolder(QuestDatabaseSO database)
    {
#if UNITY_EDITOR
        // 1. 폴더가 할당되었는지 확인
        if (database.QuestFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "Quest Folder가 할당되지 않았습니다.\n폴더를 먼저 할당해주세요.", "OK");
            return;
        }

        // 2. 폴더 경로 가져오기
        string path = AssetDatabase.GetAssetPath(database.QuestFolder);

        // 선택된 오브젝트가 실제 폴더인지 확인
        if (!AssetDatabase.IsValidFolder(path))
        {
            EditorUtility.DisplayDialog("Error", "할당된 오브젝트가 폴더가 아닙니다.", "OK");
            return;
        }

        // 3. 실행 취소(Undo) 기록 (이게 있어야 실수로 눌렀을 때 Ctrl+Z 가능)
        Undo.RecordObject(database, "Load Quests");

        // 4. 해당 경로에서 QuestInfoSO 타입의 모든 에셋 검색
        // "t:QuestInfoSO"는 타입 필터링입니다.
        string[] guids = AssetDatabase.FindAssets("t:QuestInfoSO", new[] { path });

        List<QuestInfoSO> foundQuests = new List<QuestInfoSO>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            QuestInfoSO quest = AssetDatabase.LoadAssetAtPath<QuestInfoSO>(assetPath);
            
            if (quest != null)
            {
                foundQuests.Add(quest);
            }
        }

        // 5. 이름순 정렬 (파일 이름 기준) - 선택 사항이지만 있으면 편함
        foundQuests = foundQuests.OrderBy(q => q.name).ToList();

        // 6. 리스트 교체
        database.allQuests = foundQuests;

        // 7. 변경 사항 저장 표시 (Dirty flag)
        EditorUtility.SetDirty(database);
        
        Debug.Log($"총 {foundQuests.Count}개의 퀘스트를 로드했습니다.");
#endif
    }
}