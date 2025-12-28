using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System; 
using System.Linq; 
using System.Collections.Generic;
using Object = UnityEngine.Object; 

public class QuestEditor : EditorWindow
{
    // --- 데이터 참조 ---
    private QuestDatabaseSO questDatabase;
    private QuestSceneManager sceneManager;
    private SerializedObject currentQuestSO;

    // --- UI 요소 ---
    private ObjectField databaseField;
    private VisualElement contentContainer;
    private VisualElement warningContainer;
    private TwoPaneSplitView splitView;
    private ListView leftList;
    private ScrollView rightPane;

    [MenuItem("Tools/Quest Guide Editor")]
    public static void ShowWindow()
    {
        QuestEditor wnd = GetWindow<QuestEditor>();
        wnd.titleContent = new GUIContent("Quest UI Guide");
        wnd.minSize = new Vector2(1000, 600);
    }

    public void CreateGUI()
    {
        DrawHeader();
        warningContainer = new VisualElement();
        rootVisualElement.Add(warningContainer);

        contentContainer = new VisualElement() { style = { flexGrow = 1 } };
        rootVisualElement.Add(contentContainer);

        RefreshState();
    }

    private void DrawHeader()
    {
        VisualElement header = new VisualElement()
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                paddingTop = 8, paddingBottom = 8, paddingLeft = 10, paddingRight = 10,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                borderBottomWidth = 1, borderBottomColor = new Color(0.15f, 0.15f, 0.15f)
            }
        };

        Label label = new Label("Source Database:");
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginRight = 10;
        header.Add(label);

        databaseField = new ObjectField() { objectType = typeof(QuestDatabaseSO), style = { width = 300 } };
        databaseField.RegisterValueChangedCallback(evt => { questDatabase = evt.newValue as QuestDatabaseSO; RefreshState(); });

        if (questDatabase == null)
        {
            questDatabase = FindAssetByType<QuestDatabaseSO>();
            databaseField.value = questDatabase;
        }

        header.Add(databaseField);
        header.Add(new VisualElement() { style = { flexGrow = 1 } }); // Spacer
        header.Add(new Button(RefreshState) { text = "Refresh UI", style = { height = 20 } });

        rootVisualElement.Add(header);
    }

    private void RefreshState()
    {
        warningContainer.Clear();
        contentContainer.Clear();

        if (questDatabase == null)
        {
            warningContainer.Add(new HelpBox("상단의 'Source Database' 필드에 QuestDatabaseSO를 할당해주세요.", HelpBoxMessageType.Warning));
            return;
        }

        sceneManager = FindFirstObjectByType<QuestSceneManager>();
        if (sceneManager == null)
        {
            DrawCreateManagerUI();
            return;
        }

        DrawMainEditor();
    }

    private void DrawCreateManagerUI()
    {
        VisualElement box = new VisualElement() { style = { alignItems = Align.Center, justifyContent = Justify.Center, flexGrow = 1, marginTop = 50 } };
        Label msg = new Label("⚠️ 현재 씬에 'QuestSceneManager'가 없습니다.") { style = { fontSize = 16, marginBottom = 15, unityFontStyleAndWeight = FontStyle.Bold } };
        Button createBtn = new Button(() => {
            GameObject go = new GameObject("QuestSceneManager");
            go.AddComponent<QuestSceneManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create QuestSceneManager");
            RefreshState();
        }) { text = "Create Manager in Scene", style = { height = 40, width = 250, fontSize = 14 } };
        box.Add(msg); box.Add(createBtn);
        warningContainer.Add(box);
    }

    private void DrawMainEditor()
    {
        splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
        contentContainer.Add(splitView);

        // --- 왼쪽: 리스트 ---
        VisualElement leftPane = new VisualElement() { style = { backgroundColor = new Color(0.22f, 0.22f, 0.22f) } };
        splitView.Add(leftPane);

        Label listHeader = new Label($"Quest List ({questDatabase.quests.Count})") { style = { paddingTop = 10, paddingLeft = 10, paddingBottom = 5, unityFontStyleAndWeight = FontStyle.Bold, color = new Color(0.7f, 0.7f, 0.7f) } };
        leftPane.Add(listHeader);

        leftList = new ListView();
        leftList.itemsSource = questDatabase.quests;
        leftList.fixedItemHeight = 30;
        leftList.makeItem = () => new Label() { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 10 } };
        leftList.bindItem = (e, i) => {
            Label label = e as Label;
            var quest = questDatabase.quests[i];
            label.text = (quest != null) ? (string.IsNullOrEmpty(quest.displayName) ? quest.name : quest.displayName) : "<Null Quest>";
            if (quest == null) label.style.color = Color.red;
        };

        leftList.selectionType = SelectionType.Single;
        leftList.onSelectionChange += OnQuestSelected;
        leftList.style.flexGrow = 1;
        leftPane.Add(leftList);

        // --- 오른쪽: 상세 설정 ---
        // [수정] paddingAll 대신 개별 속성 사용
        rightPane = new ScrollView() { 
            style = { 
                paddingTop = 20, paddingBottom = 20, paddingLeft = 20, paddingRight = 20, 
                backgroundColor = new Color(0.28f, 0.28f, 0.28f) 
            } 
        };
        splitView.Add(rightPane);
        rightPane.Add(new Label("Select a Quest from the list to edit.") { style = { opacity = 0.5f, unityTextAlign = TextAnchor.MiddleCenter, marginTop = 50 } });
    }

    private void OnQuestSelected(IEnumerable<object> selectedItems)
    {
        rightPane.Clear();
        foreach (var item in selectedItems)
        {
            QuestInfoSO selectedQuest = item as QuestInfoSO;
            if (selectedQuest == null) continue;
            currentQuestSO = new SerializedObject(selectedQuest);
            DrawQuestDetail(selectedQuest);
        }
    }

    private void DrawQuestDetail(QuestInfoSO selectedQuest)
    {
        DrawTitleSection(selectedQuest);
        DrawQuestConfig();
        
        VisualElement divider = new VisualElement() { style = { height = 2, backgroundColor = new Color(0.15f, 0.15f, 0.15f), marginTop = 20, marginBottom = 20 } };
        rightPane.Add(divider);

        if (sceneManager != null) DrawSceneGuideConfig(selectedQuest);
    }

    private void DrawTitleSection(QuestInfoSO selectedQuest)
    {
        // [수정] paddingAll, borderAll 제거 후 개별 속성 적용
        VisualElement titleBox = new VisualElement() { 
            style = { 
                backgroundColor = new Color(0.2f, 0.2f, 0.2f), 
                borderBottomColor = new Color(0.5f, 0.5f, 0.5f), 
                borderBottomWidth = 2, 
                marginBottom = 15, 
                paddingTop = 10, paddingBottom = 10, paddingLeft = 10, paddingRight = 10,
                borderTopLeftRadius = 5, borderTopRightRadius = 5 
            } 
        };
        string displayTitle = string.IsNullOrEmpty(selectedQuest.displayName) ? selectedQuest.name : selectedQuest.displayName;
        titleBox.Add(new Label(displayTitle) { style = { fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold } });
        titleBox.Add(new Label($"ID: {selectedQuest.id}") { style = { fontSize = 12, color = Color.gray } });
        rightPane.Add(titleBox);
    }

    private void DrawQuestConfig()
    {
        if (currentQuestSO == null) return;
        currentQuestSO.Update();

        Label header = new Label("Quest Configuration (Data)") { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } };
        rightPane.Add(header);

        // [수정] paddingAll, borderAll 제거 후 개별 속성 적용
        VisualElement container = new VisualElement() { 
            style = { 
                backgroundColor = new Color(0.24f, 0.24f, 0.24f), 
                paddingTop = 10, paddingBottom = 10, paddingLeft = 10, paddingRight = 10,
                borderTopLeftRadius = 5, borderTopRightRadius = 5, borderBottomLeftRadius = 5, borderBottomRightRadius = 5,
                borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                borderTopColor = new Color(0.1f, 0.1f, 0.1f), borderBottomColor = new Color(0.1f, 0.1f, 0.1f), borderLeftColor = new Color(0.1f, 0.1f, 0.1f), borderRightColor = new Color(0.1f, 0.1f, 0.1f)
            } 
        };

        // 1. Display Name
        SerializedProperty displayNameProp = currentQuestSO.FindProperty("displayName");
        if (displayNameProp != null) container.Add(new PropertyField(displayNameProp, "Display Name"));

        // 2. Requirements (커스텀 리스트)
        SerializedProperty reqProp = currentQuestSO.FindProperty("requirements");
        if (reqProp != null)
        {
            DrawPolymorphicList(container, reqProp, "Requirements (Conditions)", typeof(QuestRequirement));
        }

        // 3. Steps
        SerializedProperty stepsProp = currentQuestSO.FindProperty("questStepPrefabs");
        if (stepsProp != null)
        {
            var stepsField = new PropertyField(stepsProp, "Quest Steps (Prefabs)");
            stepsField.style.marginTop = 10;
            container.Add(stepsField);
        }

        // 4. Rewards (커스텀 리스트)
        SerializedProperty rewardsProp = currentQuestSO.FindProperty("rewards");
        if (rewardsProp != null)
        {
            DrawPolymorphicList(container, rewardsProp, "Rewards", typeof(QuestReward));
        }

        container.Bind(currentQuestSO);
        
        container.RegisterCallback<ChangeEvent<string>>(_ => { currentQuestSO.ApplyModifiedProperties(); leftList?.Rebuild(); });
        container.RegisterCallback<ChangeEvent<int>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<float>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<bool>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<Object>>(_ => currentQuestSO.ApplyModifiedProperties());

        rightPane.Add(container);
    }

    // ---------------------------------------------------------
    // [SerializeReference] 리스트를 위한 커스텀 그리기 함수 (팝업 포함)
    // ---------------------------------------------------------
    private void DrawPolymorphicList(VisualElement parent, SerializedProperty listProperty, string title, Type baseType)
    {
        // 1. 박스 스타일
        VisualElement root = new VisualElement();
        root.style.marginTop = 10;
        root.style.borderTopWidth = 1; root.style.borderBottomWidth = 1; root.style.borderLeftWidth = 1; root.style.borderRightWidth = 1;
        root.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f); root.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f); root.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f); root.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
        root.style.borderTopLeftRadius = 5; root.style.borderTopRightRadius = 5; root.style.borderBottomLeftRadius = 5; root.style.borderBottomRightRadius = 5;
        root.style.paddingTop = 5; root.style.paddingBottom = 5; root.style.paddingLeft = 5; root.style.paddingRight = 5;
        root.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

        // 2. 헤더
        Label label = new Label(title);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 5;
        root.Add(label);

        // 3. 아이템 목록
        VisualElement itemsContainer = new VisualElement();
        
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            int index = i; 
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(index);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 2;
            row.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            row.style.paddingTop = 3; row.style.paddingBottom = 3; row.style.paddingLeft = 3; row.style.paddingRight = 3;
            row.style.borderTopLeftRadius = 3; row.style.borderTopRightRadius = 3; row.style.borderBottomLeftRadius = 3; row.style.borderBottomRightRadius = 3;

            // 내용 필드 (라벨 없이 필드만 표시하기 위해 "" 전달)
            PropertyField field = new PropertyField(elementProp, ""); 
            field.style.flexGrow = 1;
            field.style.marginRight = 5;
            row.Add(field);

            // 삭제 버튼
            Button removeBtn = new Button(() => 
            {
                listProperty.DeleteArrayElementAtIndex(index);
                listProperty.serializedObject.ApplyModifiedProperties();
                
                // [수정됨] 단순히 DrawQuestConfig만 부르는 게 아니라, 패널을 비우고 전체를 다시 그립니다.
                RefreshRightPane(); 
            }) 
            { text = "X", style = { width = 25, backgroundColor = new Color(0.8f, 0.3f, 0.3f) } };
            
            row.Add(removeBtn);
            itemsContainer.Add(row);
        }
        root.Add(itemsContainer);

        // 4. 추가 버튼
        Button addBtn = new Button(() => 
        {
            GenericMenu menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () => 
                {
                    listProperty.arraySize++;
                    var lastElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                    lastElement.managedReferenceValue = Activator.CreateInstance(type);
                    listProperty.serializedObject.ApplyModifiedProperties();
                    
                    // [수정됨] 패널을 비우고 전체를 다시 그립니다.
                    RefreshRightPane();
                });
            }
            
            if (!types.Any()) menu.AddDisabledItem(new GUIContent("No classes found inheriting " + baseType.Name));

            menu.ShowAsContext();
        }) 
        { text = "+ Add Item", style = { marginTop = 5, height = 25 } };

        root.Add(addBtn);
        parent.Add(root);
    }

    // [추가됨] 오른쪽 패널 전체 갱신 헬퍼 함수
    private void RefreshRightPane()
    {
        if (currentQuestSO == null || currentQuestSO.targetObject == null) return;
        
        // 1. 기존 내용을 모두 지웁니다.
        rightPane.Clear();
        
        // 2. 처음부터 깔끔하게 다시 그립니다.
        DrawQuestDetail((QuestInfoSO)currentQuestSO.targetObject);
    }

    private void DrawSceneGuideConfig(QuestInfoSO selectedQuest)
    {
        Label header = new Label("UI Guide Sequence (Scene)") { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } };
        rightPane.Add(header);

        var sequenceData = GetOrCreateSequence(selectedQuest);
        SerializedObject serializedManager = new SerializedObject(sceneManager);
        SerializedProperty listProp = serializedManager.FindProperty("questSequences");
        int index = sceneManager.questSequences.IndexOf(sequenceData);

        if (index >= 0)
        {
            SerializedProperty uiStepsProp = listProp.GetArrayElementAtIndex(index).FindPropertyRelative("uiSteps");
            rightPane.Add(new HelpBox("Hierarchy에 있는 UI 오브젝트(Button 등)를 아래 리스트로 드래그하세요.\n순서대로 가이드 핑이 찍힙니다.", HelpBoxMessageType.Info) { style = { marginBottom = 10 } });
            
            // [수정] paddingAll 제거
            VisualElement listContainer = new VisualElement() { 
                style = { 
                    backgroundColor = new Color(0.24f, 0.24f, 0.24f), 
                    paddingTop = 10, paddingBottom = 10, paddingLeft = 10, paddingRight = 10,
                    borderTopLeftRadius = 5, borderTopRightRadius = 5, borderBottomLeftRadius = 5, borderBottomRightRadius = 5
                } 
            };
            PropertyField listField = new PropertyField(uiStepsProp, "Sequence List");
            listField.Bind(serializedManager);
            listContainer.Add(listField);
            rightPane.Add(listContainer);
        }
    }

    private QuestSceneManager.QuestSequence GetOrCreateSequence(QuestInfoSO quest)
    {
        foreach (var seq in sceneManager.questSequences) { if (seq.quest == quest) return seq; }
        var newSeq = new QuestSceneManager.QuestSequence(); newSeq.quest = quest; newSeq.uiSteps = new List<RectTransform>();
        sceneManager.questSequences.Add(newSeq); EditorUtility.SetDirty(sceneManager); return newSeq;
    }

    private T FindAssetByType<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }
}