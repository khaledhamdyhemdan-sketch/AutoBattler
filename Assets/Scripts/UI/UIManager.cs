using UnityEngine;
using TMPro;
using AutoBattler.Core;

namespace AutoBattler.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Elements")]
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI stateText;
        public TextMeshProUGUI roundText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (GameManager.Instance != null)
            {
                stateText.text = $"State: {GameManager.Instance.CurrentPhase}";
                roundText.text = $"Round: {GameManager.Instance.CurrentRound}";
                timerText.text = $"Time: {Mathf.CeilToInt(GameManager.Instance.PhaseTimer)}";
            }
        }
    }
}
