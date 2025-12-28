using System;
using UnityEngine;

public class QuestEventManager : MonoBehaviour
{
    public static QuestEventManager instance { get; private set; }

    public QuestEvents questEvents { get; private set; }

    public void Init()
    {
        if (instance != null)
        {
            Debug.LogError("More than one QuestEventManager in the scene!");
        }

        instance = this;

        questEvents = new QuestEvents();
    }
}