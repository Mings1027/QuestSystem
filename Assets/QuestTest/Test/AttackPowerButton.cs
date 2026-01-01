using System;
using UnityEngine;
using UnityEngine.UI;

public class AttackPowerButton : MonoBehaviour
{
    private Button _button;

    [SerializeField] private StatType statType;

    private void Awake()
    {
        _button = GetComponent<Button>();

        _button.onClick.AddListener(() =>
        {
            // 1. 실제 DB 데이터를 먼저 변경합니다.
            if (statType == StatType.Attack)
            {
                DBPlayerGameData.Instance.attackPower++;
            }

            // 2. 변경된 '최종 결과값'을 이벤트로 쏩니다.
            // Replace 방식은 이 값을 그대로 퀘스트 수치로 사용합니다.
            int newValue = DBPlayerGameData.Instance.attackPower;
            QuestEventManager.Instance.StatChanged(statType, newValue);

            Debug.Log($"[Replace] 현재 공격력: {newValue}");
        });
    }
}