using UnityEngine;
using System;

public abstract class QuestStepDataSO : ScriptableObject
{
    // 런타임에 필요한 컴포넌트 타입을 반환
    public abstract Type GetQuestStepType();

    // [핵심] 에디터에서 "이 퀘스트(ID)가 너를 몇 번째(Index)에서 쓰고 있어"라고 알려주는 함수
    public abstract void SyncQuestData(string questId, int questIndex);
}