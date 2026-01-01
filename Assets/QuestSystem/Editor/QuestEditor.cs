using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

public class QuestEditor : EditorWindow
{
    private QuestDatabaseSO questDatabase;
    private QuestSceneManager sceneManager;
    private SerializedObject serializedDatabase;
    private ReorderableList reorderableList;

    private ObjectField databaseField;
    private VisualElement contentContainer;
    private VisualElement warningContainer;
    private ScrollView rightPane;

    [MenuItem("Tools/Quest Guide Editor")]
    public static void ShowWindow()
    {
        QuestEditor wnd = GetWindow<QuestEditor>();
        wnd.titleContent = new GUIContent("Quest Editor");
        wnd.minSize = new Vector2(1000, 600);
    }

    private void OnEnable()
    {
        if (questDatabase == null) questDatabase = FindAssetByType<QuestDatabaseSO>();
    }

    public void CreateGUI()
    {
        // 중복 생성 방지를 위해 루트 클리어
        rootVisualElement.Clear();

        DrawHeader();
        warningContainer = new VisualElement();
        rootVisualElement.Add(warningContainer);
        contentContainer = new VisualElement() { style = { flexGrow = 1 } };
        rootVisualElement.Add(contentContainer);

        RefreshState();
    }

    private void DrawHeader()
    {
        VisualElement header = new VisualElement();
        header.style.ApplyRowStyle();
        header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

        header.Add(new Label("Database:")
            { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 10, alignSelf = Align.Center } });

        databaseField = new ObjectField() { objectType = typeof(QuestDatabaseSO), style = { width = 250 } };
        databaseField.value = questDatabase;
        databaseField.RegisterValueChangedCallback(evt =>
        {
            questDatabase = evt.newValue as QuestDatabaseSO;
            RefreshState();
        });
        header.Add(databaseField);

        header.Add(new VisualElement() { style = { flexGrow = 1 } });
        header.Add(new Button(RefreshState) { text = "Refresh UI", style = { height = 20 } });
        rootVisualElement.Add(header);
    }

    private void RefreshState()
    {
        if (warningContainer == null || contentContainer == null) return;
        warningContainer.Clear();
        contentContainer.Clear();

        if (questDatabase == null) questDatabase = FindAssetByType<QuestDatabaseSO>();
        if (questDatabase == null)
        {
            warningContainer.Add(new HelpBox("QuestDatabaseSO를 찾을 수 없습니다.", HelpBoxMessageType.Warning));
            return;
        }

        SyncAllStepData();
        serializedDatabase = new SerializedObject(questDatabase);
        InitReorderableList();

        sceneManager = Object.FindFirstObjectByType<QuestSceneManager>();
        if (sceneManager == null)
        {
            DrawCreateManagerUI();
            return;
        }

        DrawMainEditor();
    }

    private void DrawCreateManagerUI()
    {
        VisualElement box = new VisualElement() { style = { alignItems = Align.Center, marginTop = 50 } };
        box.Add(new Label("⚠️ QuestSceneManager가 씬에 없습니다."));
        box.Add(new Button(() =>
        {
            new GameObject("QuestSceneManager").AddComponent<QuestSceneManager>();
            RefreshState();
        }) { text = "매니저 생성", style = { width = 200, height = 30, marginTop = 10 } });
        warningContainer.Add(box);
    }

    private void DrawMainEditor()
    {
        TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        contentContainer.Add(splitView);

        IMGUIContainer leftList = new IMGUIContainer(() =>
        {
            if (serializedDatabase == null || serializedDatabase.targetObject == null) return;
            serializedDatabase.Update();
            reorderableList?.DoLayoutList();
            serializedDatabase.ApplyModifiedProperties();
        });
        splitView.Add(leftList);

        rightPane = new ScrollView() { style = { paddingLeft = 10, paddingRight = 10, paddingTop = 10 } };
        splitView.Add(rightPane);

        if (reorderableList != null && reorderableList.index >= 0) RefreshRightPane();
    }

    private void InitReorderableList()
    {
        // 퀘스트 리스트 프로퍼티 가져오기
        SerializedProperty questsProp = serializedDatabase.FindProperty("quests");

        // ReorderableList 초기화 (드래그 가능, 헤더 표시, 추가/삭제 버튼 활성화)
        reorderableList = new ReorderableList(serializedDatabase, questsProp, true, true, true, true);

        // 1. 헤더 그리기
        reorderableList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Quest List"); };

        // 2. 리스트 요소 그리기 (인덱스와 표시 이름 출력)
        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = questsProp.GetArrayElementAtIndex(index);
            var quest = element.objectReferenceValue as QuestInfoSO;
            string label = (quest != null) ? $"{index}: {quest.displayName}" : $"Null Quest ({index})";

            // 텍스트가 너무 길어질 수 있으므로 한 줄로 표시
            EditorGUI.LabelField(rect, label);
        };

        // 3. 요소 선택 시 오른쪽 상세 창 갱신
        reorderableList.onSelectCallback = (list) => RefreshRightPane();

        // 4.순서가 변경되었을 때 실행 (데이터 인덱스 재정렬)
        reorderableList.onReorderCallbackWithDetails = (list, oldIdx, newIdx) =>
        {
            // 리스트 순서가 바뀌면 각 StepSO 내부의 questIndex도 바뀌어야 하므로 전체 동기화 실행
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
            SyncAllStepData();
            RefreshRightPane();
            Repaint();
        };

        // 5. 아이템이 삭제되었을 때 실행 (미사용 데이터 청소)
        reorderableList.onRemoveCallback = (list) =>
        {
            // 실수 방지를 위한 확인 팝업 (선택 사항)
            if (EditorUtility.DisplayDialog("퀘스트 삭제", "리스트에서 삭제하시겠습니까? (연결된 SO 파일은 삭제되지 않습니다)", "삭제", "취소"))
            {
                // 기본 삭제 동작 수행
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                list.serializedProperty.serializedObject.ApplyModifiedProperties();
                // 삭제 후 남은 퀘스트들의 ID를 기준으로 데이터 정리 및 인덱스 갱신
                SyncAllStepData();
                RefreshRightPane();
                Repaint();
            }
        };
    }

    private void RefreshRightPane()
    {
        rightPane.Clear();
        if (reorderableList == null || reorderableList.index < 0 ||
            reorderableList.index >= questDatabase.quests.Count) return;

        QuestInfoSO selected = questDatabase.quests[reorderableList.index];
        if (selected == null) return;

        SerializedObject so = new SerializedObject(selected);

        VisualElement genBox = new VisualElement();
        genBox.style.ApplyBoxStyle(new Color(0.25f, 0.25f, 0.25f));
        genBox.Add(new Label("퀘스트 정보") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });
        genBox.Add(new PropertyField(so.FindProperty("displayName")));
        genBox.Add(new PropertyField(so.FindProperty("autoStart")));
        genBox.Add(new PropertyField(so.FindProperty("autoComplete")));
        genBox.Bind(so);
        rightPane.Add(genBox);

        DrawScriptableObjectList(rightPane, so.FindProperty("requirements"), "Requirements",
            typeof(QuestRequirementDataSO), "Requirements", selected.ID);
        DrawScriptableObjectList(rightPane, so.FindProperty("steps"), "Quest Steps", typeof(QuestStepDataSO), "Steps",
            selected.ID, selected);
        DrawScriptableObjectList(rightPane, so.FindProperty("rewards"), "Rewards", typeof(QuestRewardDataSO), "Rewards",
            selected.ID);
    }

    private void DrawScriptableObjectList(VisualElement parent, SerializedProperty listProp, string title,
        Type baseType, string folder, string questId, QuestInfoSO selectedQuest = null)
    {
        VisualElement container = new VisualElement();
        container.style.ApplyBoxStyle(new Color(0.22f, 0.22f, 0.22f));
        container.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });

        for (int i = 0; i < listProp.arraySize; i++)
        {
            int index = i;
            SerializedProperty element = listProp.GetArrayElementAtIndex(i);
            Object dataSO = element.objectReferenceValue;

            Foldout foldout = new Foldout() { value = false, text = "" };
            VisualElement header = foldout.Q<VisualElement>(className: "unity-foldout__input");

            VisualElement row = new VisualElement();
            row.style.ApplyRowStyle();
            row.style.flexGrow = 1;
            row.style.marginLeft = 15;

            ObjectField objField = new ObjectField()
                { objectType = baseType, value = dataSO, style = { flexGrow = 1 } };
            objField.RegisterValueChangedCallback(evt =>
            {
                element.objectReferenceValue = evt.newValue;
                listProp.serializedObject.ApplyModifiedProperties();
                SyncAllStepData();
                RefreshRightPane();
            });
            row.Add(objField);

            Button delBtn = new Button(() =>
            {
                listProp.DeleteArrayElementAtIndex(index);
                listProp.serializedObject.ApplyModifiedProperties();
                RefreshRightPane();
            });
            delBtn.ApplyRemoveButtonStyle();
            row.Add(delBtn);
            header.Add(row);

            if (dataSO != null)
            {
                VisualElement content = new VisualElement() { style = { marginLeft = 20, marginTop = 5 } };
                DrawSOFields(content, dataSO, questId);
                if (selectedQuest != null && baseType == typeof(QuestStepDataSO))
                    DrawStepUIGuide(content, selectedQuest, index);
                foldout.Add(content);
            }

            container.Add(foldout);
        }

        Button addBtn = new Button(() => ShowAddMenu(listProp, baseType, folder));
        addBtn.ApplyAddButtonStyle();
        container.Add(addBtn);
        parent.Add(container);
    }

    private void DrawSOFields(VisualElement parent, Object dataSO, string questId)
    {
        SerializedObject so = new SerializedObject(dataSO);
        SerializedProperty specificList = so.FindProperty("questSpecificDatas");
        if (specificList == null) return;

        bool found = false;
        for (int j = 0; j < specificList.arraySize; j++)
        {
            SerializedProperty entry = specificList.GetArrayElementAtIndex(j);
            if (entry.FindPropertyRelative("questId").stringValue == questId)
            {
                found = true;
                SerializedProperty iter = entry.Copy();
                SerializedProperty end = entry.GetEndProperty();
                if (iter.NextVisible(true))
                {
                    do
                    {
                        if (SerializedProperty.EqualContents(iter, end)) break;
                        if (iter.name == "questId" || iter.name == "questIndex") continue;
                        PropertyField pf = new PropertyField(iter.Copy());
                        pf.Bind(so);
                        parent.Add(pf);
                    } while (iter.NextVisible(false));
                }

                break;
            }
        }
    }

    private void DrawStepUIGuide(VisualElement parent, QuestInfoSO quest, int stepIdx)
    {
        var seq = GetOrCreateSequence(quest);
        if (stepIdx >= seq.StepSequences.Count) return;

        SerializedObject smSO = new SerializedObject(sceneManager);
        var targetProp = smSO.FindProperty("questSequences")
            .GetArrayElementAtIndex(sceneManager.questSequences.IndexOf(seq))
            .FindPropertyRelative("stepSequences").GetArrayElementAtIndex(stepIdx)
            .FindPropertyRelative("uiTargets");

        VisualElement uiBox = new VisualElement();
        uiBox.style.ApplyBoxStyle(new Color(0.18f, 0.18f, 0.2f));
        PropertyField listField = new PropertyField(targetProp, "핑 찍을 UI 리스트");
        listField.Bind(smSO);
        uiBox.Add(listField);
        parent.Add(uiBox);
    }

    private void ShowAddMenu(SerializedProperty listProp, Type baseType, string subFolder)
    {
        GenericMenu menu = new GenericMenu();
        var types = TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract).OrderBy(t => t.Name);
        foreach (var t in types)
            menu.AddItem(new GUIContent($"Create New/{t.Name}"), false,
                () => CreateAndAddAsset(t, listProp, subFolder));

        // [핵심 수정] 빈 슬롯 추가 시 null로 초기화 및 동기화
        menu.AddItem(new GUIContent("Add Empty Slot"), false, () =>
        {
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = null; // 이전 원소 복제 방지
            listProp.serializedObject.ApplyModifiedProperties();
            SyncAllStepData(); // 추가된 슬롯 상태 반영을 위한 동기화
            RefreshRightPane();
        });
        menu.ShowAsContext();
    }

    private void CreateAndAddAsset(Type type, SerializedProperty listProp, string subFolder)
    {
        string path = AssetDatabase.GetAssetPath(questDatabase);
        string dir = System.IO.Path.GetDirectoryName(path) + "/" + subFolder;
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

        string fullPath =
            AssetDatabase.GenerateUniqueAssetPath(
                $"{dir}/{type.Name}_{Guid.NewGuid().ToString().Substring(0, 4)}.asset");
        ScriptableObject asset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();

        listProp.arraySize++;
        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = asset;
        listProp.serializedObject.ApplyModifiedProperties();

        // 생성 후 즉시 동기화하여 내부 데이터 필드가 바로 보이게 함
        SyncAllStepData();
        RefreshRightPane();
    }

    private QuestSceneManager.QuestSequence GetOrCreateSequence(QuestInfoSO quest)
    {
        var seq = sceneManager.questSequences.FirstOrDefault(s => s.Quest == quest);
        if (seq == null)
        {
            seq = new QuestSceneManager.QuestSequence();
            seq.Init(quest);
            sceneManager.questSequences.Add(seq);
            EditorUtility.SetDirty(sceneManager);
        }

        while (seq.StepSequences.Count < quest.steps.Count)
            seq.StepSequences.Add(new QuestSceneManager.StepGuideSequence());
        return seq;
    }

    private void SyncAllStepData()
    {
        if (questDatabase == null) return;

        HashSet<string> validQuestIds = new HashSet<string>(
            questDatabase.quests.Where(q => q != null).Select(q => q.ID)
        );

        for (int i = 0; i < questDatabase.quests.Count; i++)
        {
            var q = questDatabase.quests[i];
            if (q == null) continue;

            q.requirements.ForEach(r =>
            {
                if (r) r.SyncQuestData(q.ID, i);
            });
            q.steps.ForEach(s =>
            {
                if (s) s.SyncQuestData(q.ID, i);
            });
            q.rewards.ForEach(rw =>
            {
                if (rw) rw.SyncQuestData(q.ID, i);
            });
        }

        CleanUpAllQuestAssets(validQuestIds);

        AssetDatabase.SaveAssets();
    }

    private void CleanUpAllQuestAssets(HashSet<string> validQuestIds)
    {
        // Step, Requirement, Reward 에셋들을 모두 검색
        string[] filter = { "t:QuestStepDataSO", "t:QuestRequirementDataSO", "t:QuestRewardDataSO" };
        var guids = AssetDatabase.FindAssets(string.Join(" ", filter));

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (asset == null) continue;

            // CleanUpUnusedData 메서드가 있는지 확인 후 실행 (Reflection 사용)
            var method = asset.GetType().GetMethod("CleanUpUnusedData");
            if (method != null)
            {
                method.Invoke(asset, new object[] { validQuestIds });
                EditorUtility.SetDirty(asset);
            }
        }
    }

    private static T FindAssetByType<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }
}