using System;
using UnityEngine;

public class QuestEvents
{
    public event Action<string> onStartQuest;
    public void StartQuest(string id)
    {
        if (onStartQuest != null)
        {
            onStartQuest(id);
            Debug.Log("Start Quest: " + id + "");
        }
    }

    public event Action<string> onAdvanceQuest;
    public void AdvanceQuest(string id)
    {
        if (onAdvanceQuest != null)
        {
            onAdvanceQuest(id);
            Debug.Log("Advance Quest: " + id + "");
        }
    }

    public event Action<string> onFinishQuest;
    public void FinishQuest(string id)
    {
        if (onFinishQuest != null)
        {
            onFinishQuest(id);
            Debug.Log("Finish Quest: " + id + "");
        }
    }

    public event Action<Quest> onQuestStateChange;
    public void QuestStateChange(Quest quest)
    {
        if (onQuestStateChange != null)
        {
            onQuestStateChange(quest);
            Debug.Log($"Quest State Change: {quest.info.ID}");
        }
    }

    public event Action<string, int, QuestStepState> onQuestStepStateChange;
    public void QuestStepStateChange(string id, int stepIndex, QuestStepState questStepState)
    {
        if (onQuestStepStateChange != null)
        {
            onQuestStepStateChange(id, stepIndex, questStepState);
            Debug.Log($"Quest Step State Change: {id} / {stepIndex} / {questStepState.state}");
        }
    }
}