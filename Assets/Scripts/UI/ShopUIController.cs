using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Data;
using AutoBattler.Core;
using System.Collections.Generic;

namespace AutoBattler.UI
{
    /// <summary>
    /// Unity 6.3 UI Toolkit-based Shop UI with SVG integration and custom shaders
    /// Replaces legacy uGUI for performance and scalability
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ShopUIController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset shopSlotTemplate;

        [Header("Settings")]
        [SerializeField] private ShopManager shopManager;

        // UI Elements
        private VisualElement root;
        private VisualElement shopContainer;
        private Button rerollButton;
        private Label goldLabel;
        private Label levelLabel;

        private List<ShopSlotUI> shopSlots = new List<ShopSlotUI>();

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            InitializeUI();
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        #endregion

        #region UI Initialization

        private void InitializeUI()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

            // Fallback: Load Shop UXML if not assigned
            if (uiDocument.visualTreeAsset == null)
            {
                var visualTree = Resources.Load<VisualTreeAsset>("UI/UXML/Shop");
                if (visualTree != null)
                {
                    uiDocument.visualTreeAsset = visualTree;
                }
                else
                {
                    Debug.LogError("[ShopUIController] Could not load Shop.uxml from Resources/UI/UXML/Shop");
                }
            }

            root = uiDocument.rootVisualElement;

            // Query UI elements
            shopContainer = root.Q<VisualElement>("shop-container");
            rerollButton = root.Q<Button>("reroll-button");
            goldLabel = root.Q<Label>("gold-label");
            levelLabel = root.Q<Label>("level-label");

            // Setup reroll button
            if (rerollButton != null)
            {
                rerollButton.clicked += OnRerollClicked;
            }

            // Create shop slots
            CreateShopSlots();

            Debug.Log("[ShopUIController] UI initialized");
        }

        private void CreateShopSlots()
        {
            if (shopSlotTemplate == null)
            {
                shopSlotTemplate = Resources.Load<VisualTreeAsset>("UI/Templates/UnitCardTemplate");
            }

            if (shopContainer == null || shopSlotTemplate == null) return;

            shopSlots.Clear();
            shopContainer.Clear();

            // Create 5 shop slots (standard auto-battler)
            for (int i = 0; i < 5; i++)
            {
                VisualElement slotElement = shopSlotTemplate.CloneTree();
                shopContainer.Add(slotElement);

                ShopSlotUI slot = new ShopSlotUI(slotElement, i);
                slot.OnSlotClicked += OnShopSlotClicked;
                shopSlots.Add(slot);
            }
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            if (shopManager == null) shopManager = ShopManager.Instance;

            if (shopManager != null)
            {
                shopManager.OnShopRefreshed += UpdateShopDisplay;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged += UpdateGoldDisplay;
                GameManager.Instance.OnLevelChanged += UpdateLevelDisplay;
            }
        }

        private void UnbindEvents()
        {
            if (shopManager != null)
            {
                shopManager.OnShopRefreshed -= UpdateShopDisplay;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
                GameManager.Instance.OnLevelChanged -= UpdateLevelDisplay;
            }
        }

        #endregion

        #region UI Updates

        private void UpdateShopDisplay(List<UnitStatsSO> units)
        {
            for (int i = 0; i < shopSlots.Count; i++)
            {
                if (i < units.Count)
                {
                    shopSlots[i].SetUnit(units[i]);
                }
                else
                {
                    shopSlots[i].Clear();
                }
            }
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (goldLabel != null)
            {
                goldLabel.text = $"Gold: {gold}";
            }
        }

        private void UpdateLevelDisplay(int level)
        {
            if (levelLabel != null)
            {
                levelLabel.text = $"Level: {level}";
            }
        }

        #endregion

        #region User Interactions

        private void OnRerollClicked()
        {
            if (shopManager != null)
            {
                shopManager.RerollShop();
            }
        }

        private void OnShopSlotClicked(int slotIndex)
        {
            if (shopManager != null)
            {
                bool success = shopManager.PurchaseUnit(slotIndex);
                
                if (success)
                {
                    // Clear the slot visually
                    if (slotIndex < shopSlots.Count)
                    {
                        shopSlots[slotIndex].Clear();
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a single shop slot in the UI
    /// </summary>
    public class ShopSlotUI
    {
        private VisualElement rootElement;
        private int slotIndex;

        // Child elements
        private VisualElement unitIcon;
        private Label unitNameLabel;
        private Label unitCostLabel;
        private Button buyButton;

        private UnitStatsSO currentUnit;

        public event System.Action<int> OnSlotClicked;

        public ShopSlotUI(VisualElement element, int index)
        {
            rootElement = element;
            slotIndex = index;

            // Query child elements
            unitIcon = rootElement.Q<VisualElement>("unit-icon");
            unitNameLabel = rootElement.Q<Label>("unit-name");
            unitCostLabel = rootElement.Q<Label>("unit-cost");
            buyButton = rootElement.Q<Button>("buy-button");

            // Bind button
            if (buyButton != null)
            {
                buyButton.clicked += () => OnSlotClicked?.Invoke(slotIndex);
            }
        }

        public void SetUnit(UnitStatsSO unit)
        {
            currentUnit = unit;

            if (unit == null)
            {
                Clear();
                return;
            }

            // Update UI
            if (unitNameLabel != null)
            {
                unitNameLabel.text = unit.unitName;
            }

            if (unitCostLabel != null)
            {
                unitCostLabel.text = $"{unit.goldCost}g";
            }

            // Set icon using Unity 6.3 SVG support or sprite
            if (unitIcon != null && unit.icon != null)
            {
                unitIcon.style.backgroundImage = new StyleBackground(unit.icon);
            }

            // Enable buy button
            if (buyButton != null)
            {
                buyButton.SetEnabled(true);
            }

            rootElement.style.display = DisplayStyle.Flex;
        }

        public void Clear()
        {
            currentUnit = null;

            if (unitNameLabel != null) unitNameLabel.text = "";
            if (unitCostLabel != null) unitCostLabel.text = "";
            if (unitIcon != null) unitIcon.style.backgroundImage = null;
            if (buyButton != null) buyButton.SetEnabled(false);

            rootElement.style.display = DisplayStyle.None;
        }
    }
}
