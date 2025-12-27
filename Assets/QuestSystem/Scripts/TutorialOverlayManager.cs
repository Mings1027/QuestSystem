using System.Collections.Generic;
using QuestSystem;
using UnityEngine;

public class TutorialOverlayManager : MonoBehaviour
{
    public static TutorialOverlayManager Instance;
    
    [SerializeField] private GameObject pingIconPrefab;
    private Dictionary<UI_TargetID, RectTransform> targets = new Dictionary<UI_TargetID, RectTransform>();
    private GameObject currentPingInstance;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterTarget(UI_TargetID id, RectTransform rect)
    {
        if (!targets.ContainsKey(id)) targets.Add(id, rect);
    }

    public void ShowPing(UI_TargetID id)
    {
        if (targets.TryGetValue(id, out RectTransform targetRect))
        {
            if (currentPingInstance == null)
                currentPingInstance = Instantiate(pingIconPrefab, transform); // Canvas 자식으로 생성

            currentPingInstance.SetActive(true);
            currentPingInstance.transform.position = targetRect.position;
        }
    }

    public void HidePing()
    {
        if (currentPingInstance != null) currentPingInstance.SetActive(false);
    }
}