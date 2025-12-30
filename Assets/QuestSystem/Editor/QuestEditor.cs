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
    private SerializedObject currentQuestSO;
    private SerializedObject serializedDatabase;
    private ReorderableList reorderableList;

    private ObjectField databaseField;
    private VisualElement contentContainer;
    private VisualElement warningContainer;
    private TwoPaneSplitView splitView;
    private IMGUIContainer leftListContainer;
    private ScrollView rightPane;

    [MenuItem("Tools/Quest Guide Editor")]
    public static void ShowWindow()
    {
        QuestEditor wnd = GetWindow<QuestEditor>();
        wnd.titleContent = new GUIContent("Quest UI Guide");
        wnd.minSize = new Vector2(1000, 600);
    }

    private void OnEnable()
    {
        if (questDatabase == null) questDatabase = FindAssetByType<QuestDatabaseSO>();
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
        VisualElement header = new VisualElement();
        header.style.ApplyRowStyle();
        header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        header.style.paddingLeft = 10;
        header.style.paddingRight = 10;
        header.style.paddingTop = 8;
        header.style.paddingBottom = 8;

        Label label = new Label("Source Database:")
            { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 10 } };
        header.Add(label);

        databaseField = new ObjectField() { objectType = typeof(QuestDatabaseSO), style = { width = 300 } };
        databaseField.RegisterValueChangedCallback(evt =>
        {
            questDatabase = evt.newValue as QuestDatabaseSO;
            RefreshState();
        });

        if (questDatabase == null) questDatabase = FindAssetByType<QuestDatabaseSO>();
        if (questDatabase != null) databaseField.value = questDatabase;

        header.Add(databaseField);
        header.Add(new VisualElement() { style = { flexGrow = 1 } });
        header.Add(new Button(RefreshState) { text = "Refresh UI", style = { height = 20 } });
        rootVisualElement.Add(header);
    }

    private void RefreshState()
    {
        warningContainer.Clear();
        contentContainer.Clear();

        if (questDatabase == null)
        {
            questDatabase = FindAssetByType<QuestDatabaseSO>();
            if (questDatabase != null)
            {
                databaseField.value = questDatabase;
                RefreshState();
                return;
            }

            warningContainer.Add(new HelpBox("QuestDatabaseSO not found.", HelpBoxMessageType.Warning));
            return;
        }

        SyncAllStepData();

        serializedDatabase = new SerializedObject(questDatabase);
        InitReorderableList();

        sceneManager = FindFirstObjectByType<QuestSceneManager>();
        if (sceneManager == null)
        {
            DrawCreateManagerUI();
            return;
        }

        DrawMainEditor();
    }

    private void SyncAllStepData()
    {
        if (questDatabase == null) return;

        for (int i = 0; i < questDatabase.quests.Count; i++)
        {
            QuestInfoSO quest = questDatabase.quests[i];
            if (quest == null) continue;

            // 1. Requirements 동기화
            if (quest.requirements != null)
            {
                foreach (var reqSO in quest.requirements)
                {
                    if (reqSO != null)
                    {
                        reqSO.SyncQuestData(quest.ID, i);
                        EditorUtility.SetDirty(reqSO);
                    }
                }
            }

            // 2. Steps 동기화
            if (quest.steps != null)
            {
                foreach (var stepSO in quest.steps)
                {
                    if (stepSO != null)
                    {
                        stepSO.SyncQuestData(quest.ID, i);
                        EditorUtility.SetDirty(stepSO);
                    }
                }
            }

            // 3. Rewards 동기화
            if (quest.rewards != null)
            {
                foreach (var rewardSO in quest.rewards)
                {
                    if (rewardSO != null)
                    {
                        rewardSO.SyncQuestData(quest.ID, i);
                        EditorUtility.SetDirty(rewardSO);
                    }
                }
            }
        }
    }

    private void InitReorderableList()
    {
        SerializedProperty questsProp = serializedDatabase.FindProperty("quests");
        reorderableList = new ReorderableList(serializedDatabase, questsProp, true, true, true, true)
        {
            drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, $"Quest List ({questsProp.arraySize})"); }
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index >= questDatabase.quests.Count) return;
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            QuestInfoSO quest = element.objectReferenceValue as QuestInfoSO;

            rect.y += 2;
            string nameLabel = (quest != null)
                ? (string.IsNullOrEmpty(quest.displayName) ? quest.name : quest.displayName)
                : "Null Quest";

            bool isCompleted = false;
            if (DBPlayerGameData.Instance != null)
                isCompleted = index <= DBPlayerGameData.Instance.completedQuestNumber;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            if (isCompleted) labelStyle.normal.textColor = Color.green;
            else if (quest == null) labelStyle.normal.textColor = Color.red;

            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                $"{index}. {nameLabel}", labelStyle);
        };

        reorderableList.onSelectCallback = (list) => RefreshRightPane();
        reorderableList.onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
        {
            serializedDatabase.ApplyModifiedProperties();
            RefreshRightPane();
        };
    }

    private void DrawCreateManagerUI()
    {
        VisualElement box = new VisualElement()
            { style = { alignItems = Align.Center, justifyContent = Justify.Center, flexGrow = 1, marginTop = 50 } };
        box.Add(new Label("⚠️ QuestSceneManager missing.")
            { style = { fontSize = 16, marginBottom = 15, unityFontStyleAndWeight = FontStyle.Bold } });

        Button createBtn = new Button(() =>
        {
            GameObject go = new GameObject("QuestSceneManager");
            go.AddComponent<QuestSceneManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create QuestSceneManager");
            RefreshState();
        }) { text = "Create Manager", style = { height = 40, width = 250, fontSize = 14 } };

        box.Add(createBtn);
        warningContainer.Add(box);
    }

    private void DrawMainEditor()
    {
        splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
        contentContainer.Add(splitView);

        VisualElement leftPane = new VisualElement() { style = { backgroundColor = new Color(0.22f, 0.22f, 0.22f) } };
        splitView.Add(leftPane);

        leftListContainer = new IMGUIContainer(() =>
        {
            if (reorderableList != null)
            {
                serializedDatabase.Update();
                reorderableList.DoLayoutList();
                serializedDatabase.ApplyModifiedProperties();
            }
        }) { style = { flexGrow = 1 } };
        leftPane.Add(leftListContainer);

        rightPane = new ScrollView()
        {
            style =
            {
                paddingLeft = 20, paddingRight = 20, paddingTop = 20, paddingBottom = 20,
                backgroundColor = new Color(0.28f, 0.28f, 0.28f)
            }
        };
        splitView.Add(rightPane);
        rightPane.Add(new Label("Select a Quest to edit.")
            { style = { opacity = 0.5f, unityTextAlign = TextAnchor.MiddleCenter, marginTop = 50 } });
    }

    private void RefreshRightPane()
    {
        rightPane.Clear();
        if (reorderableList == null || reorderableList.index < 0 ||
            reorderableList.index >= questDatabase.quests.Count) return;

        SyncAllStepData();

        QuestInfoSO selectedQuest = questDatabase.quests[reorderableList.index];
        if (selectedQuest == null) return;

        currentQuestSO = new SerializedObject(selectedQuest);
        DrawQuestDetail(selectedQuest);
    }

    private void DrawQuestDetail(QuestInfoSO selectedQuest)
    {
        DrawTitleSection(selectedQuest);
        DrawQuestConfig();

        VisualElement divider = new VisualElement()
        {
            style = { height = 2, backgroundColor = new Color(0.15f, 0.15f, 0.15f), marginTop = 20, marginBottom = 20 }
        };
        rightPane.Add(divider);

        if (sceneManager != null) DrawSceneGuideConfig(selectedQuest);
    }

    private void DrawTitleSection(QuestInfoSO selectedQuest)
    {
        VisualElement titleBox = new VisualElement();
        titleBox.style.ApplyBoxStyle(new Color(0.2f, 0.2f, 0.2f));
        titleBox.style.borderBottomWidth = 2;
        titleBox.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
        titleBox.style.marginBottom = 15;

        int questIndex = questDatabase.quests.IndexOf(selectedQuest);
        VisualElement row = new VisualElement()
            { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

        string displayTitle = string.IsNullOrEmpty(selectedQuest.displayName)
            ? selectedQuest.name
            : selectedQuest.displayName;
        row.Add(new Label(displayTitle)
            { style = { fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });

        if (questIndex != -1 && DBPlayerGameData.Instance != null &&
            questIndex <= DBPlayerGameData.Instance.completedQuestNumber)
        {
            row.Add(new Label("COMPLETED")
            {
                style =
                {
                    fontSize = 12, color = Color.white, backgroundColor = new Color(0.2f, 0.6f, 0.2f),
                    paddingTop = 2, paddingBottom = 2, paddingLeft = 6, paddingRight = 6,
                    borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });
        }

        titleBox.Add(row);

        string orderText = questIndex != -1 ? $"Order: {questIndex}" : "Order: Not in DB";
        titleBox.Add(new Label($"ID: {selectedQuest.ID} | {orderText}")
            { style = { fontSize = 12, color = Color.gray, marginTop = 2 } });
        rightPane.Add(titleBox);
    }

    private void DrawQuestConfig()
    {
        if (currentQuestSO == null) return;
        currentQuestSO.Update();

        rightPane.Add(new Label("Quest Configuration (Data)")
            { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } });

        VisualElement container = new VisualElement();
        container.style.ApplyBoxStyle(new Color(0.24f, 0.24f, 0.24f));

        SerializedProperty displayNameProp = currentQuestSO.FindProperty("displayName");
        if (displayNameProp != null) container.Add(new PropertyField(displayNameProp, "Display Name"));

        SerializedProperty autoStartProp = currentQuestSO.FindProperty("autoStart");
        SerializedProperty autoCompleteProp = currentQuestSO.FindProperty("autoComplete");

        if (autoStartProp != null) container.Add(new PropertyField(autoStartProp, "Auto Start"));
        if (autoCompleteProp != null) container.Add(new PropertyField(autoCompleteProp, "Auto Complete"));

        string currentQuestID = currentQuestSO.FindProperty("id").stringValue;

        SerializedProperty reqProp = currentQuestSO.FindProperty("requirements");
        if (reqProp != null)
            DrawScriptableObjectList(container, reqProp, "Requirements", typeof(QuestRequirementDataSO), "Requirements",
                currentQuestID);

        SerializedProperty stepsProp = currentQuestSO.FindProperty("steps");
        if (stepsProp != null)
            DrawScriptableObjectList(container, stepsProp, "Quest Steps", typeof(QuestStepDataSO), "Steps",
                currentQuestID);

        SerializedProperty rewardsProp = currentQuestSO.FindProperty("rewards");
        if (rewardsProp != null)
            DrawScriptableObjectList(container, rewardsProp, "Rewards", typeof(QuestRewardDataSO), "Rewards",
                currentQuestID);

        container.Bind(currentQuestSO);

        // 변경 감지 시 저장
        container.RegisterCallback<ChangeEvent<string>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<int>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<float>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<bool>>(_ => currentQuestSO.ApplyModifiedProperties());
        container.RegisterCallback<ChangeEvent<Object>>(_ => currentQuestSO.ApplyModifiedProperties());

        rightPane.Add(container);
    }

    // [매개변수 추가] string questId 받도록 수정
    private void DrawScriptableObjectList(VisualElement parent, SerializedProperty listProperty, string title,
        Type baseType, string subFolderName, string questId)
    {
        VisualElement root = new VisualElement();
        root.style.ApplyBoxStyle(new Color(0.2f, 0.2f, 0.2f));
        root.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });

        VisualElement itemsContainer = new VisualElement();

        for (int i = 0; i < listProperty.arraySize; i++)
        {
            int index = i;
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(index);
            Object dataSO = elementProp.objectReferenceValue;

            VisualElement row = new VisualElement();
            row.style.ApplyRowStyle();

            // 1. SO 파일 연결 필드
            ObjectField objectField = new ObjectField()
                { objectType = baseType, value = dataSO, style = { flexGrow = 1 } };
            objectField.RegisterValueChangedCallback(evt =>
            {
                listProperty.GetArrayElementAtIndex(index).objectReferenceValue = evt.newValue;
                listProperty.serializedObject.ApplyModifiedProperties();
                RefreshRightPane();
            });
            row.Add(objectField);

            // 2. 삭제 버튼
            Button removeBtn = new Button(() =>
            {
                listProperty.DeleteArrayElementAtIndex(index);
                listProperty.serializedObject.ApplyModifiedProperties();
                RefreshRightPane();
            });
            removeBtn.ApplyRemoveButtonStyle();
            row.Add(removeBtn);

            itemsContainer.Add(row);

            // 3. [핵심 수정] 인라인 에디터 (SO 내부 값 수정)
            if (dataSO != null)
            {
                SerializedObject stepSO_Serialized = new SerializedObject(dataSO);
                stepSO_Serialized.Update();

                Foldout soFoldout = new Foldout() { text = "Edit Value", value = true, style = { marginLeft = 15 } };

                // A. 먼저 'questSpecificDatas' 리스트가 있는지 찾습니다.
                SerializedProperty specificList = stepSO_Serialized.FindProperty("questSpecificDatas");

                if (specificList != null && specificList.isArray)
                {
                    // B. 리스트가 있다면 -> 내 퀘스트 ID와 일치하는 항목을 찾습니다.
                    bool foundMyEntry = false;
                    for (int j = 0; j < specificList.arraySize; j++)
                    {
                        SerializedProperty entry = specificList.GetArrayElementAtIndex(j);
                        SerializedProperty idProp = entry.FindPropertyRelative("questId");

                        if (idProp != null && idProp.stringValue == questId)
                        {
                            foundMyEntry = true;

                            // 찾은 항목의 내부 변수들(amount, targetLevel 등)을 그립니다.
                            // 단, questId와 questIndex는 에디터에서 건드리지 못하게 숨깁니다.
                            SerializedProperty endProp = entry.GetEndProperty();
                            SerializedProperty childProp = entry.Copy();

                            if (childProp.NextVisible(true))
                            {
                                VisualElement valueBox = new VisualElement();
                                valueBox.style.ApplyBoxStyle(new Color(0.25f, 0.25f, 0.3f));
                                valueBox.style.marginTop = 5;

                                do
                                {
                                    if (SerializedProperty.EqualContents(childProp, endProp)) break;
                                    if (childProp.name == "questId" || childProp.name == "questIndex") continue;

                                    PropertyField field = new PropertyField(childProp);
                                    field.Bind(stepSO_Serialized);
                                    valueBox.Add(field);
                                } while (childProp.NextVisible(false));

                                soFoldout.Add(valueBox);
                            }

                            break;
                        }
                    }

                    if (!foundMyEntry)
                    {
                        soFoldout.Add(new Label($"(Data not synced for ID: {questId}. Please Refresh UI.)")
                            { style = { color = Color.yellow } });
                    }
                }
                else
                {
                    // C. 리스트가 없다면(구버전 SO 등) -> 그냥 모든 변수를 다 그립니다.
                    SerializedProperty iter = stepSO_Serialized.GetIterator();
                    iter.NextVisible(true);
                    while (iter.NextVisible(false))
                    {
                        if (iter.name == "m_Script") continue;
                        PropertyField commonField = new PropertyField(iter.Copy());
                        commonField.Bind(stepSO_Serialized);
                        soFoldout.Add(commonField);
                    }
                }

                itemsContainer.Add(soFoldout);

                // 값 변경 시 SO 저장
                // (이벤트 등록 부분은 기존과 동일)
                soFoldout.RegisterCallback<ChangeEvent<string>>(_ => stepSO_Serialized.ApplyModifiedProperties());
                soFoldout.RegisterCallback<ChangeEvent<int>>(_ => stepSO_Serialized.ApplyModifiedProperties());
                soFoldout.RegisterCallback<ChangeEvent<Enum>>(_ => stepSO_Serialized.ApplyModifiedProperties());
                soFoldout.RegisterCallback<ChangeEvent<float>>(_ => stepSO_Serialized.ApplyModifiedProperties());
                soFoldout.RegisterCallback<ChangeEvent<bool>>(_ => stepSO_Serialized.ApplyModifiedProperties());
            }
        }

        root.Add(itemsContainer);

        // 4. 추가 버튼 (기존 코드 유지)
        Button addBtn = new Button(() =>
        {
            GenericMenu menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name);

            foreach (var type in types)
            {
                menu.AddItem(new GUIContent($"Create New/{type.Name}"), false,
                    () => CreateAndAddAsset(type, listProperty, subFolderName));
            }

            menu.AddItem(new GUIContent("Add Empty Slot"), false, () =>
            {
                listProperty.arraySize++;
                listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1).objectReferenceValue = null;
                listProperty.serializedObject.ApplyModifiedProperties();
                RefreshRightPane();
            });
            menu.ShowAsContext();
        });
        addBtn.ApplyAddButtonStyle();
        root.Add(addBtn);

        parent.Add(root);
    }
 
    // --------------------------------------------------------------------------
    // [공용] SO 에셋 생성 및 리스트 추가 (범용화됨)
    // --------------------------------------------------------------------------
    private void CreateAndAddAsset(Type type, SerializedProperty listProperty, string subFolderName)
    {
        string path = AssetDatabase.GetAssetPath(questDatabase);
        string parentFolder = System.IO.Path.GetDirectoryName(path);

        string targetFolder = $"{parentFolder}/{subFolderName}";
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            AssetDatabase.CreateFolder(parentFolder, subFolderName);
        }

        string fileName = $"{type.Name}_{Guid.NewGuid().ToString().Substring(0, 4)}.asset";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{fileName}");

        ScriptableObject asset = ScriptableObject.CreateInstance(type);
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();

        listProperty.arraySize++;
        listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1).objectReferenceValue = asset;
        listProperty.serializedObject.ApplyModifiedProperties();
        RefreshRightPane();
        EditorGUIUtility.PingObject(asset);
    }

    private void DrawSceneGuideConfig(QuestInfoSO selectedQuest)
    {
        rightPane.Add(new Label("UI Guide Sequence (Scene)")
            { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } });

        if (sceneManager == null) return;
        var sequenceData = GetOrCreateSequence(selectedQuest);
        SerializedObject serializedManager = new SerializedObject(sceneManager);
        serializedManager.Update();
        SerializedProperty listProp = serializedManager.FindProperty("questSequences");
        int index = sceneManager.questSequences.IndexOf(sequenceData);

        if (index >= 0)
        {
            SerializedProperty stepSeqsProp =
                listProp.GetArrayElementAtIndex(index).FindPropertyRelative("stepSequences");
            VisualElement listContainer = new VisualElement();
            listContainer.style.ApplyBoxStyle(new Color(0.24f, 0.24f, 0.24f));
            PropertyField simpleListField = new PropertyField(stepSeqsProp, "Step Guide List");
            simpleListField.Bind(serializedManager);
            listContainer.Add(simpleListField);
            rightPane.Add(listContainer);
        }
    }

    private QuestSceneManager.QuestSequence GetOrCreateSequence(QuestInfoSO quest)
    {
        foreach (var seq in sceneManager.questSequences)
            if (seq.quest == quest)
                return seq;
        var newSeq = new QuestSceneManager.QuestSequence()
            { quest = quest, stepSequences = new List<QuestSceneManager.StepGuideSequence>() };
        sceneManager.questSequences.Add(newSeq);
        EditorUtility.SetDirty(sceneManager);
        return newSeq;
    }

    private T FindAssetByType<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }
}