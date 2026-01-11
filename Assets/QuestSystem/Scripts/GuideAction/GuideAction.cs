using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[AttributeUsage(AttributeTargets.Class)]
public class GuideActionCategoryAttribute : Attribute
{
    public string MenuPath; // 예: "UI/핑 찍기"
    public GuideActionCategoryAttribute(string menuPath)
    {
        MenuPath = menuPath;
    }
}

[Serializable]
public abstract class GuideAction
{
    public abstract UniTask Execute(CancellationToken token);
}

// 1. 핑 찍고 클릭 대기
[Serializable]
[GuideActionCategory("UI/핑 찍기")]
public class GuideAction_Ping : GuideAction
{
    public string description;
    public RectTransform targetUI;
    public bool waitForClick = true;

    public override async UniTask Execute(CancellationToken cancellationToken)
    {
        var manager = QuestSceneManager.Instance;
        
        if (targetUI == null) return;

        manager.ShowPing(targetUI);

        if (waitForClick)
        {
            Button btn = targetUI.GetComponent<Button>();
            if (btn != null)
            {
                // UniTask의 강력한 기능: 버튼 클릭을 await로 기다림
                // 토큰이 취소되면(가이드 중단 시) 여기서 즉시 멈춤
                await btn.OnClickAsync(cancellationToken);
            }
            else
            {
                Debug.LogWarning($"[Guide] {targetUI.name}은 버튼이 아닙니다. 클릭 대기를 스킵합니다.");
            }
        }
        
        // 클릭 후 핑 숨기는 건 다음 액션이나 종료 시 처리되지만,
        // 원한다면 여기서 숨겨도 됩니다.
        // manager.HidePing(); 
    }
}

// 2. 대사 출력 (예시)
[Serializable]
[GuideActionCategory("연출/대화 출력")]
public class GuideAction_Dialogue : GuideAction
{
    [TextArea] public string text;
    public float duration = 2.0f;

    public override async UniTask Execute(CancellationToken cancellationToken)
    {
        Debug.Log($"[Guide Dialogue] {text}");
        
        // 코루틴 대신 UniTask.Delay 사용
        // DelayType.Realtime으로 하면 타임스케일 0이어도 작동
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);
    }
}

[Serializable]
[GuideActionCategory("이벤트")]
public class GuideAction_Event : GuideAction
{
    public UnityEngine.Events.UnityEvent onExecute;

    public override async UniTask Execute(CancellationToken cancellationToken)
    {
        onExecute?.Invoke();
        await UniTask.CompletedTask; // 동기 로직은 즉시 완료 처리
    }
}
