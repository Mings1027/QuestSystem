using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;

public class QuestEditor : EditorWindow
{
    // --- Data References ---
    private QuestDatabaseSO _questDatabase;
    private QuestSceneManager _sceneManager;
    private SerializedObject _serializedDatabase;

    // --- UI Components ---
    private ListView _questListView;
    private ScrollView _rightPane;
    private VisualElement _settingsContainer;
    private bool _showSettings;

    // --- State ---
    [SerializeField] private int selectedQuestIndex = -1;

    [MenuItem("Tools/Quest Guide Editor")]
    public static void ShowWindow()
    {
        QuestEditor wnd = GetWindow<QuestEditor>();
        wnd.titleContent = new GUIContent("Quest Editor");
        wnd.minSize = new Vector2(1000, 600);
    }

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
            _questListView?.RefreshItems();
        }
        
        RefreshRightPane(); 
    }
    
    public void CreateGUI()
    {
        rootVisualElement.Clear();

        if (_questDatabase == null) _questDatabase = FindAssetByType<QuestDatabaseSO>();
        if (_questDatabase == null)
        {
            rootVisualElement.Add(new HelpBox("QuestDatabaseSO not found!", HelpBoxMessageType.Error));
            return;
        }

        _serializedDatabase = new SerializedObject(_questDatabase);
        
        SyncAllStepData();

        DrawHeader();
        DrawSettings();

        TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(splitView);

        CreateQuestListView(splitView);

        _rightPane = new ScrollView { style = { paddingLeft = 10, paddingRight = 10, paddingTop = 10 } };
        splitView.Add(_rightPane);

        if (selectedQuestIndex >= 0 && selectedQuestIndex < _questDatabase.quests.Count)
        {
            _questListView.selectedIndex = selectedQuestIndex;
            RefreshRightPane();
        }
    }

    // --------------------------------------------------------------------------
    // 1. ListView Implementation (Left Pane)
    // --------------------------------------------------------------------------
    private void CreateQuestListView(VisualElement parent)
    {
        _questListView = new ListView();
        _questListView.style.flexGrow = 1;
        _questListView.bindingPath = "quests"; 
        _questListView.showAddRemoveFooter = true; 
        _questListView.reorderable = true;         
        _questListView.showBorder = true;
        _questListView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;

        _questListView.makeItem = () => new Label { style = { paddingLeft = 5, paddingTop = 2, paddingBottom = 2 } };

        _questListView.bindItem = (element, index) =>
        {
            var label = element as Label;
            if (_questDatabase.quests != null && index < _questDatabase.quests.Count)
            {
                var quest = _questDatabase.quests[index];
                // Null일 경우 (Empty) 표시
                label.text = (quest != null) ? $"[{index}] {quest.DisplayName}" : $"[{index}] (Empty Slot)";
                label.style.color = (quest == null) ? Color.yellow : Color.white;
            }
        };

        _questListView.selectionChanged += (objects) =>
        {
            selectedQuestIndex = _questListView.selectedIndex;
            RefreshRightPane();
        };

        _questListView.itemsAdded += (indices) => 
        { 
            _serializedDatabase.Update();
            SerializedProperty listProp = _serializedDatabase.FindProperty("quests");

            var enumerable = indices as int[] ?? indices.ToArray();
            foreach (int index in enumerable)
            {
                var element = listProp.GetArrayElementAtIndex(index);
                element.objectReferenceValue = null; 
            }
            
            _serializedDatabase.ApplyModifiedProperties();

            // UI 갱신 및 포커스 이동
            _questListView.RefreshItems();
            if (enumerable.Any())
            {
                _questListView.selectedIndex = enumerable.First();
            }
        };

        _questListView.itemsRemoved += (indices) => { SyncAllStepData(); RefreshRightPane(); };
        _questListView.itemIndexChanged += (oldIdx, newIdx) => { SyncAllStepData(); RefreshRightPane(); };

        _questListView.Bind(_serializedDatabase);
        parent.Add(_questListView);
    }

    // --------------------------------------------------------------------------
    // 2. Details Pane (Right Pane)
    // --------------------------------------------------------------------------
    private void RefreshRightPane()
    {
        _rightPane.Clear();

        if (selectedQuestIndex < 0 || selectedQuestIndex >= _questDatabase.quests.Count) 
        {
            _rightPane.Add(new Label("Select a quest to edit") { style = { alignSelf = Align.Center, marginTop = 20 } });
            return;
        }

        // 현재 선택된 Property 가져오기
        SerializedProperty listProp = _serializedDatabase.FindProperty("quests");
        SerializedProperty questProp = listProp.GetArrayElementAtIndex(selectedQuestIndex);
        QuestInfoSO selectedQuest = questProp.objectReferenceValue as QuestInfoSO;

        // 1. 할당 필드 (항상 최상단에 표시)
        VisualElement assignBox = new VisualElement();
        assignBox.style.ApplyBoxStyle(new Color(0.25f, 0.25f, 0.25f));
        
        ObjectField objectField = new ObjectField("Quest Asset") 
        { 
            objectType = typeof(QuestInfoSO), 
            value = selectedQuest 
        };
        
        // 값 변경 시 (드래그 앤 드롭 등)
        objectField.RegisterValueChangedCallback(evt => 
        {
            questProp.objectReferenceValue = evt.newValue;
            _serializedDatabase.ApplyModifiedProperties();
            
            // 이름 갱신을 위해 리스트뷰 새로고침
            _questListView.RefreshItems();
            RefreshRightPane();
        });

        assignBox.Add(objectField);
        _rightPane.Add(assignBox);

        // 2. Null일 경우 -> 생성 버튼 표시
        if (selectedQuest == null)
        {
            Button createBtn = new Button(() => CreateNewQuestAsset(selectedQuestIndex));
            createBtn.text = "Create New Quest Asset";
            createBtn.ApplyCreateQuestButtonStyle(); // 기존 스타일 재활용
            
            // 안내 문구
            Label guideLabel = new Label("Assign an existing Quest or Create a new one.");
            guideLabel.style.alignSelf = Align.Center;
            guideLabel.style.marginTop = 10;
            guideLabel.style.color = Color.gray;

            _rightPane.Add(guideLabel);
            _rightPane.Add(createBtn);
            return; // 여기서 그리기 중단
        }

        // 3. 에셋이 존재할 경우 -> 기존 에디터 그리기
        SerializedObject questSo = new SerializedObject(selectedQuest);
        questSo.Update();

        // --- Header ---
        VisualElement headerBox = new VisualElement();
        headerBox.style.ApplyQuestHeaderStyle();

        if (Application.isPlaying) DrawRuntimeBadge(headerBox, selectedQuest.ID);

        TextField nameField = new TextField { bindingPath = "displayName" };
        nameField.Bind(questSo);
        nameField.ApplyQuestTitleStyle();
        headerBox.Add(nameField);

        VisualElement optionsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5 } };
        var autoStart = new PropertyField(questSo.FindProperty("autoStart")) { style = { flexGrow = 1, marginRight = 10 } };
        var autoComplete = new PropertyField(questSo.FindProperty("autoComplete")) { style = { flexGrow = 1 } };
        optionsRow.Add(autoStart);
        optionsRow.Add(autoComplete);
        headerBox.Add(optionsRow);

        _rightPane.Add(headerBox);

        // --- Sub Lists ---
        DrawScriptableObjectList(_rightPane, questSo.FindProperty("requirements"), "Requirements", typeof(QuestRequirementDataSO), selectedQuest.ID);
        DrawScriptableObjectList(_rightPane, questSo.FindProperty("steps"), "Quest Steps", typeof(QuestStepDataSO), selectedQuest.ID, selectedQuest);
        DrawScriptableObjectList(_rightPane, questSo.FindProperty("rewards"), "Rewards", typeof(QuestRewardDataSO), selectedQuest.ID);
    }

    // --------------------------------------------------------------------------
    // 3. Sub-List Logic (Shared)
    // --------------------------------------------------------------------------
    private void DrawScriptableObjectList(VisualElement parent, SerializedProperty listProp, string title, Type baseType, string questId, QuestInfoSO selectedQuest = null)
    {
        VisualElement container = new VisualElement();
        container.style.ApplyBoxStyle(new Color(0.22f, 0.22f, 0.22f));

        container.Add(new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });

        for (int i = 0; i < listProp.arraySize; i++)
        {
            int index = i;
            SerializedProperty element = listProp.GetArrayElementAtIndex(i);
            Object dataSo = element.objectReferenceValue;

            string foldoutKey = (dataSo != null) ? $"Fold_{dataSo.GetInstanceID()}" : $"Fold_{questId}_{listProp.name}_{index}";
            bool isExpanded = SessionState.GetBool(foldoutKey, true);

            VisualElement itemBox = new VisualElement { style = { marginBottom = 2 } };
            
            VisualElement headerBar = new VisualElement();
            headerBar.style.ApplyListItemHeaderStyle();

            Foldout foldout = new Foldout { value = isExpanded, text = "" };
            foldout.RegisterValueChangedCallback(evt => {
                SessionState.SetBool(foldoutKey, evt.newValue);
                itemBox.Q("content-box").style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            
            ObjectField objField = new ObjectField { objectType = baseType, value = dataSo, style = { flexGrow = 1, marginLeft = 5 } };
            objField.RegisterValueChangedCallback(evt => {
                element.objectReferenceValue = evt.newValue;
                listProp.serializedObject.ApplyModifiedProperties();
                SyncAllStepData();
                RefreshRightPane();
            });

            Button delBtn = new Button(() => {
                listProp.DeleteArrayElementAtIndex(index);
                listProp.serializedObject.ApplyModifiedProperties();
                SyncAllStepData();
                RefreshRightPane();
            });
            delBtn.ApplyRemoveButtonStyle();

            headerBar.Add(foldout);
            headerBar.Add(objField);
            headerBar.Add(delBtn);
            itemBox.Add(headerBar);

            VisualElement contentBox = new VisualElement { name = "content-box" };
            contentBox.style.ApplyListItemContentStyle();
            contentBox.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (dataSo != null)
            {
                DrawSoFields(contentBox, dataSo, questId);
                if (selectedQuest != null && baseType == typeof(QuestStepDataSO))
                    DrawStepUIGuide(contentBox, selectedQuest, index);
            }

            itemBox.Add(contentBox);
            container.Add(itemBox);
        }

        Button addBtn = new Button(() => ShowAddMenu(listProp, baseType));
        addBtn.ApplyAddButtonStyle();
        container.Add(addBtn);

        parent.Add(container);
    }

    // --------------------------------------------------------------------------
    // 4. Scene Guide Action Logic (Custom Button & Menu)
    // --------------------------------------------------------------------------
    private void DrawStepUIGuide(VisualElement parent, QuestInfoSO quest, int stepIdx)
    {
        var seq = GetOrCreateSequence(quest);
        if (stepIdx >= seq.StepSequences.Count) return;

        if (_sceneManager == null) _sceneManager = FindFirstObjectByType<QuestSceneManager>();
        if (_sceneManager == null) return;

        SerializedObject smSo = new SerializedObject(_sceneManager);
        
        var stepSeqProp = smSo.FindProperty("questSequences")
            .GetArrayElementAtIndex(_sceneManager.questSequences.IndexOf(seq))
            .FindPropertyRelative("stepSequences").GetArrayElementAtIndex(stepIdx);
            
        var actionsProp = stepSeqProp.FindPropertyRelative("actions");

        VisualElement uiBox = new VisualElement();
        uiBox.style.ApplySceneSectionStyle();

        uiBox.Add(new Label("SCENE GUIDE ACTIONS") { 
            style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10, color = QuestEditorTheme.SceneHeaderColor, marginBottom = 4 } 
        });

        PropertyField listField = new PropertyField(actionsProp, "");
        listField.Bind(smSo);
        uiBox.Add(listField);

        Button addBtn = new Button(() => ShowActionAddMenu(actionsProp));
        addBtn.text = "+ Add Action";
        addBtn.style.height = 24;
        addBtn.style.marginTop = 4;
        addBtn.style.backgroundColor = new Color(0.25f, 0.4f, 0.25f);
        addBtn.style.color = Color.white;
        addBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiBox.Add(addBtn);

        parent.Add(uiBox);
    }

    private void ShowActionAddMenu(SerializedProperty listProp)
    {
        GenericMenu menu = new GenericMenu();
        var types = TypeCache.GetTypesDerivedFrom<GuideAction>()
            .Where(t => !t.IsAbstract && !t.IsGenericType)
            .OrderBy(t => t.Name);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<GuideActionCategoryAttribute>();
            string menuPath = attr != null ? attr.MenuPath : type.Name;

            menu.AddItem(new GUIContent(menuPath), false, () => {
                listProp.serializedObject.Update();
                listProp.arraySize++;
                var element = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                element.managedReferenceValue = Activator.CreateInstance(type);
                listProp.serializedObject.ApplyModifiedProperties();
            });
        }
        if (!types.Any()) menu.AddDisabledItem(new GUIContent("No Actions Found"));
        menu.ShowAsContext();
    }

    // --------------------------------------------------------------------------
    // 5. Asset & Data Helpers
    // --------------------------------------------------------------------------
    private void CreateNewQuestAsset(int index)
    {
        string dirPath = GetFolderPath("questFolder");
        string fileName = $"New Quest_{Guid.NewGuid().ToString()[..7]}";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{dirPath}/{fileName}.asset");

        QuestInfoSO newQuest = CreateInstance<QuestInfoSO>();
        newQuest.DisplayName = "New Quest";
        AssetDatabase.CreateAsset(newQuest, fullPath);

        // [중요] 특정 인덱스에 할당
        SerializedProperty listProp = _serializedDatabase.FindProperty("quests");
        if (index >= 0 && index < listProp.arraySize)
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);
            element.objectReferenceValue = newQuest;
            listProp.serializedObject.ApplyModifiedProperties();
        }
        
        SyncAllStepData();
        
        // UI 갱신 (리스트뷰 이름 업데이트 & 오른쪽 패널 갱신)
        _questListView.RefreshItems();
        RefreshRightPane();
    }

    private void CreateAndAddAsset(Type type, SerializedProperty listProp)
    {
        string fieldName = "";
        if (typeof(QuestRequirementDataSO).IsAssignableFrom(type)) fieldName = "requirementFolder";
        else if (typeof(QuestStepDataSO).IsAssignableFrom(type)) fieldName = "stepFolder";
        else if (typeof(QuestRewardDataSO).IsAssignableFrom(type)) fieldName = "rewardFolder";

        string dirPath = GetFolderPath(fieldName);
        string fileName = $"{type.Name}_{Guid.NewGuid().ToString()[..4]}";
        string fullPath = AssetDatabase.GenerateUniqueAssetPath($"{dirPath}/{fileName}.asset");

        ScriptableObject asset = CreateInstance(type);
        AssetDatabase.CreateAsset(asset, fullPath);

        listProp.arraySize++;
        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = asset;
        listProp.serializedObject.ApplyModifiedProperties();

        SyncAllStepData();
        RefreshRightPane();
    }

    private void ShowAddMenu(SerializedProperty listProp, Type baseType)
    {
        GenericMenu menu = new GenericMenu();
        var types = TypeCache.GetTypesDerivedFrom(baseType).Where(t => !t.IsAbstract).OrderBy(t => t.Name);

        foreach (var t in types)
            menu.AddItem(new GUIContent($"Create New/{t.Name}"), false, () => CreateAndAddAsset(t, listProp));

        menu.ShowAsContext();
    }

    private string GetFolderPath(string propertyName)
    {
        var prop = _serializedDatabase.FindProperty(propertyName);
        if (prop != null && prop.objectReferenceValue is DefaultAsset asset)
            return AssetDatabase.GetAssetPath(asset);

        string dbPath = AssetDatabase.GetAssetPath(_questDatabase);
        string dir = System.IO.Path.GetDirectoryName(dbPath);
        if (!System.IO.Directory.Exists(dir)) { System.IO.Directory.CreateDirectory(dir); AssetDatabase.Refresh(); }
        return dir;
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

    private void SyncAllStepData()
    {
        if (_questDatabase == null) return;
        Dictionary<ScriptableObject, HashSet<string>> assetUsageMap = new Dictionary<ScriptableObject, HashSet<string>>();

        void Register(ScriptableObject asset, string qId) {
            if (!asset) return;
            if (!assetUsageMap.ContainsKey(asset)) assetUsageMap[asset] = new HashSet<string>();
            assetUsageMap[asset].Add(qId);
        }

        for (int i = 0; i < _questDatabase.quests.Count; i++)
        {
            var q = _questDatabase.quests[i];
            if (q == null) continue;
            
            q.requirements.ForEach(r => { Register(r, q.ID); if(r) r.SyncQuestData(q.ID, i); });
            q.steps.ForEach(s => { Register(s, q.ID); if(s) s.SyncQuestData(q.ID, i); });
            q.rewards.ForEach(r => { Register(r, q.ID); if(r) r.SyncQuestData(q.ID, i); });
        }

        string[] filter = { "t:QuestStepDataSO", "t:QuestRequirementDataSO", "t:QuestRewardDataSO" };
        foreach (var guid in AssetDatabase.FindAssets(string.Join(" ", filter)))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null) continue;

            if (!assetUsageMap.TryGetValue(asset, out var validIds)) validIds = new HashSet<string>();

            if (asset is QuestStepDataSO s) s.CleanUpUnusedData(validIds);
            else if (asset is QuestRequirementDataSO r) r.CleanUpUnusedData(validIds);
            else if (asset is QuestRewardDataSO rw) rw.CleanUpUnusedData(validIds);
        }

        if (_sceneManager != null) {
            _sceneManager.questSequences.RemoveAll(seq => seq.Quest == null || !_questDatabase.quests.Contains(seq.Quest));
            foreach (var seq in _sceneManager.questSequences) {
                while (seq.StepSequences.Count < seq.Quest.steps.Count) seq.StepSequences.Add(new QuestSceneManager.StepGuideSequence());
            }
            EditorUtility.SetDirty(_sceneManager);
        }
    }

    private void DrawHeader()
    {
        VisualElement header = new VisualElement();
        header.style.ApplyRowStyle();
        header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);

        header.Add(new Label("Database:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 10, alignSelf = Align.Center } });
        
        ObjectField dbField = new ObjectField { objectType = typeof(QuestDatabaseSO), value = _questDatabase, style = { width = 250 } };
        dbField.RegisterValueChangedCallback(evt => { _questDatabase = evt.newValue as QuestDatabaseSO; CreateGUI(); });
        header.Add(dbField);
        
        header.Add(new VisualElement { style = { flexGrow = 1 } }); 
        
        Button settingsBtn = new Button(() => _settingsContainer.style.display = (_showSettings = !_showSettings) ? DisplayStyle.Flex : DisplayStyle.None) 
            { text = "Settings", style = { height = 20 } };
        header.Add(settingsBtn);
        
        rootVisualElement.Add(header);
    }

    private void DrawSettings()
    {
        _settingsContainer = new VisualElement { style = { display = DisplayStyle.None } };
        _settingsContainer.style.ApplyBoxStyle(new Color(0.25f, 0.25f, 0.25f));
        
        void DrawPath(string prop) {
            PropertyField f = new PropertyField(_serializedDatabase.FindProperty(prop));
            f.Bind(_serializedDatabase);
            _settingsContainer.Add(f);
        }
        DrawPath("questFolder"); DrawPath("requirementFolder"); DrawPath("stepFolder"); DrawPath("rewardFolder");
        rootVisualElement.Add(_settingsContainer);
    }
    
    private void DrawRuntimeBadge(VisualElement parent, string questId)
    {
        Label badge = new Label();
        badge.ApplyStateBadgeStyle();
        parent.Add(badge);
        
        badge.schedule.Execute(() => {
            if (QuestManager.Instance == null) return;
            var q = QuestManager.Instance.GetQuestById(questId);
            if (q != null) {
                badge.text = q.state.ToString();
                badge.style.backgroundColor = EditorStyleExtensions.GetQuestStateColor(q.state);
                badge.style.display = DisplayStyle.Flex;
            } else {
                badge.style.display = DisplayStyle.None;
            }
        }).Every(500);
    }
    
    private void DrawSoFields(VisualElement parent, Object dataSo, string questId)
    {
        SerializedObject so = new SerializedObject(dataSo);
        SerializedProperty specificList = so.FindProperty("questSpecificDatas");
        if (specificList == null) return;

        for (int j = 0; j < specificList.arraySize; j++) {
            SerializedProperty entry = specificList.GetArrayElementAtIndex(j);
            if (entry.FindPropertyRelative("questId").stringValue == questId) {
                SerializedProperty iter = entry.Copy();
                SerializedProperty end = entry.GetEndProperty();
                if (iter.NextVisible(true)) {
                    do {
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
    
    private static T FindAssetByType<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T)}");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }
}