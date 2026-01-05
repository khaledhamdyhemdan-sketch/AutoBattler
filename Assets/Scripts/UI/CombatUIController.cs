using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Core;

namespace AutoBattler.UI
{
    /// <summary>
    /// Controls combat UI elements including Fight button
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatUIController : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        // UI Elements
        private VisualElement root;
        private Button fightButton;
        private Label combatStatusLabel;
        private VisualElement playerUnitsContainer;
        private VisualElement enemyUnitsContainer;
        private Label playerCountLabel;
        private Label enemyCountLabel;

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
            // For now, create UI programmatically
            // In production, load from UXML file
            CreateUIElements();
            
            Debug.Log("[CombatUIController] UI initialized");
        }

        private void CreateUIElements()
        {
            root = uiDocument.rootVisualElement;
            root.Clear();

            // Create main container
            var mainContainer = new VisualElement();
            mainContainer.name = "combat-container";
            mainContainer.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            mainContainer.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            mainContainer.style.paddingTop = 20;
            mainContainer.style.paddingBottom = 20;
            mainContainer.style.paddingLeft = 20;
            mainContainer.style.paddingRight = 20;

            // Fight button
            fightButton = new Button();
            fightButton.name = "fight-button";
            fightButton.text = "FIGHT!";
            fightButton.style.width = 200;
            fightButton.style.height = 60;
            fightButton.style.fontSize = 24;
            fightButton.style.marginBottom = 20;
            fightButton.style.alignSelf = Align.Center;
            fightButton.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
            fightButton.style.color = new StyleColor(Color.white);
            fightButton.clicked += OnFightButtonClicked;

            // Combat status label
            combatStatusLabel = new Label("Preparing for battle...");
            combatStatusLabel.name = "combat-status-label";
            combatStatusLabel.style.fontSize = 20;
            combatStatusLabel.style.marginBottom = 20;
            combatStatusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            combatStatusLabel.style.color = new StyleColor(Color.white);

            // Units info container
            var unitsInfoContainer = new VisualElement();
            unitsInfoContainer.style.flexDirection = FlexDirection.Row;
            unitsInfoContainer.style.justifyContent = Justify.SpaceAround;
            unitsInfoContainer.style.marginTop = 20;

            // Player units info
            var playerInfo = CreateUnitInfoPanel("Player Units", out playerCountLabel, out playerUnitsContainer);
            
            // Enemy units info
            var enemyInfo = CreateUnitInfoPanel("Enemy Units", out enemyCountLabel, out enemyUnitsContainer);

            unitsInfoContainer.Add(playerInfo);
            unitsInfoContainer.Add(enemyInfo);

            // Add all to main container
            mainContainer.Add(fightButton);
            mainContainer.Add(combatStatusLabel);
            mainContainer.Add(unitsInfoContainer);

            root.Add(mainContainer);
        }

        private VisualElement CreateUnitInfoPanel(string title, out Label countLabel, out VisualElement container)
        {
            var panel = new VisualElement();
            panel.style.width = 300;
            panel.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 0.8f));
            panel.style.paddingTop = 10;
            panel.style.paddingBottom = 10;
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.borderTopLeftRadius = 10;
            panel.style.borderTopRightRadius = 10;
            panel.style.borderBottomLeftRadius = 10;
            panel.style.borderBottomRightRadius = 10;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 18;
            titleLabel.style.marginBottom = 10;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLabel.style.color = new StyleColor(Color.yellow);

            countLabel = new Label("0 units");
            countLabel.style.fontSize = 16;
            countLabel.style.marginBottom = 10;
            countLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            countLabel.style.color = new StyleColor(Color.white);

            container = new VisualElement();
            container.style.maxHeight = 200;

            panel.Add(titleLabel);
            panel.Add(countLabel);
            panel.Add(container);

            return panel;
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            }

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnUnitCountsChanged += UpdateUnitCounts;
                // Update initial counts
                UpdateUnitCounts(CombatManager.Instance.PlayerUnitCount, CombatManager.Instance.EnemyUnitCount);
            }
        }

        private void UnbindEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnUnitCountsChanged -= UpdateUnitCounts;
            }

            if (fightButton != null)
            {
                fightButton.clicked -= OnFightButtonClicked;
            }
        }

        #endregion

        #region UI Event Handlers

        private void OnFightButtonClicked()
        {
            Debug.Log("[CombatUIController] Fight button clicked!");

            if (GameManager.Instance != null)
            {
                // Transition to combat phase
                GameManager.Instance.TransitionToPhase(GamePhase.Combat);
            }
        }

        private void OnPhaseChanged(GamePhase newPhase)
        {
            UpdateUIForPhase(newPhase);
        }

        private void UpdateUIForPhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Preparation:
                    fightButton.SetEnabled(true);
                    fightButton.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
                    combatStatusLabel.text = "Prepare your army!";
                    break;

                case GamePhase.Combat:
                    fightButton.SetEnabled(false);
                    fightButton.style.backgroundColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                    combatStatusLabel.text = "⚔️ BATTLE IN PROGRESS ⚔️";
                    combatStatusLabel.style.color = new StyleColor(Color.red);
                    break;

                case GamePhase.Results:
                    fightButton.SetEnabled(false);
                    bool victory = CombatManager.Instance?.PlayerVictory ?? false;
                    combatStatusLabel.text = victory ? "🎉 VICTORY! 🎉" : "💀 DEFEAT 💀";
                    combatStatusLabel.style.color = new StyleColor(victory ? Color.green : Color.red);
                    break;

                case GamePhase.GameOver:
                    fightButton.SetEnabled(false);
                    combatStatusLabel.text = "GAME OVER";
                    combatStatusLabel.style.color = new StyleColor(Color.red);
                    break;
            }
        }

        #endregion

        #region Public Methods

        public void UpdateUnitCounts(int playerCount, int enemyCount)
        {
            if (playerCountLabel != null)
            {
                playerCountLabel.text = $"{playerCount} units";
            }

            if (enemyCountLabel != null)
            {
                enemyCountLabel.text = $"{enemyCount} units";
            }
        }

        #endregion
    }
}
