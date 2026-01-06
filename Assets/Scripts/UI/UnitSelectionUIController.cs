using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Data;
using AutoBattler.Core;
using System.Collections.Generic;

namespace AutoBattler.UI
{
    /// <summary>
    /// Unit Selection UI - The Void style with medieval iron/gold aesthetic
    /// Programmatic UI construction for Unity 6.3 reliability
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UnitSelectionUIController : MonoBehaviour
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
        private static readonly Color BloodDeep = new Color(0.50f, 0.11f, 0.11f); // #7f1d1d
        private static readonly Color DangerRed = new Color(0.94f, 0.27f, 0.27f); // #ef4444
        private static readonly Color Emerald = new Color(0.06f, 0.73f, 0.51f); // #10b981

        // UI Elements
        private VisualElement root;
        private VisualElement poolContainer;
        private VisualElement squadContainer;
        private Button startBattleButton;
        private Button clearButton;
        private Button backButton;
        private Label squadCountLabel;
        private Label totalHpLabel;
        private Label totalDmgLabel;

        // Data
        private readonly string[] unitClasses = { "Vanguard", "Marksman", "Sentinel", "Arcanist", "Medic", "Assassin", "Shock Trooper", "Guardian" };
        private readonly Dictionary<string, string> classRoles = new()
        {
            { "Vanguard", "Frontline Tank" },
            { "Marksman", "Ranged DPS" },
            { "Sentinel", "Shield Wall" },
            { "Arcanist", "AOE Caster" },
            { "Medic", "Healing Support" },
            { "Assassin", "Burst Finisher" },
            { "Shock Trooper", "Bruiser" },
            { "Guardian", "Team Barrier" },
        };

        private List<int> selectedIndices = new List<int>();

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            
            // Initialize the unit pool in the manager
            if (UnitSelectionManager.Instance != null)
            {
                UnitSelectionManager.Instance.RefreshAvailableUnits();
                Debug.Log("[UnitSelectionUI] Refreshed available units in manager");
            }
            
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

            // Root styling
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            root.style.flexDirection = FlexDirection.Column;

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("UI/UnitSelection");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // Build sections
            BuildHeader();
            BuildMainContent();
            BuildFooter();

            // Initial population
            PopulatePool();
            UpdateSquadStats();

            Debug.Log("[UnitSelectionUI] UI built successfully");
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 48;
            header.style.paddingRight = 48;
            header.style.paddingTop = 24;
            header.style.paddingBottom = 24;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Stone800;

            // Left - Title
            var headerLeft = new VisualElement();
            headerLeft.style.flexDirection = FlexDirection.Column;

            var title = new Label("SELECT YOUR WARBAND");
            title.style.fontSize = 32;
            title.style.color = MedievalGold;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 4;
            headerLeft.Add(title);

            var subtitle = new Label("Choose wisely. Your fate depends on it.");
            subtitle.style.fontSize = 14;
            subtitle.style.color = Stone500;
            subtitle.style.letterSpacing = 2;
            subtitle.style.marginTop = 4;
            headerLeft.Add(subtitle);

            header.Add(headerLeft);

            // Right - Gold display
            var goldPanel = CreateIronPanel();
            goldPanel.style.flexDirection = FlexDirection.Row;
            goldPanel.style.alignItems = Align.Center;
            goldPanel.style.paddingLeft = 16;
            goldPanel.style.paddingRight = 16;
            goldPanel.style.paddingTop = 8;
            goldPanel.style.paddingBottom = 8;

            var goldIcon = new Label("$");
            goldIcon.style.fontSize = 18;
            goldIcon.style.color = MedievalGold;
            goldIcon.style.marginRight = 8;
            goldPanel.Add(goldIcon);

            var goldValue = new Label("100");
            goldValue.style.fontSize = 20;
            goldValue.style.color = Color.white;
            goldValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            goldPanel.Add(goldValue);

            header.Add(goldPanel);
            root.Add(header);
        }

        private void BuildMainContent()
        {
            var mainContent = new VisualElement();
            mainContent.style.flexGrow = 1;
            mainContent.style.flexDirection = FlexDirection.Row;
            mainContent.style.paddingLeft = 48;
            mainContent.style.paddingRight = 48;
            mainContent.style.paddingTop = 24;
            mainContent.style.paddingBottom = 24;

            // Pool Section (Left - larger)
            var poolSection = CreateSection("AVAILABLE UNITS", "8 Units");
            poolSection.style.flexGrow = 2;
            poolSection.style.marginRight = 24;

            var poolScroll = new ScrollView();
            poolScroll.style.flexGrow = 1;

            poolContainer = new VisualElement();
            poolContainer.style.flexDirection = FlexDirection.Row;
            poolContainer.style.flexWrap = Wrap.Wrap;
            poolContainer.style.paddingLeft = 16;
            poolContainer.style.paddingRight = 16;
            poolContainer.style.paddingTop = 16;
            poolContainer.style.paddingBottom = 16;

            poolScroll.Add(poolContainer);
            poolSection.Add(poolScroll);
            mainContent.Add(poolSection);

            // Squad Section (Right - smaller)
            var squadSection = CreateSection("YOUR SQUAD", "0 / 5");
            squadSection.style.flexGrow = 1;
            squadSection.style.minWidth = 300;

            squadCountLabel = squadSection.Q<Label>("section-count");

            var squadScroll = new ScrollView();
            squadScroll.style.flexGrow = 1;

            squadContainer = new VisualElement();
            squadContainer.style.flexDirection = FlexDirection.Column;
            squadContainer.style.paddingLeft = 16;
            squadContainer.style.paddingRight = 16;
            squadContainer.style.paddingTop = 16;
            squadContainer.style.paddingBottom = 16;

            squadScroll.Add(squadContainer);
            squadSection.Add(squadScroll);

            // Squad Stats
            var statsPanel = new VisualElement();
            statsPanel.style.flexDirection = FlexDirection.Row;
            statsPanel.style.justifyContent = Justify.SpaceAround;
            statsPanel.style.paddingTop = 16;
            statsPanel.style.paddingBottom = 16;
            statsPanel.style.borderTopWidth = 1;
            statsPanel.style.borderTopColor = Stone800;
            statsPanel.style.backgroundColor = new Color(0, 0, 0, 0.3f);

            var hpStat = CreateStatItem("TOTAL HP", "0", Emerald);
            totalHpLabel = hpStat.Q<Label>("stat-value");
            statsPanel.Add(hpStat);

            var dmgStat = CreateStatItem("TOTAL DMG", "0", DangerRed);
            totalDmgLabel = dmgStat.Q<Label>("stat-value");
            statsPanel.Add(dmgStat);

            squadSection.Add(statsPanel);
            mainContent.Add(squadSection);

            root.Add(mainContent);
        }

        private void BuildFooter()
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;
            footer.style.paddingLeft = 48;
            footer.style.paddingRight = 48;
            footer.style.paddingTop = 20;
            footer.style.paddingBottom = 24;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = Stone800;

            // Left - Back button
            var footerLeft = new VisualElement();
            footerLeft.style.flexDirection = FlexDirection.Row;

            backButton = CreateFooterButton("BACK", ButtonStyle.Secondary);
            backButton.clicked += OnBackClicked;
            footerLeft.Add(backButton);

            footer.Add(footerLeft);

            // Right - Clear & Battle buttons
            var footerRight = new VisualElement();
            footerRight.style.flexDirection = FlexDirection.Row;

            clearButton = CreateFooterButton("CLEAR", ButtonStyle.Danger);
            clearButton.clicked += OnClearClicked;
            footerRight.Add(clearButton);

            startBattleButton = CreateFooterButton("START BATTLE", ButtonStyle.Primary);
            startBattleButton.style.minWidth = 240;
            startBattleButton.clicked += OnStartBattleClicked;
            footerRight.Add(startBattleButton);

            footer.Add(footerRight);
            root.Add(footer);
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

        private VisualElement CreateSection(string title, string count)
        {
            var section = new VisualElement();
            section.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            section.style.borderTopWidth = 1;
            section.style.borderBottomWidth = 1;
            section.style.borderLeftWidth = 1;
            section.style.borderRightWidth = 1;
            section.style.borderTopColor = Stone700;
            section.style.borderBottomColor = Stone700;
            section.style.borderLeftColor = Stone700;
            section.style.borderRightColor = Stone700;
            section.style.borderTopLeftRadius = 12;
            section.style.borderTopRightRadius = 12;
            section.style.borderBottomLeftRadius = 12;
            section.style.borderBottomRightRadius = 12;
            section.style.flexDirection = FlexDirection.Column;

            // Header
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.paddingLeft = 20;
            header.style.paddingRight = 20;
            header.style.paddingTop = 16;
            header.style.paddingBottom = 16;
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Stone800;
            header.style.backgroundColor = new Color(0, 0, 0, 0.3f);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 14;
            titleLabel.style.color = Stone400;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 2;
            header.Add(titleLabel);

            var countLabel = new Label(count);
            countLabel.name = "section-count";
            countLabel.style.fontSize = 12;
            countLabel.style.color = MedievalGold;
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(countLabel);

            section.Add(header);
            return section;
        }

        private VisualElement CreateStatItem(string label, string value, Color valueColor)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Column;
            item.style.alignItems = Align.Center;

            var labelEl = new Label(label);
            labelEl.style.fontSize = 10;
            labelEl.style.color = Stone500;
            labelEl.style.letterSpacing = 1;
            labelEl.style.marginBottom = 4;
            item.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.name = "stat-value";
            valueEl.style.fontSize = 24;
            valueEl.style.color = valueColor;
            valueEl.style.unityFontStyleAndWeight = FontStyle.Bold;
            item.Add(valueEl);

            return item;
        }

        private VisualElement CreateUnitCard(int index, string unitName, string className, string role, int hp, int dmg, int tier)
        {
            var card = new Button();
            card.style.width = 180;
            card.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = Stone800;
            card.style.borderBottomColor = Stone800;
            card.style.borderLeftColor = Stone800;
            card.style.borderRightColor = Stone800;
            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;
            card.style.marginRight = 12;
            card.style.marginBottom = 12;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.flexDirection = FlexDirection.Column;

            int capturedIndex = index;
            card.clicked += () => OnPoolCardClicked(capturedIndex, card);

            // Header row
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 8;

            // Icon
            var icon = new VisualElement();
            icon.style.width = 40;
            icon.style.height = 40;
            icon.style.backgroundColor = Stone900;
            icon.style.borderTopWidth = 1;
            icon.style.borderBottomWidth = 1;
            icon.style.borderLeftWidth = 1;
            icon.style.borderRightWidth = 1;
            icon.style.borderTopColor = Stone700;
            icon.style.borderBottomColor = Stone700;
            icon.style.borderLeftColor = Stone700;
            icon.style.borderRightColor = Stone700;
            icon.style.borderTopLeftRadius = 6;
            icon.style.borderTopRightRadius = 6;
            icon.style.borderBottomLeftRadius = 6;
            icon.style.borderBottomRightRadius = 6;
            icon.style.alignItems = Align.Center;
            icon.style.justifyContent = Justify.Center;
            icon.style.marginRight = 10;

            var iconLetter = new Label(className.Substring(0, 1));
            iconLetter.style.fontSize = 18;
            iconLetter.style.color = MedievalGold;
            iconLetter.style.unityFontStyleAndWeight = FontStyle.Bold;
            icon.Add(iconLetter);
            headerRow.Add(icon);

            // Info
            var info = new VisualElement();
            info.style.flexGrow = 1;

            var nameLabel = new Label(unitName);
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = Color.white;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            info.Add(nameLabel);

            var classLabel = new Label(className.ToUpper());
            classLabel.style.fontSize = 10;
            classLabel.style.color = Stone500;
            classLabel.style.letterSpacing = 1;
            info.Add(classLabel);

            headerRow.Add(info);
            card.Add(headerRow);

            // Role
            var roleLabel = new Label(role);
            roleLabel.style.fontSize = 10;
            roleLabel.style.color = Stone500;
            roleLabel.style.marginBottom = 8;
            card.Add(roleLabel);

            // Stats row
            var statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.paddingTop = 8;
            statsRow.style.borderTopWidth = 1;
            statsRow.style.borderTopColor = Stone800;

            var hpContainer = new VisualElement();
            hpContainer.style.flexGrow = 1;
            hpContainer.style.alignItems = Align.Center;

            var hpLabel = new Label("HP");
            hpLabel.style.fontSize = 9;
            hpLabel.style.color = Stone700;
            hpContainer.Add(hpLabel);

            var hpValue = new Label(hp.ToString());
            hpValue.style.fontSize = 14;
            hpValue.style.color = Emerald;
            hpValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            hpContainer.Add(hpValue);
            statsRow.Add(hpContainer);

            var dmgContainer = new VisualElement();
            dmgContainer.style.flexGrow = 1;
            dmgContainer.style.alignItems = Align.Center;

            var dmgLabel = new Label("DMG");
            dmgLabel.style.fontSize = 9;
            dmgLabel.style.color = Stone700;
            dmgContainer.Add(dmgLabel);

            var dmgValue = new Label(dmg.ToString());
            dmgValue.style.fontSize = 14;
            dmgValue.style.color = DangerRed;
            dmgValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            dmgContainer.Add(dmgValue);
            statsRow.Add(dmgContainer);

            card.Add(statsRow);

            // Tier badge
            var tierBadge = new VisualElement();
            tierBadge.style.position = Position.Absolute;
            tierBadge.style.top = 8;
            tierBadge.style.right = 8;
            tierBadge.style.width = 24;
            tierBadge.style.height = 24;
            tierBadge.style.backgroundColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.2f);
            tierBadge.style.borderTopWidth = 1;
            tierBadge.style.borderBottomWidth = 1;
            tierBadge.style.borderLeftWidth = 1;
            tierBadge.style.borderRightWidth = 1;
            tierBadge.style.borderTopColor = MedievalGold;
            tierBadge.style.borderBottomColor = MedievalGold;
            tierBadge.style.borderLeftColor = MedievalGold;
            tierBadge.style.borderRightColor = MedievalGold;
            tierBadge.style.borderTopLeftRadius = 12;
            tierBadge.style.borderTopRightRadius = 12;
            tierBadge.style.borderBottomLeftRadius = 12;
            tierBadge.style.borderBottomRightRadius = 12;
            tierBadge.style.alignItems = Align.Center;
            tierBadge.style.justifyContent = Justify.Center;

            var tierText = new Label(tier.ToString());
            tierText.style.fontSize = 11;
            tierText.style.color = MedievalGold;
            tierText.style.unityFontStyleAndWeight = FontStyle.Bold;
            tierBadge.Add(tierText);
            card.Add(tierBadge);

            return card;
        }

        private VisualElement CreateSquadCard(int index, string unitName, string className, int hp, int dmg)
        {
            var card = new Button();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.paddingTop = 12;
            card.style.paddingBottom = 12;
            card.style.backgroundColor = new Color(0, 0, 0, 0.4f);
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = Stone800;
            card.style.borderBottomColor = Stone800;
            card.style.borderLeftColor = Stone800;
            card.style.borderRightColor = Stone800;
            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;
            card.style.marginBottom = 8;

            int capturedIndex = index;
            card.clicked += () => OnSquadCardClicked(capturedIndex);

            // Icon
            var icon = new VisualElement();
            icon.style.width = 36;
            icon.style.height = 36;
            icon.style.backgroundColor = Stone900;
            icon.style.borderTopWidth = 1;
            icon.style.borderBottomWidth = 1;
            icon.style.borderLeftWidth = 1;
            icon.style.borderRightWidth = 1;
            icon.style.borderTopColor = Stone700;
            icon.style.borderBottomColor = Stone700;
            icon.style.borderLeftColor = Stone700;
            icon.style.borderRightColor = Stone700;
            icon.style.borderTopLeftRadius = 6;
            icon.style.borderTopRightRadius = 6;
            icon.style.borderBottomLeftRadius = 6;
            icon.style.borderBottomRightRadius = 6;
            icon.style.alignItems = Align.Center;
            icon.style.justifyContent = Justify.Center;
            icon.style.marginRight = 12;

            var iconLetter = new Label(className.Substring(0, 1));
            iconLetter.style.fontSize = 16;
            iconLetter.style.color = MedievalGold;
            iconLetter.style.unityFontStyleAndWeight = FontStyle.Bold;
            icon.Add(iconLetter);
            card.Add(icon);

            // Info
            var info = new VisualElement();
            info.style.flexGrow = 1;

            var nameLabel = new Label(unitName);
            nameLabel.style.fontSize = 14;
            nameLabel.style.color = Color.white;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            info.Add(nameLabel);

            var statsLabel = new Label($"HP: {hp}  DMG: {dmg}");
            statsLabel.style.fontSize = 10;
            statsLabel.style.color = Stone500;
            info.Add(statsLabel);
            card.Add(info);

            // Remove button
            var removeBtn = new VisualElement();
            removeBtn.style.width = 28;
            removeBtn.style.height = 28;
            removeBtn.style.backgroundColor = new Color(BloodDeep.r, BloodDeep.g, BloodDeep.b, 0.3f);
            removeBtn.style.borderTopWidth = 1;
            removeBtn.style.borderBottomWidth = 1;
            removeBtn.style.borderLeftWidth = 1;
            removeBtn.style.borderRightWidth = 1;
            removeBtn.style.borderTopColor = BloodDeep;
            removeBtn.style.borderBottomColor = BloodDeep;
            removeBtn.style.borderLeftColor = BloodDeep;
            removeBtn.style.borderRightColor = BloodDeep;
            removeBtn.style.borderTopLeftRadius = 14;
            removeBtn.style.borderTopRightRadius = 14;
            removeBtn.style.borderBottomLeftRadius = 14;
            removeBtn.style.borderBottomRightRadius = 14;
            removeBtn.style.alignItems = Align.Center;
            removeBtn.style.justifyContent = Justify.Center;

            var removeX = new Label("×");
            removeX.style.fontSize = 18;
            removeX.style.color = DangerRed;
            removeBtn.Add(removeX);
            card.Add(removeBtn);

            return card;
        }

        private enum ButtonStyle { Primary, Secondary, Danger }

        private Button CreateFooterButton(string text, ButtonStyle style)
        {
            var btn = new Button();
            btn.text = text;
            btn.style.height = 56;
            btn.style.minWidth = 180;
            btn.style.fontSize = 14;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.letterSpacing = 2;
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;
            btn.style.marginLeft = 12;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;

            switch (style)
            {
                case ButtonStyle.Primary:
                    btn.style.backgroundColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
                    btn.style.borderTopColor = MedievalGold;
                    btn.style.borderBottomColor = MedievalGold;
                    btn.style.borderLeftColor = MedievalGold;
                    btn.style.borderRightColor = MedievalGold;
                    btn.style.color = MedievalGold;
                    break;

                case ButtonStyle.Secondary:
                    btn.style.backgroundColor = new Color(0, 0, 0, 0.6f);
                    btn.style.borderTopColor = Stone700;
                    btn.style.borderBottomColor = Stone700;
                    btn.style.borderLeftColor = Stone700;
                    btn.style.borderRightColor = Stone700;
                    btn.style.color = Stone500;
                    break;

                case ButtonStyle.Danger:
                    btn.style.backgroundColor = new Color(BloodDeep.r, BloodDeep.g, BloodDeep.b, 0.2f);
                    btn.style.borderTopColor = BloodDeep;
                    btn.style.borderBottomColor = BloodDeep;
                    btn.style.borderLeftColor = BloodDeep;
                    btn.style.borderRightColor = BloodDeep;
                    btn.style.color = DangerRed;
                    break;
            }

            return btn;
        }

        #endregion

        #region Pool Management

        private void PopulatePool()
        {
            if (poolContainer == null) return;
            poolContainer.Clear();

            // Get units from UnitSelectionManager instead of creating demo data
            if (UnitSelectionManager.Instance == null)
            {
                Debug.LogWarning("[UnitSelectionUI] UnitSelectionManager not found! Cannot populate pool.");
                return;
            }

            var availableUnits = UnitSelectionManager.Instance.GetAvailableUnits();
            if (availableUnits == null || availableUnits.Count == 0)
            {
                Debug.LogWarning("[UnitSelectionUI] No available units from manager!");
                return;
            }

            Debug.Log($"[UnitSelectionUI] Populating pool with {availableUnits.Count} units from manager");

            for (int i = 0; i < availableUnits.Count; i++)
            {
                var unit = availableUnits[i];
                if (unit == null)
                {
                    Debug.LogWarning($"[UnitSelectionUI] Unit at index {i} is null, skipping");
                    continue;
                }

                string className = unit.unitType.ToString();
                string role = classRoles.GetValueOrDefault(className, "Specialist");
                var card = CreateUnitCard(i, unit.unitName, className, role, unit.maxHealth, unit.attackDamage, 1);
                poolContainer.Add(card);
            }
        }

        private void UpdateSquadDisplay()
        {
            if (squadContainer == null) return;
            squadContainer.Clear();

            var squad = UnitSelectionManager.Instance?.GetSelectedSquad();

            if (squad != null && squad.Count > 0)
            {
                for (int i = 0; i < squad.Count; i++)
                {
                    var unit = squad[i];
                    if (unit == null) continue;

                    string className = unitClasses[i % unitClasses.Length];
                    var card = CreateSquadCard(i, unit.unitName, className, unit.maxHealth, unit.attackDamage);
                    squadContainer.Add(card);
                }
            }
            else if (selectedIndices.Count > 0)
            {
                // Use local selection for demo
                for (int i = 0; i < selectedIndices.Count; i++)
                {
                    int idx = selectedIndices[i];
                    string className = unitClasses[idx % unitClasses.Length];
                    int hp = 100 + idx * 20;
                    int dmg = 15 + idx * 5;
                    var card = CreateSquadCard(i, $"{className} Unit", className, hp, dmg);
                    squadContainer.Add(card);
                }
            }
            else
            {
                // Empty state
                var emptyState = new VisualElement();
                emptyState.style.flexGrow = 1;
                emptyState.style.alignItems = Align.Center;
                emptyState.style.justifyContent = Justify.Center;
                emptyState.style.paddingTop = 40;
                emptyState.style.paddingBottom = 40;

                var emptyIcon = new Label("⚔️");
                emptyIcon.style.fontSize = 48;
                emptyIcon.style.color = Stone800;
                emptyIcon.style.marginBottom = 16;
                emptyState.Add(emptyIcon);

                var emptyText = new Label("Select units from the pool");
                emptyText.style.fontSize = 14;
                emptyText.style.color = Stone700;
                emptyState.Add(emptyText);

                squadContainer.Add(emptyState);
            }

            // Update count
            int count = UnitSelectionManager.Instance?.GetSelectedSquad()?.Count ?? selectedIndices.Count;
            if (squadCountLabel != null) squadCountLabel.text = $"{count} / 5";

            UpdateSquadStats();
        }

        private void UpdateSquadStats()
        {
            int totalHp = 0;
            int totalDmg = 0;

            var squad = UnitSelectionManager.Instance?.GetSelectedSquad();
            if (squad != null)
            {
                foreach (var unit in squad)
                {
                    if (unit != null)
                    {
                        totalHp += unit.maxHealth;
                        totalDmg += unit.attackDamage;
                    }
                }
            }
            else
            {
                // Demo calculation
                foreach (int idx in selectedIndices)
                {
                    totalHp += 100 + idx * 20;
                    totalDmg += 15 + idx * 5;
                }
            }

            if (totalHpLabel != null) totalHpLabel.text = totalHp.ToString();
            if (totalDmgLabel != null) totalDmgLabel.text = totalDmg.ToString();
        }

        #endregion

        #region Event Handlers

        private void OnPoolCardClicked(int index, Button card)
        {
            if (selectedIndices.Count >= 5)
            {
                Debug.Log("[UnitSelectionUI] Squad is full!");
                return;
            }

            if (selectedIndices.Contains(index))
            {
                // Already selected, deselect
                selectedIndices.Remove(index);
                card.style.borderTopColor = Stone800;
                card.style.borderBottomColor = Stone800;
                card.style.borderLeftColor = Stone800;
                card.style.borderRightColor = Stone800;
                
                Debug.Log($"[UnitSelectionUI] Deselected unit at pool index {index}");
            }
            else
            {
                // Select
                selectedIndices.Add(index);
                card.style.borderTopColor = Emerald;
                card.style.borderBottomColor = Emerald;
                card.style.borderLeftColor = Emerald;
                card.style.borderRightColor = Emerald;

                // Also notify manager
                bool success = UnitSelectionManager.Instance?.SelectUnit(index) ?? false;
                Debug.Log($"[UnitSelectionUI] Selected unit at pool index {index}, Manager notified: {success}");
            }

            UpdateSquadDisplay();
        }

        private void OnSquadCardClicked(int index)
        {
            if (index >= 0 && index < selectedIndices.Count)
            {
                int poolIndex = selectedIndices[index];
                selectedIndices.RemoveAt(index);

                // Reset card border in pool
                if (poolContainer != null && poolIndex < poolContainer.childCount)
                {
                    var poolCard = poolContainer[poolIndex];
                    poolCard.style.borderTopColor = Stone800;
                    poolCard.style.borderBottomColor = Stone800;
                    poolCard.style.borderLeftColor = Stone800;
                    poolCard.style.borderRightColor = Stone800;
                }

                var squad = UnitSelectionManager.Instance?.GetSelectedSquad();
                if (squad != null && index < squad.Count)
                {
                    UnitSelectionManager.Instance?.DeselectUnit(squad[index]);
                }

                UpdateSquadDisplay();
            }
        }

        private void OnClearClicked()
        {
            selectedIndices.Clear();
            UnitSelectionManager.Instance?.ClearSquad();

            // Reset all pool card borders
            if (poolContainer != null)
            {
                foreach (var child in poolContainer.Children())
                {
                    child.style.borderTopColor = Stone800;
                    child.style.borderBottomColor = Stone800;
                    child.style.borderLeftColor = Stone800;
                    child.style.borderRightColor = Stone800;
                }
            }

            UpdateSquadDisplay();
        }

        private void OnBackClicked()
        {
            Debug.Log("[UnitSelectionUI] Back clicked");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
        }

        private void OnStartBattleClicked()
        {
            // Check both the UI's tracking and the manager's squad
            var squad = UnitSelectionManager.Instance?.GetSelectedSquad();
            int managerCount = squad?.Count ?? 0;
            int uiCount = selectedIndices.Count;
            
            Debug.Log($"[UnitSelectionUI] Start Battle - Manager squad: {managerCount}, UI selected: {uiCount}");
            
            if (managerCount == 0 && uiCount == 0)
            {
                Debug.LogWarning("[UnitSelectionUI] Select at least one unit!");
                return;
            }

            // If counts don't match, log a warning but still proceed if manager has units
            if (managerCount != uiCount)
            {
                Debug.LogWarning($"[UnitSelectionUI] Mismatch between UI ({uiCount}) and Manager ({managerCount}) counts!");
            }

            Debug.Log($"[UnitSelectionUI] Starting battle with {managerCount} units!");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadGameplay();
            }
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            if (UnitSelectionManager.Instance != null)
            {
                UnitSelectionManager.Instance.OnPoolRefreshed += OnPoolRefreshed;
                UnitSelectionManager.Instance.OnSquadChanged += OnSquadChanged;
            }
        }

        private void UnbindEvents()
        {
            if (UnitSelectionManager.Instance != null)
            {
                UnitSelectionManager.Instance.OnPoolRefreshed -= OnPoolRefreshed;
                UnitSelectionManager.Instance.OnSquadChanged -= OnSquadChanged;
            }
        }

        private void OnPoolRefreshed(List<UnitStatsSO> units)
        {
            PopulatePool();
        }

        private void OnSquadChanged(List<UnitStatsSO> units)
        {
            UpdateSquadDisplay();
        }

        #endregion
    }
}
