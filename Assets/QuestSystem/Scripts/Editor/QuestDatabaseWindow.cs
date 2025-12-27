using System;
using System.Collections.Generic;
using System.Linq; // 리스트 정렬 등을 위해 필요
using System.Reflection;
using QuestSystem; // 리플렉션(클래스 찾기)을 위해 필요
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class QuestDatabaseWindow : EditorWindow
{
    private QuestDatabaseSO currentDatabase;
    private ListView leftList;
    private ScrollView rightPane; // VisualElement 대신 ScrollView로 변경하여 스크롤 지원

    private const string DB_PREFS_KEY = "SelectedQuestDBPath";

    [MenuItem("Quest System/Open Database")]
    public static void OpenWindow()
    {
        QuestDatabaseWindow wnd = GetWindow<QuestDatabaseWindow>();
        wnd.titleContent = new GUIContent("Quest DB Editor");
    }

    public void CreateGUI()
    {
        // 루트 방향 설정
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        // ---------------------------------------------------------
        // 1. 먼저 UI 요소들을 생성하고 배치합니다. (순서 중요!)
        // ---------------------------------------------------------

        // 상단 툴바 영역 생성
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.paddingBottom = 5;
        toolbar.style.paddingTop = 5;
        toolbar.style.paddingLeft = 5;
        toolbar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

        // 메인 영역: SplitView 생성
        var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        splitView.style.flexGrow = 1;

        // 왼쪽 패널 (리스트) 생성 - 여기서 new를 해줘야 함
        leftList = new ListView();
        leftList.style.flexGrow = 1;
        leftList.makeItem = () => new Label();
        // 바인딩 로직 설정
        leftList.bindItem = (item, index) =>
        {
            if (currentDatabase != null && index < currentDatabase.allQuests.Count)
            {
                var quest = currentDatabase.allQuests[index];
                (item as Label).text = (quest != null) ? quest.id : "[Empty]";
            }
        };
        leftList.selectionType = SelectionType.Single;
        leftList.onSelectionChange += OnQuestSelected;

        // 오른쪽 패널 (상세 정보) 생성
        rightPane = new ScrollView();
        rightPane.style.paddingLeft = 10;
        rightPane.style.paddingRight = 10;
        rightPane.style.paddingTop = 10;
        rightPane.style.paddingBottom = 10;

        // ---------------------------------------------------------
        // 2. UI 계층 구조 조립 (Add)
        // ---------------------------------------------------------

        // Toolbar에 들어갈 Field 생성 (콜백 등록)
        var dbField = new ObjectField("Target Database");
        dbField.objectType = typeof(QuestDatabaseSO);
        dbField.style.flexGrow = 1;
        dbField.RegisterValueChangedCallback(evt =>
        {
            currentDatabase = evt.newValue as QuestDatabaseSO;
            SaveSelection();
            RefreshList();
        });

        toolbar.Add(dbField);
        rootVisualElement.Add(toolbar); // 툴바 추가

        splitView.Add(leftList); // 왼쪽 추가
        splitView.Add(rightPane); // 오른쪽 추가
        rootVisualElement.Add(splitView); // 메인 뷰 추가

        // ---------------------------------------------------------
        // 3. 데이터 로드 및 값 할당 (가장 마지막에!)
        // ---------------------------------------------------------

        // 저장된 DB 경로 불러오기
        LoadSelection();

        // UI가 다 만들어진 상태에서 값을 넣어야 안전함
        if (currentDatabase != null)
        {
            dbField.value = currentDatabase; // 이 시점에서 콜백이 터져도 leftList는 null이 아님
        }

        // 초기화
        RefreshList();
    }

    private void RefreshList()
    {
        // 안전장치: UI가 아직 생성되지 않았으면 무시
        if (leftList == null) return;

        if (currentDatabase != null)
        {
            leftList.itemsSource = currentDatabase.allQuests;
            leftList.Rebuild();
        }
        else
        {
            leftList.itemsSource = new List<QuestInfoSO>();
            leftList.Rebuild();

            // rightPane도 null 체크
            if (rightPane != null)
            {
                rightPane.Clear();
                rightPane.Add(new Label("Please assign a Quest Database SO above.")
                {
                    style = { color = Color.yellow, paddingLeft = 10, paddingTop = 10 }
                });
            }
        }
    }

    private void OnQuestSelected(IEnumerable<object> selectedItems)
    {
        rightPane.Clear();
        foreach (var item in selectedItems)
        {
            QuestInfoSO so = item as QuestInfoSO;
            if (so == null) continue;

            // 1. 기본 인스펙터 그리기 (데이터 표시)
            var editor = Editor.CreateEditor(so);
            var inspector = new InspectorElement(editor);
            rightPane.Add(inspector);

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.justifyContent = Justify.SpaceBetween;
            buttonContainer.style.marginTop = 15;
            buttonContainer.style.marginBottom = 20;

            // 2. 스텝 추가 버튼 (기존)
            var addStepButton = new Button(() => ShowAddStepMenu(so))
            {
                text = "+ Add Step",
                style =
                {
                    flexGrow = 1, height = 30, marginRight = 5,
                    backgroundColor = new Color(0.2f, 0.45f, 0.2f), color = Color.white
                }
            };

            // 3. [NEW] 보상 추가 버튼
            var addRewardButton = new Button(() => ShowAddRewardMenu(so))
            {
                text = "+ Add Reward",
                style =
                {
                    flexGrow = 1, height = 30, marginLeft = 5,
                    backgroundColor = new Color(0.2f, 0.2f, 0.5f), color = Color.white // 파란색
                }
            };

            buttonContainer.Add(addStepButton);
            buttonContainer.Add(addRewardButton);
            rightPane.Add(buttonContainer);
            // =========================================================
            // 3. 런타임 정보 표시 (플레이 모드 시)
            if (Application.isPlaying)
            {
                var runtimeContainer = new VisualElement();
                runtimeContainer.style.borderTopWidth = 1;
                runtimeContainer.style.borderTopColor = Color.gray;
                runtimeContainer.style.paddingTop = 10;

                var runtimeLabel = new Label("--- Runtime State ---")
                {
                    style = { color = Color.green, unityFontStyleAndWeight = FontStyle.Bold }
                };
                runtimeContainer.Add(runtimeLabel);

                var statusLabel = new Label("Waiting for updates...");
                runtimeContainer.Add(statusLabel);

                rightPane.Add(runtimeContainer);

                // 0.5초마다 갱신
                rightPane.schedule.Execute(() =>
                {
                    if (QuestManager.Instance != null)
                    {
                        var q = QuestManager.Instance.GetQuestById(so.id);
                        if (q != null)
                        {
                            statusLabel.text =
                                $"State: {q.State}\nStep Index: {q.CurrentStepIndex}\nStep Data: {q.GetCurrentStep()?.GetState()}";
                        }
                        else
                        {
                            statusLabel.text = "Quest Not Active / Not Started";
                        }
                    }
                }).Every(500);
            }
        }
    }

    // --- 팝업 메뉴 로직 ---

    private void ShowAddStepMenu(QuestInfoSO questInfo)
    {
        GenericMenu menu = new GenericMenu();

        // 1. 프로젝트 내의 모든 'QuestStep' 상속 타입 찾기 (Unity TypeCache 사용 - 빠름)
        var stepTypes = TypeCache.GetTypesDerivedFrom<QuestStep>();

        foreach (var type in stepTypes)
        {
            // 추상 클래스는 생성 불가하므로 제외
            if (type.IsAbstract) continue;

            // 2. [QuestMenu] 어트리뷰트 확인
            var attribute = type.GetCustomAttribute<QuestMenuAttribute>();
            string menuPath;

            if (attribute != null)
            {
                menuPath = attribute.Path;
            }
            else
            {
                // 어트리뷰트 없으면 Uncategorized 폴더에 넣음
                menuPath = "Uncategorized/" + type.Name;
            }

            // 3. 메뉴 아이템 추가
            menu.AddItem(new GUIContent(menuPath), false, () => { AddStepToQuest(questInfo, type); });
        }

        menu.ShowAsContext();
    }

    private void AddStepToQuest(QuestInfoSO questInfo, Type stepType)
    {
        // Undo 등록 (Ctrl+Z 가능하게)
        Undo.RecordObject(questInfo, "Add Quest Step");

        // 리스트가 null이면 초기화
        if (questInfo.steps == null)
            questInfo.steps = new List<QuestStep>();

        // 선택된 타입의 인스턴스 생성 (new Class()와 동일)
        QuestStep newStep = (QuestStep)Activator.CreateInstance(stepType);

        // 리스트에 추가
        questInfo.steps.Add(newStep);

        // 변경 사항 저장 알림
        EditorUtility.SetDirty(questInfo);

        // UI 갱신 (버튼 누른 직후 인스펙터에 바로 반영되게 하기 위함)
        // 현재 선택된 퀘스트를 다시 로드하는 방식으로 리프레시
        OnQuestSelected(new List<object> { questInfo });
    }

    private void ShowAddRewardMenu(QuestInfoSO questInfo)
    {
        GenericMenu menu = new GenericMenu();
        var rewardTypes = TypeCache.GetTypesDerivedFrom<QuestReward>();

        foreach (var type in rewardTypes)
        {
            if (type.IsAbstract) continue;

            var attribute = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<RewardMenuAttribute>(type);
            string menuPath = (attribute != null) ? attribute.Path : "Uncategorized/" + type.Name;

            menu.AddItem(new GUIContent(menuPath), false, () => { AddRewardToQuest(questInfo, type); });
        }

        menu.ShowAsContext();
    }

    private void AddRewardToQuest(QuestInfoSO questInfo, Type rewardType)
    {
        Undo.RecordObject(questInfo, "Add Quest Reward");

        if (questInfo.rewards == null) questInfo.rewards = new List<QuestReward>();

        QuestReward newReward = (QuestReward)Activator.CreateInstance(rewardType);
        questInfo.rewards.Add(newReward);

        EditorUtility.SetDirty(questInfo);
        OnQuestSelected(new List<object> { questInfo }); // UI 갱신
    }
    // --- 저장/로드 로직 ---

    private void SaveSelection()
    {
        if (currentDatabase != null)
        {
            string path = AssetDatabase.GetAssetPath(currentDatabase);
            EditorPrefs.SetString(DB_PREFS_KEY, path);
        }
    }

    private void LoadSelection()
    {
        string path = EditorPrefs.GetString(DB_PREFS_KEY, "");
        if (!string.IsNullOrEmpty(path))
        {
            currentDatabase = AssetDatabase.LoadAssetAtPath<QuestDatabaseSO>(path);
        }
    }
}