using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Core;

namespace AutoBattler.UI
{
    /// <summary>
    /// Main Battle HUD using Unity 6.3 UI Toolkit
    /// Displays phase timer, round info, and player stats
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BattleHUDController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        // UI Elements
        private VisualElement root;
        private Label phaseLabel;
        private Label timerLabel;
        private Label roundLabel;
        private Label goldLabel;
        
        // New Elements
        private ScrollView warbandList;
        private VisualElement detailsPanel;
        private VisualElement shopContainer;
        private Button rerollButton;
        private Button battleButton;

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

        private void Update()
        {
            UpdateTimer();
        }

        #endregion

        #region UI Initialization

        private void InitializeUI()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            // Ensure PanelSettings is assigned
            if (uiDocument.panelSettings == null)
            {
                uiDocument.panelSettings = Resources.Load<PanelSettings>("Settings/GameHUDPanelSettings");
            }

            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[BattleHUDController] RootVisualElement is null. Ensure PanelSettings is assigned to the UIDocument.");
                return;
            }

            // Load the BattleHUD UXML if root is empty or we want to force it
            var visualTree = Resources.Load<VisualTreeAsset>("UI/BattleHUD");
            if (visualTree != null)
            {
                root.Clear();
                visualTree.CloneTree(root);
            }

            // Apply Theme
            ApplyTheme();

            // Query UI elements
            phaseLabel = root.Q<Label>("phase-label");
            timerLabel = root.Q<Label>("timer-label");
            roundLabel = root.Q<Label>("round-label");
            goldLabel = root.Q<Label>("gold-label");
            
            // Bind new elements
            warbandList = root.Q<ScrollView>("warband-list");
            detailsPanel = root.Q<VisualElement>("details-panel");
            shopContainer = root.Q<VisualElement>("shop-container");
            rerollButton = root.Q<Button>("reroll-btn");
            battleButton = root.Q<Button>("battle-btn");

            Debug.Log("[BattleHUDController] UI initialized");
            
            // Initial update
            UpdateWarbandList();
        }

        private void ApplyTheme()
        {
            var theme = Resources.Load<StyleSheet>("UI/Styles/Theme");
            if (theme != null)
            {
                root.styleSheets.Add(theme);
            }
            else
            {
                Debug.LogWarning("[BattleHUDController] Theme stylesheet not found at UI/Styles/Theme");
            }
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += UpdatePhaseDisplay;
                GameManager.Instance.OnRoundChanged += UpdateRoundDisplay;
                GameManager.Instance.OnGoldChanged += UpdateGoldDisplay;
                GameManager.Instance.OnHealthChanged += UpdateHealthDisplay;
                GameManager.Instance.OnLevelChanged += UpdateLevelDisplay;
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
                GameManager.Instance.OnPhaseChanged -= UpdatePhaseDisplay;
                GameManager.Instance.OnRoundChanged -= UpdateRoundDisplay;
                GameManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
                GameManager.Instance.OnHealthChanged -= UpdateHealthDisplay;
                GameManager.Instance.OnLevelChanged -= UpdateLevelDisplay;
            }
        }

        #endregion

        #region UI Updates

        private void UpdateWarbandList()
        {
            if (warbandList == null || CombatManager.Instance == null) return;

            warbandList.Clear();
            
            // Get player units from CombatManager
            // We need a way to get the actual list of units
            // For now, let's assume we can access them via a public property we'll add
            var units = CombatManager.Instance.GetPlayerUnits();
            
            foreach (var unit in units)
            {
                if (unit == null) continue;

                var card = UIFactory.LoadTemplate("UI/Templates/UnitCardTemplate");
                card.AddToClassList("unit-card-mini"); // We'll add this class to USS
                warbandList.Add(card);

                var unitCard = new UnitCardUI(card, 0);
                unitCard.SetUnit(unit.UnitStats);
                
                // If unit is dead, dim the card
                if (unit.IsDead)
                {
                    card.style.opacity = 0.5f;
                }
            }
        }

        private void UpdatePhaseDisplay(GamePhase phase)
        {
            if (phaseLabel != null)
            {
                // Simplified text to be less overwhelming
                string phaseText = phase == GamePhase.Preparation ? "PREPARING" : phase.ToString().ToUpper();
                phaseLabel.text = phaseText;

                // Apply phase-specific styling
                phaseLabel.RemoveFromClassList("preparation-phase");
                phaseLabel.RemoveFromClassList("combat-phase");

                switch (phase)
                {
                    case GamePhase.Preparation:
                        phaseLabel.AddToClassList("preparation-phase");
                        break;
                    case GamePhase.Combat:
                        phaseLabel.AddToClassList("combat-phase");
                        UpdateWarbandList(); // Refresh list when combat starts
                        break;
                }
            }
        }

        private void UpdateTimer()
        {
            if (GameManager.Instance == null || timerLabel == null) return;

            float timer = GameManager.Instance.PhaseTimer;
            int minutes = Mathf.FloorToInt(timer / 60);
            int seconds = Mathf.FloorToInt(timer % 60);

            timerLabel.text = $"{minutes:00}:{seconds:00}";
        }

        private void UpdateRoundDisplay(int round)
        {
            if (roundLabel != null)
            {
                roundLabel.text = $"Round {round}";
            }
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (goldLabel != null)
            {
                goldLabel.text = $"{gold}";
            }
        }

        private void UpdateHealthDisplay(int health)
        {
            // Removed health label from new design
        }

        private void UpdateLevelDisplay(int level)
        {
            // Removed level label from new design
        }

        private void UpdateExperienceBar()
        {
            // Removed exp bar from new design
        }

        #endregion
    }
}
