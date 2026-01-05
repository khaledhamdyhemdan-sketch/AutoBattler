using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.UI
{
    public class DraftUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset cardTemplate; // Optional: if we want to use a separate UXML for cards

        private VisualElement root;
        private VisualElement cardsContainer;
        private Button rerollButton;
        private Button skipButton;
        private Label rerollCostLabel;

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError("[DraftUIController] No UIDocument found!");
                return;
            }

            root = uiDocument.rootVisualElement;
            InitializeUI();

            // Subscribe to RoguelikeManager events
            if (RoguelikeManager.Instance != null)
            {
                RoguelikeManager.Instance.OnDraftOptionsRefreshed += UpdateCards;
                RoguelikeManager.Instance.OnDraftPhaseEnded += HideUI;
            }
            
            // Listen for phase changes to show UI
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (RoguelikeManager.Instance != null)
            {
                RoguelikeManager.Instance.OnDraftOptionsRefreshed -= UpdateCards;
                RoguelikeManager.Instance.OnDraftPhaseEnded -= HideUI;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void InitializeUI()
        {
            cardsContainer = root.Q<VisualElement>("CardsContainer");
            rerollButton = root.Q<Button>("RerollButton");
            skipButton = root.Q<Button>("SkipButton");
            rerollCostLabel = root.Q<Label>("RerollCostLabel");

            if (rerollButton != null)
                rerollButton.clicked += OnRerollClicked;

            if (skipButton != null)
                skipButton.clicked += OnSkipClicked;

            // Initially hide
            root.style.display = DisplayStyle.None;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Draft)
            {
                ShowUI();
            }
            else
            {
                HideUI();
            }
        }

        private void ShowUI()
        {
            root.style.display = DisplayStyle.Flex;
            UpdateRerollCostDisplay();
        }

        private void HideUI()
        {
            root.style.display = DisplayStyle.None;
        }

        private void UpdateCards(List<EquipmentSO> options)
        {
            cardsContainer.Clear();

            for (int i = 0; i < options.Count; i++)
            {
                EquipmentSO equipment = options[i];
                int index = i; // Capture for lambda

                VisualElement card = CreateCardElement(equipment);
                
                // Add click handler to the select button inside the card
                Button selectBtn = card.Q<Button>(className: "select-button");
                if (selectBtn != null)
                {
                    selectBtn.clicked += () => OnCardSelected(index);
                }

                cardsContainer.Add(card);
            }
        }

        private VisualElement CreateCardElement(EquipmentSO equipment)
        {
            // Programmatic creation of the card visual tree
            // (matching the commented-out template in DraftUI.uxml)

            var card = new VisualElement();
            card.AddToClassList("equipment-card");

            var icon = new VisualElement();
            icon.AddToClassList("card-icon");
            // Set background image if icon exists
            if (equipment.icon != null)
            {
                icon.style.backgroundImage = new StyleBackground(equipment.icon);
            }
            card.Add(icon);

            var nameLabel = new Label(equipment.equipmentName);
            nameLabel.AddToClassList("card-name");
            card.Add(nameLabel);

            var typeLabel = new Label(equipment.category.ToString());
            typeLabel.AddToClassList("card-type");
            card.Add(typeLabel);

            // Format stats string
            string statsText = "";
            if (equipment.bonusDamage > 0) statsText += $"+{equipment.bonusDamage} Damage\n";
            if (equipment.bonusHealth > 0) statsText += $"+{equipment.bonusHealth} Health\n";
            if (equipment.bonusAttackSpeed > 0) statsText += $"+{equipment.bonusAttackSpeed:P0} Atk Spd\n";
            if (equipment.bonusMoveSpeed > 0) statsText += $"+{equipment.bonusMoveSpeed:P0} Mov Spd\n";
            if (equipment.bonusAttackRange > 0) statsText += $"+{equipment.bonusAttackRange} Range\n";

            var statsLabel = new Label(statsText);
            statsLabel.AddToClassList("card-stats");
            card.Add(statsLabel);

            var selectBtn = new Button();
            selectBtn.AddToClassList("select-button");
            
            var btnLabel = new Label("SELECT");
            btnLabel.AddToClassList("select-button-label");
            selectBtn.Add(btnLabel);
            
            card.Add(selectBtn);

            return card;
        }

        private void OnCardSelected(int index)
        {
            RoguelikeManager.Instance?.SelectEquipment(index);
        }

        private void OnRerollClicked()
        {
            RoguelikeManager.Instance?.RerollDraft();
            UpdateRerollCostDisplay();
        }

        private void OnSkipClicked()
        {
            RoguelikeManager.Instance?.SkipDraft();
        }

        private void UpdateRerollCostDisplay()
        {
            if (rerollCostLabel != null && RoguelikeManager.Instance != null)
            {
                rerollCostLabel.text = $"{RoguelikeManager.Instance.RerollCost} Gold";
            }
        }
    }
}
