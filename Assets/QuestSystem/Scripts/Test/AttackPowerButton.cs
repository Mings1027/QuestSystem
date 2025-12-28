using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuestSystem.Scripts.Test
{
    public class AttackPowerButton : MonoBehaviour
    {
        private Button _button;
        private int _currentStatValue;

        [SerializeField] private StatType statType;

        private void Awake()
        {
            _button = GetComponent<Button>();

            _currentStatValue = DBPlayerGameData.Instance.attackPower;
            
            _button.onClick.AddListener(() =>
                QuestEventManager.Instance.StatChanged(statType, _currentStatValue++));
        }
    }
}