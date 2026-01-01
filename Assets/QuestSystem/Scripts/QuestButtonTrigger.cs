using UnityEngine;
using UnityEngine.UI;

public class QuestButtonTrigger : MonoBehaviour
{
    private string _category;
    private string _targetId;

    public void Setup(string category, string targetId)
    {
        _category = category;
        _targetId = targetId;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnButtonClicked);
            btn.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        QuestEventManager.Instance.NotifyEvent(_category, _targetId, 1);
    }
}