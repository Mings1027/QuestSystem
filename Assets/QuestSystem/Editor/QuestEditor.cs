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
    // --- Data References ---
    private QuestDatabaseSO _questDatabase;
    private QuestSceneManager _sceneManager;
    private SerializedObject _serializedDatabase;

    // --- UI Components ---
    private ReorderableList _reorderableList;
    private VisualElement _settingsContainer;
    private VisualElement _contentContainer;
    private VisualElement _warningContainer;
    private ScrollView _rightPane;
    private ObjectField _databaseField;

    // --- State Variables ---
    private bool _showSettings;

    [SerializeField] private int selectedQuestIndex = -1; // 선택 상태 저장 (Undo 지원용)

    // --- Menu Item ---
    [MenuItem("Tools/Quest Guide Editor")]
    public static void ShowWindow()
    {
        QuestEditor wnd = GetWindow<QuestEditor>();
        wnd.titleContent = new GUIContent("Quest Editor");
        wnd.minSize = new Vector2(1000, 600);
    }

    // --- Lifecycle ---
    private void OnEnable()
    {
        if (_questDatabase == null)
            _questDatabase = FindAssetByType<QuestDatabaseSO>();

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        if (_serializedDatabase != null)
        {
            _serializedDatabase.Update();
            if (_reorderableList != null) _reorderableList.index = selectedQuestIndex;
        }

        RefreshState();
        Repaint();
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
        {
            // 리스트 전체를 다시 그리면 무거우므로, Repaint만 호출하여 
            // OnGUI나 VisualElement의 Dirty 상태를 갱신
            // (만약 상태 변화 시 UI 구조가 바뀌어야 한다면 RefreshRightPane()을 조건부 호출)
            Repaint();

            // 상태 표시 UI만 텍스트/색상 업데이트 (아래 로직에서 처리됨)
            // 여기서는 단순 Repaint로 UI가 갱신되도록 유도
        }
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();

        DrawHeader();

        _settingsContainer = new VisualElement { style = { display = DisplayStyle.None } };
        rootVisualElement.Add(_settingsContainer);

        _warningContainer = new VisualElement();
        rootVisualElement.Add(_warningContainer);

        _contentContainer = new VisualElement { style = { flexGrow = 1 } };
        rootVisualElement.Add(_contentContainer);

        RefreshState();
    }

    // --- Drawing UI ---
    private void DrawHeader()
    {
        VisualElement header = new VisualElement();
        header.style.ApplyRowStyle();
        header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

        header.Add(new Label("Database:")
            { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 10, alignSelf = Align.Center } });

        _databaseField = new ObjectField() { objectType = typeof(QuestDatabaseSO), style = { width = 250 } };
        _databaseField.value = _questDatabase;
        _databaseField.RegisterValueChangedCallback(evt =>
        {
            _questDatabase = evt.newValue as QuestDatabaseSO;
            RefreshState();
        });
        header.Add(_databaseField);

        header.Add(new VisualElement { style = { flexGrow = 1 } }); // Spacer

        Button settingsBtn = new Button(ToggleSettings)
        {
            text = "",
            style = { height = 20, marginRight = 5, alignSelf = Align.Center }
        };
        settingsBtn.Add(new Image { image = EditorGUIUtility.IconContent("d_SettingsIcon").image });
        header.Add(settingsBtn);

        header.Add(new Button(RefreshState) { text = "Refresh UI", style = { height = 20 } });
        rootVisualElement.Add(header);
    }

    private void ToggleSettings()
    {
        _showSettings = !_showSettings;
        if (_settingsContainer != null)
            _settingsContainer.style.display = _showSettings ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshState()
    {
        if (_warningContainer == null || _contentContainer == null) return;

        _warningContainer.Clear();
        _contentContainer.Clear();
        _settingsContainer.Clear();

        if (_questDatabase == null) _questDatabase = FindAssetByType<QuestDatabaseSO>();

        if (_questDatabase == null)
        {
            _warningContainer.Add(new HelpBox("QuestDatabaseSO를 찾을 수 없습니다.", HelpBoxMessageType.Warning));
            return;
        }

        // 데이터 동기화 먼저 수행
        SyncAllStepData();

        _serializedDatabase = new SerializedObject(_questDatabase);

        DrawSettingsContent();
        InitReorderableList();

        _sceneManager = FindFirstObjectByType<QuestSceneManager>();
        if (_sceneManager == null)
        {
            DrawCreateManagerUI();
            return;
        }

        DrawMainEditor();
    }

    private void DrawSettingsContent()
    {
        _settingsContainer.style.ApplyBoxStyle(new Color(0.25f, 0.25f, 0.25f));
        _settingsContainer.style.marginLeft = 5;
        _settingsContainer.style.marginRight = 5;
        _settingsContainer.style.marginBottom = 10;

        _settingsContainer.Add(new Label("Asset Generation Settings")
            { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });

        DrawFolderField("questFolder", "Quest Folder");
        DrawFolderField("requirementFolder", "Requirement Folder");
        DrawFolderField("stepFolder", "Step Folder");
        DrawFolderField("rewardFolder", "Reward Folder");
    }

    private void DrawFolderField(string propertyName, string label)
    {
        SerializedProperty prop = _serializedDatabase.FindProperty(propertyName);
        if (prop != null)
        {
            PropertyField field = new PropertyField(prop, label);
            field.Bind(_serializedDatabase);
            _settingsContainer.Add(field);
        }
        else
        {
            _settingsContainer.Add(new HelpBox($"Field '{propertyName}' not found.", HelpBoxMessageType.Error));
        }
    }

    private void DrawCreateManagerUI()
    {
        VisualElement box = new VisualElement { style = { alignItems = Align.Center, marginTop = 50 } };
        box.Add(new Label("⚠️ QuestSceneManager가 씬에 없습니다."));
        box.Add(new Button(() =>
        {
            var go = new GameObject("QuestSceneManager");
            Undo.RegisterCreatedObjectUndo(go, "Create Manager");
            go.AddComponent<QuestSceneManager>();
            RefreshState();
        }) { text = "매니저 생성", style = { width = 200, height = 30, marginTop = 10 } });
        _warningContainer.Add(box);
    }

    private void DrawMainEditor()
    {
        TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        _contentContainer.Add(splitView);

        // Left Pane (Quest List)
        IMGUIContainer leftList = new IMGUIContainer(() =>
        {
            if (_serializedDatabase == null || _serializedDatabase.targetObject == null) return;
            _serializedDatabase.Update();
            _reorderableList?.DoLayoutList();
            _serializedDatabase.ApplyModifiedProperties();
        });
        splitView.Add(leftList);

        // Right Pane (Details)
        _rightPane = new ScrollView { style = { paddingLeft = 10, paddingRight = 10, paddingTop = 10 } };
        splitView.Add(_rightPane);

        if (_reorderableList != null && _reorderableList.index >= 0)
            RefreshRightPane();
    }

    // --- ReorderableList Logic ---
    private void InitReorderableList()
    {
        SerializedProperty questsProp = _serializedDatabase.FindProperty("quests");
        _reorderableList = new ReorderableList(_serializedDatabase, questsProp, true, true, true, true);

        // 복구된 인덱스 적용
        _reorderableList.index = selectedQuestIndex;

        _reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Quest List");

        _reorderableList.drawElementCallback = (rect, index, _, _) =>
        {
            var element = questsProp.GetArrayElementAtIndex(index);
            var quest = element.objectReferenceValue as QuestInfoSO;
            string label = (quest != null) ? $"{index}: {quest.DisplayName}" : $"Null Quest ({index})";
            EditorGUI.LabelField(rect, label);
        };

        _reorderableList.onSelectCallback = (list) =>
        {
            Undo.RecordObject(this, "Select Quest");
            selectedQuestIndex = list.index;
            RefreshRightPane();
        };

        _reorderableList.onReorderCallbackWithDetails = (list, oldIdx, newIdx) =>
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Reorder Quest List");

            list.serializedProperty.serializedObject.ApplyModifiedProperties();
            SyncAllStepData(); // 재정렬 후 인덱스 동기화

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            RefreshRightPane();
        };

        _reorderableList.onRemoveCallback = (list) =>
        {
            if (EditorUtility.DisplayDialog("퀘스트 삭제", "리스트에서 삭제하시겠습니까?", "삭제", "취소"))
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Remove Quest");

                // 1. 리스트 삭제
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                list.serializedProperty.serializedObject.ApplyModifiedProperties();

                // 2. 연쇄 데이터 정리 (중요: 여기서 CleanUpAllQuestAssets가 호출됨)
                SyncAllStepData();

                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                RefreshRightPane();
            }
        };
    }

    private void RefreshRightPane()
    {
        _rightPane.Clear();

        if (_reorderableList == null || _reorderableList.index < 0 || 
            _reorderableList.index >= _questDatabase.quests.Count) return;

        SerializedProperty listEntry = _serializedDatabase.FindProperty("quests")
            .GetArrayElementAtIndex(_reorderableList.index);

        QuestInfoSO selectedQuest = listEntry.objectReferenceValue as QuestInfoSO;

        // --- 1. Asset Assign ---
        VisualElement assetAssignBox = new VisualElement();
        assetAssignBox.style.ApplySeparatorStyle();

        ObjectField questAssetField = new ObjectField("Target Quest Asset")
        {
            objectType = typeof(QuestInfoSO),
            value = selectedQuest
        };

        questAssetField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_questDatabase, "Change Quest Asset");
            _questDatabase.quests[_reorderableList.index] = evt.newValue as QuestInfoSO;
            _serializedDatabase.Update();
            RefreshRightPane();
        });

        assetAssignBox.Add(questAssetField);
        _rightPane.Add(assetAssignBox);

        if (selectedQuest == null)
        {
            _rightPane.Add(new HelpBox("퀘스트 에셋을 할당하거나 새로 생성하세요.", HelpBoxMessageType.Info));
            Button createBtn = new Button(() => CreateNewQuestAsset(listEntry, _reorderableList.index));
            createBtn.text = "새 퀘스트 에셋 생성";
            createBtn.ApplyCreateQuestButtonStyle();
            _rightPane.Add(createBtn);
            return;
        }

        // --- 2. Header & State Badge ---
        SerializedObject questSo = new SerializedObject(selectedQuest);
        questSo.Update();

        VisualElement headerBox = new VisualElement();
        headerBox.style.ApplyQuestHeaderStyle();

        // [배지 생성]
        if (Application.isPlaying) 
        {
            Label stateBadge = new Label(""); // 초기 텍스트 비움
            stateBadge.ApplyStateBadgeStyle(); 
            
            // 배지 업데이트 로직
            stateBadge.schedule.Execute(() => 
            {
                if (QuestManager.Instance == null) 
                {
                    stateBadge.text = "No Manager";
                    stateBadge.style.backgroundColor = Color.gray;
                    stateBadge.style.display = DisplayStyle.Flex;
                    return;
                }

                var runtimeQuest = QuestManager.Instance.GetQuestById(selectedQuest.ID);

                if (runtimeQuest != null)
                {
                    stateBadge.text = runtimeQuest.state.ToString();
                    stateBadge.style.backgroundColor = EditorStyleExtensions.GetQuestStateColor(runtimeQuest.state);
                    stateBadge.style.display = DisplayStyle.Flex; // 데이터 있으면 표시
                }
                else
                {
                    // 런타임 퀘스트를 못 찾으면 숨기거나 Inactive 표시
                    stateBadge.style.display = DisplayStyle.None; 
                }
            }).Every(100); 

            headerBox.Add(stateBadge);
        }

        // Quest Title
        TextField nameField = new TextField { bindingPath = "displayName" };
        nameField.Bind(questSo);
        nameField.ApplyQuestTitleStyle();
        nameField.style.marginRight = 70; // 배지 공간 확보
        headerBox.Add(nameField);

        // Options
        VisualElement optionsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
        
        var autoStart = new PropertyField(questSo.FindProperty("autoStart")) { style = { flexGrow = 1, marginRight = 10 } };
        var autoComplete = new PropertyField(questSo.FindProperty("autoComplete")) { style = { flexGrow = 1 } };
        
        optionsRow.Add(autoStart);
        optionsRow.Add(autoComplete);
        
        headerBox.Add(optionsRow);
        
        _rightPane.Add(headerBox);

        // --- 3. Sub-lists ---
        DrawScriptableObjectList(_rightPane, questSo.FindProperty("requirements"), "Requirements",
            typeof(QuestRequirementDataSO), "Requirements", selectedQuest.ID);

        DrawScriptableObjectList(_rightPane, questSo.FindProperty("steps"), "Quest Steps",
            typeof(QuestStepDataSO), "Steps", selectedQuest.ID, selectedQuest);

        DrawScriptableObjectList(_rightPane, questSo.FindProperty("rewards"), "Rewards",
            typeof(QuestRewardDataSO), "Rewards", selectedQuest.ID);
    }

    // --- Sub-list Drawing Logic ---
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
            Object dataSo = element.objectReferenceValue;

            string foldoutKey = (dataSo != null)
                ? $"QuestEditor_Fold_{dataSo.GetInstanceID()}"
                : $"QuestEditor_Fold_{questId}_{listProp.name}_{index}";
            bool isExpanded = SessionState.GetBool(foldoutKey, false);

            VisualElement itemContainer = new VisualElement { style = { marginBottom = 2 } };
            VisualElement headerBar = new VisualElement();
            headerBar.style.ApplyListItemHeaderStyle();

            // Foldout Arrow
            Foldout foldout = new Foldout { value = isExpanded, text = "" };
            foldout.style.marginRight = 0;

            // Object Field
            ObjectField objField = new ObjectField
            {
                objectType = baseType,
                value = dataSo,
                style = { flexGrow = 1, marginLeft = 5, marginRight = 5 }
            };
            objField.RegisterValueChangedCallback(evt =>
            {
                element.objectReferenceValue = evt.newValue;
                listProp.serializedObject.ApplyModifiedProperties();
                SyncAllStepData();
                RefreshRightPane();
            });

            // Delete Button
            Button delBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("항목 삭제", "삭제하시겠습니까?", "삭제", "취소"))
                {
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName($"Remove {baseType.Name}");

                    if (selectedQuest != null) Undo.RecordObject(selectedQuest, "Remove Item");

                    listProp.DeleteArrayElementAtIndex(index);
                    listProp.serializedObject.ApplyModifiedProperties();

                    SyncAllStepData(); // 여기서 CleanUp이 동작하여 실제 데이터 삭제됨

                    Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                    RefreshRightPane();
                }
            });
            delBtn.ApplyRemoveButtonStyle();
            delBtn.style.height = 20;
            delBtn.style.alignSelf = Align.Center;

            headerBar.Add(foldout);
            headerBar.Add(objField);
            headerBar.Add(delBtn);
            itemContainer.Add(headerBar);

            // Content Box
            VisualElement contentBox = new VisualElement { name = "content-box" };
            contentBox.style.ApplyListItemContentStyle();
            contentBox.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (isExpanded)
            {
                headerBar.style.borderBottomLeftRadius = 0;
                headerBar.style.borderBottomRightRadius = 0;
            }

            // Foldout Callback (UI Toggling)
            foldout.RegisterValueChangedCallback(evt =>
            {
                SessionState.SetBool(foldoutKey, evt.newValue);
                contentBox.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;

                if (evt.newValue)
                {
                    headerBar.style.borderBottomLeftRadius = 0;
                    headerBar.style.borderBottomRightRadius = 0;
                }
                else
                {
                    headerBar.style.borderBottomLeftRadius = 5;
                    headerBar.style.borderBottomRightRadius = 5;
                }
            });

            if (dataSo != null)
            {
                DrawSoFields(contentBox, dataSo, questId);
                if (selectedQuest != null && baseType == typeof(QuestStepDataSO))
                    DrawStepUIGuide(contentBox, selectedQuest, index);
            }
            else
            {
                contentBox.Add(new Label("Empty Slot") { style = { color = Color.gray } });
            }

            itemContainer.Add(contentBox);
            container.Add(itemContainer);
        }

        Button addBtn = new Button(() => ShowAddMenu(listProp, baseType, folder));
        addBtn.ApplyAddButtonStyle();
        container.Add(addBtn);

        parent.Add(container);
    }

    // --- Helper Methods ---
    private void CreateNewQuestAsset(SerializedProperty listEntry, int index)
    {
        string dirPath = GetFolderPath("questFolder");
        string fileName = $"New Quest_{Guid.NewGuid().ToString()[..7]}";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{dirPath}/{fileName}.asset");

        QuestInfoSO newQuest = CreateInstance<QuestInfoSO>();
        newQuest.DisplayName = "New Quest";
        AssetDatabase.CreateAsset(newQuest, fullPath);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Create Quest");
        Undo.RecordObject(_questDatabase, "Add Quest");

        listEntry.objectReferenceValue = newQuest;
        listEntry.serializedObject.ApplyModifiedProperties();

        if (index >= 0 && index < _questDatabase.quests.Count)
            _questDatabase.quests[index] = newQuest;

        SyncAllStepData();
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        RefreshRightPane();
        EditorGUIUtility.PingObject(newQuest);
    }

    private void CreateAndAddAsset(Type type, SerializedProperty listProp, string subFolder)
    {
        string fieldName = "";
        if (typeof(QuestRequirementDataSO).IsAssignableFrom(type)) fieldName = "requirementFolder";
        else if (typeof(QuestStepDataSO).IsAssignableFrom(type)) fieldName = "stepFolder";
        else if (typeof(QuestRewardDataSO).IsAssignableFrom(type)) fieldName = "rewardFolder";

        string dirPath = GetFolderPath(fieldName, subFolder);
        string fileName = $"{type.Name}_{Guid.NewGuid().ToString()[..4]}";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{dirPath}/{fileName}.asset");

        ScriptableObject asset = CreateInstance(type);
        AssetDatabase.CreateAsset(asset, fullPath);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName($"Add {type.Name}");

        // 리스트 추가
        listProp.arraySize++;
        var element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
        element.objectReferenceValue = asset;
        listProp.serializedObject.ApplyModifiedProperties();

        SyncAllStepData();

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        RefreshRightPane();
    }

    private string GetFolderPath(string propertyName, string defaultSubFolder = "")
    {
        var prop = _serializedDatabase.FindProperty(propertyName);
        if (prop != null && prop.objectReferenceValue is DefaultAsset asset)
        {
            return AssetDatabase.GetAssetPath(asset);
        }

        string dbPath = AssetDatabase.GetAssetPath(_questDatabase);
        string dir = System.IO.Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(defaultSubFolder)) dir += "/" + defaultSubFolder;

        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        return dir;
    }

    private void ShowAddMenu(SerializedProperty listProp, Type baseType, string subFolder)
    {
        GenericMenu menu = new GenericMenu();
        var types = TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract).OrderBy(t => t.Name);

        foreach (var t in types)
            menu.AddItem(new GUIContent($"Create New/{t.Name}"), false,
                () => CreateAndAddAsset(t, listProp, subFolder));

        menu.AddItem(new GUIContent("Add Empty Slot"), false, () =>
        {
            Undo.RecordObject(listProp.serializedObject.targetObject, "Add Empty Slot");
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = null;
            listProp.serializedObject.ApplyModifiedProperties();

            SyncAllStepData();
            RefreshRightPane();
        });
        menu.ShowAsContext();
    }

    private void DrawSoFields(VisualElement parent, Object dataSo, string questId)
    {
        SerializedObject so = new SerializedObject(dataSo);
        SerializedProperty specificList = so.FindProperty("questSpecificDatas");
        if (specificList == null) return;

        for (int j = 0; j < specificList.arraySize; j++)
        {
            SerializedProperty entry = specificList.GetArrayElementAtIndex(j);
            if (entry.FindPropertyRelative("questId").stringValue == questId)
            {
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

        if (_sceneManager == null) _sceneManager = FindFirstObjectByType<QuestSceneManager>();
        
        SerializedObject smSo = new SerializedObject(_sceneManager);
        var targetProp = smSo.FindProperty("questSequences")
            .GetArrayElementAtIndex(_sceneManager.questSequences.IndexOf(seq))
            .FindPropertyRelative("stepSequences").GetArrayElementAtIndex(stepIdx)
            .FindPropertyRelative("uiTargets");

        // [수정] Scene UI 섹션 디자인 개선 (깔끔하게)
        VisualElement uiBox = new VisualElement();
        uiBox.style.ApplySceneSectionStyle();

        // 심플한 텍스트 헤더
        Label headerLabel = new Label("SCENE UI TARGETS");
        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerLabel.style.fontSize = 10;
        headerLabel.style.color = QuestEditorTheme.SceneHeaderColor;
        headerLabel.style.marginBottom = 4;
        uiBox.Add(headerLabel);
        
        PropertyField listField = new PropertyField(targetProp, "");
        listField.Bind(smSo);
        uiBox.Add(listField);
        
        parent.Add(uiBox);
    }

    private QuestSceneManager.QuestSequence GetOrCreateSequence(QuestInfoSO quest)
    {
        var seq = _sceneManager.questSequences.FirstOrDefault(s => s.Quest == quest);
        if (seq == null)
        {
            seq = new QuestSceneManager.QuestSequence();
            seq.Init(quest);
            Undo.RecordObject(_sceneManager, "Add Quest Sequence");
            _sceneManager.questSequences.Add(seq);
            EditorUtility.SetDirty(_sceneManager);
        }

        while (seq.StepSequences.Count < quest.steps.Count)
            seq.StepSequences.Add(new QuestSceneManager.StepGuideSequence());

        return seq;
    }

    // --- Synchronization & Cleanup ---
    private void SyncAllStepData()
    {
        if (_questDatabase == null) return;

        // [중요] Usage Map 구축: "어떤 에셋이 어떤 퀘스트에서 쓰이고 있는가?"
        Dictionary<ScriptableObject, HashSet<string>> assetUsageMap =
            new Dictionary<ScriptableObject, HashSet<string>>();

        void RegisterUsage(ScriptableObject asset, string questId)
        {
            if (asset == null) return;
            Undo.RecordObject(asset, "Sync Data Index");
            if (!assetUsageMap.ContainsKey(asset)) assetUsageMap[asset] = new HashSet<string>();
            assetUsageMap[asset].Add(questId);
        }

        for (int i = 0; i < _questDatabase.quests.Count; i++)
        {
            var q = _questDatabase.quests[i];
            if (q == null) continue;

            q.requirements.ForEach(r =>
            {
                if (r)
                {
                    RegisterUsage(r, q.ID);
                    r.SyncQuestData(q.ID, i);
                }
            });
            q.steps.ForEach(s =>
            {
                if (s)
                {
                    RegisterUsage(s, q.ID);
                    s.SyncQuestData(q.ID, i);
                }
            });
            q.rewards.ForEach(rw =>
            {
                if (rw)
                {
                    RegisterUsage(rw, q.ID);
                    rw.SyncQuestData(q.ID, i);
                }
            });
        }

        CleanUpAllQuestAssets(assetUsageMap);
        SyncQuestSceneManager();
    }

    private void SyncQuestSceneManager()
    {
        if (_sceneManager == null || _questDatabase == null) return;

        Undo.RecordObject(_sceneManager, "Sync Scene Manager");

        // 1. 없는 퀘스트 제거
        _sceneManager.questSequences.RemoveAll(seq =>
            seq.Quest == null || !_questDatabase.quests.Contains(seq.Quest));

        // 2. 스텝 개수 동기화
        foreach (var seq in _sceneManager.questSequences)
        {
            if (seq.Quest == null) continue;
            int actualCount = seq.Quest.steps.Count;

            if (seq.StepSequences.Count > actualCount)
                seq.StepSequences.RemoveRange(actualCount, seq.StepSequences.Count - actualCount);

            while (seq.StepSequences.Count < actualCount)
                seq.StepSequences.Add(new QuestSceneManager.StepGuideSequence());
        }

        EditorUtility.SetDirty(_sceneManager);
    }

    private void CleanUpAllQuestAssets(Dictionary<ScriptableObject, HashSet<string>> usageMap)
    {
        string[] filter = { "t:QuestStepDataSO", "t:QuestRequirementDataSO", "t:QuestRewardDataSO" };
        var guids = AssetDatabase.FindAssets(string.Join(" ", filter));

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) continue;

            // UsageMap에 없으면(아무도 안 쓰면) 빈 Set을 넘겨서 데이터 전체 삭제 유도
            if (!usageMap.TryGetValue(asset, out var validIds))
                validIds = new HashSet<string>();

            Undo.RecordObject(asset, "Cleanup Data");

            if (asset is QuestStepDataSO s) s.CleanUpUnusedData(validIds);
            else if (asset is QuestRequirementDataSO r) r.CleanUpUnusedData(validIds);
            else if (asset is QuestRewardDataSO rw) rw.CleanUpUnusedData(validIds);
        }
    }

    private static T FindAssetByType<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }
}