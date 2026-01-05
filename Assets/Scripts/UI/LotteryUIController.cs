using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.UI
{
    public class LotteryUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private float spinDuration = 2.0f;
        
        private VisualElement root;
        private VisualElement slotIcon;
        private Label itemNameLabel;
        private Label itemRarityLabel;
        private Button claimButton;

        private EquipmentSO finalReward;
        private bool isSpinning = false;

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[LotteryUIController] No UIDocument found!");
                return;
            }

            root = uiDocument.rootVisualElement;
            InitializeUI();

            // Subscribe to RoguelikeManager events
            if (RoguelikeManager.Instance != null)
            {
                RoguelikeManager.Instance.OnLotteryTriggered += StartLottery;
            }
        }

        private void OnDisable()
        {
            if (RoguelikeManager.Instance != null)
            {
                RoguelikeManager.Instance.OnLotteryTriggered -= StartLottery;
            }
        }

        private void InitializeUI()
        {
            slotIcon = root.Q<VisualElement>("SlotIcon");
            itemNameLabel = root.Q<Label>("ItemName");
            itemRarityLabel = root.Q<Label>("ItemRarity");
            claimButton = root.Q<Button>("ClaimButton");

            if (claimButton != null)
            {
                claimButton.clicked += OnClaimClicked;
                claimButton.SetEnabled(false); // Disable until spin finishes
            }

            // Initially hide
            root.style.display = DisplayStyle.None;
        }

        private void StartLottery(EquipmentSO reward)
        {
            finalReward = reward;
            root.style.display = DisplayStyle.Flex;
            claimButton.SetEnabled(false);
            
            StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            isSpinning = true;
            float timer = 0f;
            float interval = 0.1f;
            
            // Get some dummy icons to cycle through if possible, 
            // or just use the reward icon but flicker it?
            // Better to cycle random equipment from DB if available.
            // We'll access DB via RoguelikeManager if possible, or just flicker.
            
            var db = RoguelikeManager.Instance?.GetDatabase();
            var randomItems = db != null ? db.GetRandomEquipment(10) : new System.Collections.Generic.List<EquipmentSO>();

            while (timer < spinDuration)
            {
                // Show random item
                if (randomItems.Count > 0)
                {
                    var item = randomItems[Random.Range(0, randomItems.Count)];
                    UpdateVisuals(item);
                }
                else
                {
                    // Fallback if no DB access
                    itemNameLabel.text = "Spinning...";
                }

                yield return new WaitForSeconds(interval);
                timer += interval;
                
                // Slow down
                if (timer > spinDuration * 0.7f) interval *= 1.2f;
            }

            // Show final reward
            UpdateVisuals(finalReward);
            isSpinning = false;
            claimButton.SetEnabled(true);
            
            // Play sound?
        }

        private void UpdateVisuals(EquipmentSO item)
        {
            if (item == null) return;
            
            if (item.icon != null)
                slotIcon.style.backgroundImage = new StyleBackground(item.icon);
            
            itemNameLabel.text = item.equipmentName;
            itemRarityLabel.text = item.rarity.ToString();
            
            // Color code rarity
            // Simple logic
            itemRarityLabel.style.color = GetRarityColor(item.rarity);
        }

        private Color GetRarityColor(AutoBattler.Core.VoidGame.GearRarity rarity)
        {
            switch (rarity)
            {
                case AutoBattler.Core.VoidGame.GearRarity.Common: return Color.gray;
                case AutoBattler.Core.VoidGame.GearRarity.Rare: return Color.blue;
                case AutoBattler.Core.VoidGame.GearRarity.Legendary: return new Color(1f, 0.5f, 0f); // Orange
                default: return Color.white;
            }
        }

        private void OnClaimClicked()
        {
            if (isSpinning) return;

            RoguelikeManager.Instance?.ClaimLotteryReward();
            root.style.display = DisplayStyle.None;
        }
    }
}
