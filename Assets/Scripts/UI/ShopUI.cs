using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.UI;
using DG.Tweening;

namespace AutoBattler.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement poolContainer;
        private Label goldLabel;
        private Button closeButton;

        private List<UnitCardUI> shopCards = new List<UnitCardUI>();

        private readonly string[] humanClasses = new[] { "Vanguard", "Marksman", "Sentinel", "Arcanist", "Medic", "Assassin" };

        private int playerGold = 100;

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            InitializeUI();

            if (AutoBattler.Core.GameManager.Instance != null)
            {
                AutoBattler.Core.GameManager.Instance.OnGoldChanged += OnGoldChanged;
            }
        }

        private void InitializeUI()
        {
            root = uiDocument.rootVisualElement;
            
            // Check if we are part of a larger HUD
            var existingContainer = root.Q<VisualElement>("shop-container");
            
            if (existingContainer != null)
            {
                // We are in the BattleHUD, use the existing container
                poolContainer = existingContainer;
                // We don't need to clear root or add template, just hook up
                // But we might need to instantiate the shop cards structure inside it if it's empty
                // For now, let's assume we just use it as the pool container
            }
            else
            {
                // Standalone mode
                root.Clear();
                var baseSheet = Resources.Load<StyleSheet>("UI/Base");
                if (baseSheet != null) root.styleSheets.Add(baseSheet);
                
                // Apply Theme
                ApplyTheme();

                var shopTemplate = UIFactory.LoadTemplate("UI/Templates/ShopTemplate");
                root.Add(shopTemplate);

                poolContainer = root.Q<VisualElement>("shop-pool-container");
                goldLabel = root.Q<Label>("player-gold");
                closeButton = root.Q<Button>("close-shop");
                
                if (closeButton != null) closeButton.clicked += CloseShop;
                closeButton?.AddToClassList("button-secondary");
                poolContainer?.AddToClassList("glass-panel");
            }

            // Init logic
            if (AutoBattler.Core.GameManager.Instance != null)
            {
                playerGold = AutoBattler.Core.GameManager.Instance.PlayerGold;
            }
            PopulateShop();
            UpdateGoldDisplay();
        }

        private void ApplyTheme()
        {
            var theme = Resources.Load<StyleSheet>("UI/Styles/Theme");
            if (theme != null)
            {
                root.styleSheets.Add(theme);
            }
        }

        private void PopulateShop()
        {
            poolContainer.Clear();
            shopCards.Clear();

            for (int i = 0; i < 8; i++)
            {
                var card = UIFactory.LoadTemplate("UI/Templates/UnitCardTemplate");
                poolContainer.Add(card);

                var unitCard = new UnitCardUI(card, i);
                string className = humanClasses[i % humanClasses.Length];
                unitCard.UpdateFromData(className, 15 + i * 5, className, "Shop class offering", 140 + i * 12, 18 + i * 3);
                unitCard.OnCardClicked += OnBuyCard;
                shopCards.Add(unitCard);
            }
        }

        private void OnBuyCard(UnitCardUI card)
        {
            TryBuyCard(card.Id);
        }

        /// <summary>
        /// Try to buy the card at index; returns true if purchase succeeded.
        /// </summary>
        public bool TryBuyCard(int index)
        {
            if (index < 0 || index >= shopCards.Count) return false;

            var card = shopCards[index];
            var costLabel = card.Root.Q<Label>("unit-cost");
            int cost = 0;
            if (costLabel != null)
            {
                string txt = costLabel.text.Replace("g", "");
                int.TryParse(txt, out cost);
            }

            if (AutoBattler.Core.GameManager.Instance != null)
            {
                bool success = AutoBattler.Core.GameManager.Instance.SpendGold(cost);
                if (success)
                {
                    UpdateGoldDisplay();
                    AutoBattler.UIAnimator.Pop(card.Root, 0.16f);
                    // Brief success pulse on gold label
                    if (goldLabel != null) AutoBattler.UIAnimator.Pulse(goldLabel, 0.24f);
                    // DOTween micro-animation on the UIDocument GameObject for added polish
                    try
                    {
                        var docGO = uiDocument?.gameObject;
                        if (docGO != null)
                        {
                            docGO.transform.DOPunchScale(Vector3.one * 0.04f, 0.28f, vibrato: 8, elasticity: 0.8f);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"DOTween animation failed: {ex.Message}");
                    }
                    Debug.Log($"Shop: Bought {card.Id} for {cost}");
                    return true;
                }
                else
                {
                    // Failure feedback
                    AutoBattler.UIAnimator.AddTemporaryClass(card.Root, "fail", 0.4f);
                    Debug.Log("Shop: Not enough gold");
                    return false;
                }
            }

            Debug.LogWarning("Shop: GameManager not found");
            return false;
        }

        private int ExtractCost(UnitCardUI card)
        {
            // Very small helper: parse cost label ("15g")
            var label = card.Root.Q<Label>("unit-cost");
            if (label == null) return 0;
            string txt = label.text.Replace("g", "");
            if (int.TryParse(txt, out int v)) return v;
            return 0;
        }

        private void UpdateGoldDisplay()
        {
            if (goldLabel != null) goldLabel.text = $"Gold: {playerGold}";
        }

        private void CloseShop()
        {
            gameObject.SetActive(false);
        }



        private void OnDisable()
        {
            if (AutoBattler.Core.GameManager.Instance != null)
            {
                AutoBattler.Core.GameManager.Instance.OnGoldChanged -= OnGoldChanged;
            }
        }

        private void OnGoldChanged(int newGold)
        {
            playerGold = newGold;
            UpdateGoldDisplay();
        }
    }
}