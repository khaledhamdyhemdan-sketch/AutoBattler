using UnityEngine;
using System.Collections.Generic;
using AutoBattler.Data;
using AutoBattler.Core.VoidGame;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manager for the Roguelike game mode loop
    /// Orchestrates equipment drafting and lottery rewards
    /// </summary>
    public class RoguelikeManager : MonoBehaviour
    {
        public static RoguelikeManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private EquipmentDatabaseSO equipmentDatabase;
        [SerializeField] private int equipmentDraftCount = 3;
        [SerializeField] private int rerollCost = 2;

        public int RerollCost => rerollCost;

        [Header("Runtime State")]
        private List<EquipmentSO> currentDraftOptions = new List<EquipmentSO>();
        private bool isRoguelikeModeActive = true;
        private bool bossDefeatedThisRound = false;
        private EquipmentSO pendingLotteryReward;

        public event System.Action<List<EquipmentSO>> OnDraftOptionsRefreshed;
        public event System.Action OnDraftPhaseEnded;
        public event System.Action<EquipmentSO> OnLotteryTriggered;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Subscribe to GameManager phase changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnBossDefeated += HandleBossDefeated;
            }
            else
            {
                // Retry in coroutine if CombatManager not ready? 
                // Usually GameManager ensures managers are ready.
                StartCoroutine(SubscribeToCombatManager());
            }
        }

        private System.Collections.IEnumerator SubscribeToCombatManager()
        {
            while (CombatManager.Instance == null) yield return null;
            CombatManager.Instance.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnBossDefeated -= HandleBossDefeated;
            }
        }

        private void HandleBossDefeated(UnitController boss)
        {
            bossDefeatedThisRound = true;
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (!isRoguelikeModeActive) return;

            if (newPhase == GamePhase.Draft)
            {
                StartDraftingPhase();
            }
            else if (newPhase == GamePhase.Results)
            {
                if (bossDefeatedThisRound)
                {
                    TriggerLottery();
                }
            }
            else if (newPhase == GamePhase.Preparation)
            {
                bossDefeatedThisRound = false;
            }
        }

        #region Drafting Phase
        // ... (Existing code) ...
        #endregion

        #region Lottery Phase

        private void TriggerLottery()
        {
            Debug.Log("[RoguelikeManager] Boss Defeated! Triggering Lottery...");
            
            // Pause game progression
            GameManager.Instance.SetAutoProgress(false);

            // Generate high-tier reward
            // Use Min Rarity if possible, or just random
            var rewards = equipmentDatabase.GetRandomEquipment(1, VoidGame.GearRarity.Rare);
            if (rewards.Count == 0) rewards = equipmentDatabase.GetRandomEquipment(1);
            
            if (rewards.Count > 0)
            {
                pendingLotteryReward = rewards[0];
                OnLotteryTriggered?.Invoke(pendingLotteryReward);
            }
            else
            {
                Debug.LogWarning("[RoguelikeManager] No rewards found for Lottery!");
                GameManager.Instance.SetAutoProgress(true); // Fail safe
            }
        }

        public void ClaimLotteryReward()
        {
            if (pendingLotteryReward != null)
            {
                Debug.Log($"[RoguelikeManager] Claimed Lottery Reward: {pendingLotteryReward.equipmentName}");
                ExecuteSmartEquip(pendingLotteryReward);
                pendingLotteryReward = null;
            }

            // Resume game
            GameManager.Instance.SetAutoProgress(true);
            
            // Force transition to next round/prep if timer expired during lottery, 
            // or just let timer finish naturally if we paused it?
            // If we paused auto progress, timer stopped? 
            // GameManager.UpdatePhaseTimer checks autoProgressPhases.
            // So timer paused. We just resume it.
        }

        public EquipmentDatabaseSO GetDatabase() => equipmentDatabase;

        #endregion

        #region Drafting Phase

        public void StartDraftingPhase()
        {
            Debug.Log("[RoguelikeManager] Entering Draft Phase");
            RefreshDraftOptions();
        }

        public void RefreshDraftOptions()
        {
            if (equipmentDatabase == null)
            {
                Debug.LogError("[RoguelikeManager] Equipment Database is missing!");
                return;
            }

            currentDraftOptions = equipmentDatabase.GetRandomEquipment(equipmentDraftCount);
            OnDraftOptionsRefreshed?.Invoke(currentDraftOptions);
        }

        public void RerollDraft()
        {
            if (GameManager.Instance.SpendGold(rerollCost))
            {
                RefreshDraftOptions();
            }
        }

        public void SelectEquipment(int index)
        {
            if (index < 0 || index >= currentDraftOptions.Count) return;

            EquipmentSO selected = currentDraftOptions[index];
            Debug.Log($"[RoguelikeManager] Selected equipment: {selected.equipmentName}");

            // Execute Smart Equip
            ExecuteSmartEquip(selected);

            EndDraftPhase();
        }

        public void SkipDraft()
        {
            Debug.Log("[RoguelikeManager] Draft skipped - Gained 1 gold");
            GameManager.Instance.AddGold(1);
            EndDraftPhase();
        }

        private void ExecuteSmartEquip(EquipmentSO equipment)
        {
            // Get all player units
            var playerUnits = new List<UnitController>(FindObjectsByType<UnitController>(FindObjectsSortMode.None));
            playerUnits.RemoveAll(u => u.Team != UnitTeam.Player);

            if (playerUnits.Count > 0)
            {
                SmartEquipSystem.AutoEquip(equipment, playerUnits);
            }
            else
            {
                Debug.LogWarning("[RoguelikeManager] No player units found to equip item!");
            }
        }

        private void EndDraftPhase()
        {
            OnDraftPhaseEnded?.Invoke();
            
            // Proceed to Combat
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.Combat);
            }
        }

        #endregion

        #region Public API

        public void SetRoguelikeMode(bool active)
        {
            isRoguelikeModeActive = active;
        }

        #endregion
    }
}
