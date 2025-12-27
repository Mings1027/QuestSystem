using QuestSystem;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TutorialTarget : MonoBehaviour
{
    public UI_TargetID targetID;

    private void Start()
    {
        var rect = GetComponent<RectTransform>();
        TutorialOverlayManager.Instance.RegisterTarget(targetID, rect);

        // 버튼 클릭 시 이벤트 전송 자동화 (옵션)
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => QuestEventBus.RaiseUIClicked(targetID));
        }
    }
}