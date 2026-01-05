using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Core;
using System.Collections.Generic;

namespace AutoBattler.UI
{
    /// <summary>
    /// Game HUD UI - Matches "the-void" design system
    /// Uses programmatic UI construction for reliability in Unity 6.3
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameHUDUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        // Colors (matching the-void medieval theme)
        private static readonly Color MedievalGold = new Color(0.77f, 0.63f, 0.35f); // #C5A059
        private static readonly Color Stone900 = new Color(0.11f, 0.10f, 0.09f); // #1c1917
        private static readonly Color Stone800 = new Color(0.16f, 0.14f, 0.14f); // #292524
        private static readonly Color Stone700 = new Color(0.27f, 0.25f, 0.24f); // #44403c
        private static readonly Color Stone500 = new Color(0.47f, 0.44f, 0.42f); // #78716c
        private static readonly Color Stone400 = new Color(0.66f, 0.64f, 0.62f); // #a8a29e
        private static readonly Color Emerald500 = new Color(0.06f, 0.73f, 0.51f); // #10b981
        private static readonly Color BloodDeep = new Color(0.50f, 0.11f, 0.11f); // #7f1d1d

        // UI Elements
        private VisualElement root;
        private Label goldLabel;
        private Label roundLabel;
        private Button speed1xBtn;
        private Button speed2xBtn;
        private Button speed4xBtn;
        private ScrollView characterSidebar;
        private VisualElement inventoryPanel;
        private VisualElement bottomControls;
        private Button battleBtn;
        private Label floorLabel;
        private ScrollView shopList;

        // State
        private string selectedUnitId;
        private float gameSpeed = 1f;

        #region Unity Lifecycle

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnBattleClicked();
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                OnBattleClicked();
            }
#endif
        }

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            BuildUI();
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        #endregion

        #region UI Construction

        private void BuildUI()
        {
            root = uiDocument.rootVisualElement;
            root.Clear();

            // Make root full screen and transparent
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.backgroundColor = Color.clear;
            root.pickingMode = PickingMode.Ignore;

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("UI/GameHUD");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // Build UI sections
            BuildTopBar();
            BuildCharacterSidebar();
            BuildInventoryPanel();
            BuildBottomControls();

            // Initial state
            UpdateSpeedButtons(1f);
            UpdateWarbandList();
            PopulateShop();

            Debug.Log("[GameHUDUI] UI built successfully");
        }

        private void BuildTopBar()
        {
            var topBar = new VisualElement();
            topBar.name = "TopBar";
            topBar.style.position = Position.Absolute;
            topBar.style.top = 0;
            topBar.style.left = 0;
            topBar.style.right = 0;
            topBar.style.paddingTop = 16;
            topBar.style.paddingLeft = 16;
            topBar.style.paddingRight = 16;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.pickingMode = PickingMode.Ignore;

            // Left: Resources Panel
            var resourcePanel = CreateIronPanel();
            resourcePanel.style.flexDirection = FlexDirection.Row;
            resourcePanel.style.alignItems = Align.Center;
            resourcePanel.style.paddingLeft = 12;
            resourcePanel.style.paddingRight = 12;
            resourcePanel.style.paddingTop = 6;
            resourcePanel.style.paddingBottom = 6;

            // Gold
            var goldIcon = new Label("$");
            goldIcon.style.color = MedievalGold;
            goldIcon.style.fontSize = 16;
            goldIcon.style.unityFontStyleAndWeight = FontStyle.Bold;
            resourcePanel.Add(goldIcon);

            goldLabel = new Label("100");
            goldLabel.style.color = Color.white;
            goldLabel.style.fontSize = 24;
            goldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            goldLabel.style.marginLeft = 8;
            resourcePanel.Add(goldLabel);

            // Divider
            var divider = new VisualElement();
            divider.style.width = 1;
            divider.style.height = 20;
            divider.style.backgroundColor = Stone800;
            divider.style.marginLeft = 16;
            divider.style.marginRight = 16;
            resourcePanel.Add(divider);

            // Round
            var roundIcon = new Label("R");
            roundIcon.style.color = Stone500;
            roundIcon.style.fontSize = 14;
            resourcePanel.Add(roundIcon);

            roundLabel = new Label("1");
            roundLabel.style.color = MedievalGold;
            roundLabel.style.fontSize = 16;
            roundLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            roundLabel.style.marginLeft = 8;
            resourcePanel.Add(roundLabel);

            topBar.Add(resourcePanel);

            // Right: Speed Controls
            var controlsRow = new VisualElement();
            controlsRow.style.flexDirection = FlexDirection.Row;

            var speedPanel = CreateIronPanel();
            speedPanel.style.flexDirection = FlexDirection.Row;
            speedPanel.style.paddingLeft = 4;
            speedPanel.style.paddingRight = 4;
            speedPanel.style.paddingTop = 4;
            speedPanel.style.paddingBottom = 4;

            speed1xBtn = CreateSpeedButton("1x");
            speed1xBtn.clicked += () => SetGameSpeed(1f);
            speedPanel.Add(speed1xBtn);

            speed2xBtn = CreateSpeedButton("2x");
            speed2xBtn.clicked += () => SetGameSpeed(2f);
            speedPanel.Add(speed2xBtn);

            speed4xBtn = CreateSpeedButton("4x");
            speed4xBtn.clicked += () => SetGameSpeed(4f);
            speedPanel.Add(speed4xBtn);

            controlsRow.Add(speedPanel);

            var settingsBtn = CreateSpeedButton("S");
            settingsBtn.style.marginLeft = 8;
            settingsBtn.clicked += OnSettingsClicked;
            controlsRow.Add(settingsBtn);

            topBar.Add(controlsRow);
            root.Add(topBar);
        }

        private void BuildCharacterSidebar()
        {
            characterSidebar = new ScrollView();
            characterSidebar.name = "CharacterSidebar";
            characterSidebar.style.position = Position.Absolute;
            characterSidebar.style.top = 80;
            characterSidebar.style.left = 16;
            characterSidebar.style.width = 200;
            characterSidebar.style.maxHeight = 400;
            characterSidebar.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            root.Add(characterSidebar);
        }

        private void BuildInventoryPanel()
        {
            inventoryPanel = new VisualElement();
            inventoryPanel.name = "InventoryPanel";
            inventoryPanel.style.position = Position.Absolute;
            inventoryPanel.style.top = 80;
            inventoryPanel.style.right = 16;
            inventoryPanel.style.width = 288;
            inventoryPanel.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            inventoryPanel.style.borderTopWidth = 2;
            inventoryPanel.style.borderBottomWidth = 2;
            inventoryPanel.style.borderLeftWidth = 2;
            inventoryPanel.style.borderRightWidth = 2;
            inventoryPanel.style.borderTopColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            inventoryPanel.style.borderBottomColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            inventoryPanel.style.borderLeftColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            inventoryPanel.style.borderRightColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            inventoryPanel.style.borderTopLeftRadius = 12;
            inventoryPanel.style.borderTopRightRadius = 12;
            inventoryPanel.style.borderBottomLeftRadius = 12;
            inventoryPanel.style.borderBottomRightRadius = 12;
            inventoryPanel.style.display = DisplayStyle.None; // Hidden by default
            root.Add(inventoryPanel);
        }

        private void BuildBottomControls()
        {
            bottomControls = new VisualElement();
            bottomControls.name = "BottomControls";
            bottomControls.style.position = Position.Absolute;
            bottomControls.style.bottom = 0;
            bottomControls.style.left = 0;
            bottomControls.style.right = 0;
            bottomControls.style.paddingBottom = 24;
            bottomControls.style.flexDirection = FlexDirection.Row;
            bottomControls.style.justifyContent = Justify.Center;
            bottomControls.style.alignItems = Align.FlexEnd;
            bottomControls.pickingMode = PickingMode.Ignore;

            var shopContainer = new VisualElement();
            shopContainer.style.flexDirection = FlexDirection.Row;
            shopContainer.style.alignItems = Align.FlexEnd;

            // Battle Button
            battleBtn = new Button();
            battleBtn.style.width = 224;
            battleBtn.style.height = 96;
            battleBtn.style.backgroundColor = BloodDeep;
            battleBtn.style.borderTopWidth = 4;
            battleBtn.style.borderBottomWidth = 4;
            battleBtn.style.borderLeftWidth = 4;
            battleBtn.style.borderRightWidth = 4;
            battleBtn.style.borderTopColor = Color.black;
            battleBtn.style.borderBottomColor = Color.black;
            battleBtn.style.borderLeftColor = Color.black;
            battleBtn.style.borderRightColor = Color.black;
            battleBtn.style.borderTopLeftRadius = 12;
            battleBtn.style.borderTopRightRadius = 12;
            battleBtn.style.borderBottomLeftRadius = 12;
            battleBtn.style.borderBottomRightRadius = 12;
            battleBtn.style.marginLeft = 24;
            battleBtn.style.justifyContent = Justify.Center;
            battleBtn.style.alignItems = Align.Center;
            battleBtn.style.flexDirection = FlexDirection.Column;
            battleBtn.clicked += OnBattleClicked;

            var battleText = new Label("BATTLE");
            battleText.style.color = Color.white;
            battleText.style.fontSize = 24;
            battleText.style.unityFontStyleAndWeight = FontStyle.Bold;
            battleBtn.Add(battleText);

            floorLabel = new Label("FLOOR 1");
            floorLabel.style.color = new Color(0.6f, 0.1f, 0.1f);
            floorLabel.style.fontSize = 12;
            floorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            battleBtn.Add(floorLabel);

            shopContainer.Add(battleBtn);
            bottomControls.Add(shopContainer);
            root.Add(bottomControls);
        }

        #endregion

        #region Helper Methods

        private VisualElement CreateIronPanel()
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            panel.style.borderTopWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderTopColor = Stone700;
            panel.style.borderBottomColor = Stone700;
            panel.style.borderLeftColor = Stone700;
            panel.style.borderRightColor = Stone700;
            panel.style.borderTopLeftRadius = 8;
            panel.style.borderTopRightRadius = 8;
            panel.style.borderBottomLeftRadius = 8;
            panel.style.borderBottomRightRadius = 8;
            return panel;
        }

        private Button CreateSpeedButton(string text)
        {
            var btn = new Button();
            btn.text = text;
            btn.style.width = 40;
            btn.style.height = 40;
            btn.style.backgroundColor = new Color(0, 0, 0, 0.4f);
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = Stone800;
            btn.style.borderBottomColor = Stone800;
            btn.style.borderLeftColor = Stone800;
            btn.style.borderRightColor = Stone800;
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
            btn.style.color = Stone500;
            btn.style.marginRight = 4;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            return btn;
        }

        private VisualElement CreateShopItem(string name, string type, int cost, int index)
        {
            var item = CreateIronPanel();
            item.style.width = 180;
            item.style.height = 96;
            item.style.paddingLeft = 10;
            item.style.paddingRight = 10;
            item.style.paddingTop = 8;
            item.style.paddingBottom = 8;
            item.style.marginLeft = 8;
            item.style.justifyContent = Justify.SpaceBetween;
            item.RegisterCallback<ClickEvent>(e => OnBuyUnit(index));

            var nameLabel = new Label(name);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 14;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            item.Add(nameLabel);

            var typeLabel = new Label(type.ToUpper());
            typeLabel.style.color = Stone500;
            typeLabel.style.fontSize = 10;
            item.Add(typeLabel);

            var costRow = new VisualElement();
            costRow.style.flexDirection = FlexDirection.Row;
            costRow.style.justifyContent = Justify.SpaceBetween;
            costRow.style.marginTop = 8;

            var costLabel = new Label($"$ {cost}");
            costLabel.style.color = MedievalGold;
            costLabel.style.fontSize = 12;
            costLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            costRow.Add(costLabel);

            var buyLabel = new Label("BUY");
            buyLabel.style.color = Stone400;
            buyLabel.style.fontSize = 10;
            costRow.Add(buyLabel);

            item.Add(costRow);

            return item;
        }

        private VisualElement CreateUnitCard(UnitController unit)
        {
            var card = new Button();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingLeft = 6;
            card.style.paddingRight = 6;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.marginBottom = 6;
            card.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = Stone900;
            card.style.borderBottomColor = Stone900;
            card.style.borderLeftColor = Stone900;
            card.style.borderRightColor = Stone900;
            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;
            card.clicked += () => SelectUnit(unit.GetInstanceID().ToString());

            // Icon
            var icon = new VisualElement();
            icon.style.width = 36;
            icon.style.height = 36;
            icon.style.backgroundColor = new Color(0.05f, 0.04f, 0.04f, 1f);
            icon.style.borderTopWidth = 1;
            icon.style.borderBottomWidth = 1;
            icon.style.borderLeftWidth = 1;
            icon.style.borderRightWidth = 1;
            icon.style.borderTopColor = Stone800;
            icon.style.borderBottomColor = Stone800;
            icon.style.borderLeftColor = Stone800;
            icon.style.borderRightColor = Stone800;
            icon.style.borderTopLeftRadius = 6;
            icon.style.borderTopRightRadius = 6;
            icon.style.borderBottomLeftRadius = 6;
            icon.style.borderBottomRightRadius = 6;
            icon.style.marginRight = 8;
            icon.style.justifyContent = Justify.Center;
            icon.style.alignItems = Align.Center;

            var iconLabel = new Label(unit.UnitStats.unitName.Substring(0, 1));
            iconLabel.style.color = Color.white;
            iconLabel.style.fontSize = 16;
            iconLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            icon.Add(iconLabel);
            card.Add(icon);

            // Info
            var info = new VisualElement();
            info.style.flexDirection = FlexDirection.Column;

            var nameLabel = new Label(unit.UnitStats.unitName);
            nameLabel.style.color = Stone400;
            nameLabel.style.fontSize = 11;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            info.Add(nameLabel);

            // HP Bar
            var hpBg = new VisualElement();
            hpBg.style.width = 80;
            hpBg.style.height = 4;
            hpBg.style.backgroundColor = Color.black;
            hpBg.style.borderTopLeftRadius = 2;
            hpBg.style.borderTopRightRadius = 2;
            hpBg.style.borderBottomLeftRadius = 2;
            hpBg.style.borderBottomRightRadius = 2;
            hpBg.style.marginTop = 4;

            float hpPercent = (float)unit.CurrentHealth / unit.MaxHealth;
            var hpFill = new VisualElement();
            hpFill.style.height = Length.Percent(100);
            hpFill.style.width = Length.Percent(hpPercent * 100);
            hpFill.style.backgroundColor = Emerald500;
            hpBg.Add(hpFill);

            info.Add(hpBg);
            card.Add(info);

            return card;
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
                GameManager.Instance.OnRoundChanged += OnRoundChanged;
                GameManager.Instance.OnGoldChanged += OnGoldChanged;

                // Initial values
                if (goldLabel != null) goldLabel.text = GameManager.Instance.PlayerGold.ToString();
                if (roundLabel != null) roundLabel.text = (GameManager.Instance.CurrentRound + 1).ToString();
            }

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnUnitCountsChanged += (p, e) => UpdateWarbandList();
            }
        }

        private void UnbindEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnRoundChanged -= OnRoundChanged;
                GameManager.Instance.OnGoldChanged -= OnGoldChanged;
            }
        }

        #endregion

        #region Updates

        private void OnPhaseChanged(GamePhase phase)
        {
            bool isPlanning = phase == GamePhase.Preparation;

            if (bottomControls != null)
            {
                bottomControls.style.display = isPlanning ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!isPlanning) SelectUnit(null);
            UpdateWarbandList();
        }

        private void OnRoundChanged(int round)
        {
            if (roundLabel != null) roundLabel.text = (round + 1).ToString();
            if (floorLabel != null) floorLabel.text = $"FLOOR {round + 1}";
        }

        private void OnGoldChanged(int gold)
        {
            if (goldLabel != null) goldLabel.text = gold.ToString();
        }

        private void SetGameSpeed(float speed)
        {
            gameSpeed = speed;
            Time.timeScale = speed;
            UpdateSpeedButtons(speed);
        }

        private void UpdateSpeedButtons(float speed)
        {
            SetSpeedButtonActive(speed1xBtn, speed == 1f);
            SetSpeedButtonActive(speed2xBtn, speed == 2f);
            SetSpeedButtonActive(speed4xBtn, speed == 4f);
        }

        private void SetSpeedButtonActive(Button btn, bool active)
        {
            if (btn == null) return;
            btn.style.backgroundColor = active ? MedievalGold : new Color(0, 0, 0, 0.4f);
            btn.style.color = active ? Color.black : Stone500;
        }

        public void UpdateWarbandList()
        {
            if (characterSidebar == null) return;
            characterSidebar.Clear();

            if (CombatManager.Instance == null) return;

            var units = CombatManager.Instance.GetPlayerUnits();
            foreach (var unit in units)
            {
                if (unit != null)
                {
                    var card = CreateUnitCard(unit);
                    characterSidebar.Add(card);
                }
            }
        }

        private void PopulateShop()
        {
            // Shop is now handled by ShopUIController
            if (shopList != null)
            {
                shopList.Clear();
                shopList.style.display = DisplayStyle.None;
            }
        }

        private void SelectUnit(string unitId)
        {
            selectedUnitId = unitId;
            if (inventoryPanel != null)
            {
                inventoryPanel.style.display = string.IsNullOrEmpty(unitId) ? DisplayStyle.None : DisplayStyle.Flex;
                if (!string.IsNullOrEmpty(unitId))
                {
                    UpdateInventoryPanel(unitId);
                }
            }
        }

        private void UpdateInventoryPanel(string unitId)
        {
            inventoryPanel.Clear();
            // Find unit
            if (CombatManager.Instance == null) return;
            
            var unit = CombatManager.Instance.GetPlayerUnits().Find(u => u.GetInstanceID().ToString() == unitId);
            if (unit == null) return;

            var header = new Label($"{unit.UnitStats.unitName} Inventory");
            header.style.color = MedievalGold;
            header.style.fontSize = 18;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            header.style.alignSelf = Align.Center;
            inventoryPanel.Add(header);

            var itemsContainer = new VisualElement();
            itemsContainer.style.flexDirection = FlexDirection.Row;
            itemsContainer.style.flexWrap = Wrap.Wrap;
            itemsContainer.style.justifyContent = Justify.Center;
            inventoryPanel.Add(itemsContainer);

            foreach (var item in unit.GetEquippedItems())
            {
                var itemSlot = CreateIronPanel();
                itemSlot.style.width = 60;
                itemSlot.style.height = 60;
                itemSlot.style.marginRight = 5;
                itemSlot.style.marginBottom = 5;
                itemSlot.style.justifyContent = Justify.Center;
                itemSlot.style.alignItems = Align.Center;

                if (item != null)
                {
                    var itemName = new Label(item.itemName);
                    itemName.style.fontSize = 10;
                    itemName.style.color = Color.white;
                    itemName.style.whiteSpace = WhiteSpace.Normal;
                    itemName.style.unityTextAlign = TextAnchor.MiddleCenter;
                    itemSlot.Add(itemName);
                }
                else
                {
                    var emptyLabel = new Label("Empty");
                    emptyLabel.style.fontSize = 10;
                    emptyLabel.style.color = Stone500;
                    itemSlot.Add(emptyLabel);
                }
                itemsContainer.Add(itemSlot);
            }
        }

        #endregion

        #region Actions

        private void OnSettingsClicked()
        {
            Debug.Log("[GameHUDUI] Settings clicked");
        }

        private void OnRerollClicked()
        {
            Debug.Log("[GameHUDUI] Reroll clicked");
            if (ShopManager.Instance != null && GameManager.Instance != null)
            {
                int cost = 2;
                if (GameManager.Instance.SpendGold(cost))
                {
                    ShopManager.Instance.RefreshShop(GameManager.Instance.PlayerLevel);
                    PopulateShop();
                }
            }
        }

        private void OnBuyUnit(int index)
        {
            Debug.Log($"[GameHUDUI] Buy unit at index {index}");
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.PurchaseUnit(index);
                PopulateShop();
                UpdateWarbandList();
            }
        }

        private void OnBattleClicked()
        {
            Debug.Log("[GameHUDUI] Battle clicked");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.Combat);
            }
        }

        #endregion
    }
}
