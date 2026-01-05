using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Core;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace AutoBattler.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        
        private VisualElement root;
        private Label titleLabel;
        private Label messageLabel;
        private Button restartButton;
        private Button menuButton;

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            }
            
            // Initially hide
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                root = uiDocument.rootVisualElement;
                root.style.display = DisplayStyle.None;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.GameOver)
            {
                ShowGameOver();
            }
            else
            {
                if (root != null) root.style.display = DisplayStyle.None;
            }
        }

        private void ShowGameOver()
        {
            if (root == null) BuildUI();
            
            root.style.display = DisplayStyle.Flex;
            
            // Animate in
            root.style.opacity = 0;
            root.schedule.Execute(() => 
            {
                root.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
                root.style.transitionDuration = new List<TimeValue> { new TimeValue(1f, TimeUnit.Second) };
                root.style.opacity = 1;
            }).ExecuteLater(100);
        }

        private void BuildUI()
        {
            root = uiDocument.rootVisualElement;
            root.Clear();
            
            // Full screen overlay
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            root.style.flexDirection = FlexDirection.Column;

            // Title
            titleLabel = new Label("DEFEAT");
            titleLabel.style.fontSize = 72;
            titleLabel.style.color = new Color(0.7f, 0.1f, 0.1f); // Blood red
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 20;
            root.Add(titleLabel);

            // Message
            messageLabel = new Label("Your warband has fallen.");
            messageLabel.style.fontSize = 24;
            messageLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            messageLabel.style.marginBottom = 60;
            root.Add(messageLabel);

            // Buttons Container
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.style.alignItems = Align.Center;
            root.Add(buttonContainer);

            // Restart Button
            restartButton = CreateButton("TRY AGAIN");
            restartButton.clicked += OnRestartClicked;
            restartButton.style.marginRight = 20;
            buttonContainer.Add(restartButton);

            // Menu Button
            menuButton = CreateButton("MAIN MENU");
            menuButton.clicked += OnMenuClicked;
            buttonContainer.Add(menuButton);
        }

        private Button CreateButton(string text)
        {
            var btn = new Button();
            btn.text = text;
            btn.style.width = 200;
            btn.style.height = 60;
            btn.style.fontSize = 20;
            btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            btn.style.color = Color.white;
            btn.style.borderTopWidth = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f);
            btn.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f);
            btn.style.borderLeftColor = new Color(0.5f, 0.5f, 0.5f);
            btn.style.borderRightColor = new Color(0.5f, 0.5f, 0.5f);
            
            btn.RegisterCallback<MouseEnterEvent>(e => btn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f));
            btn.RegisterCallback<MouseLeaveEvent>(e => btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f));
            
            return btn;
        }

        private void OnRestartClicked()
        {
            Debug.Log("[GameOverUI] Restart clicked");
            // Reload the current run or go to selection
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadUnitSelection();
            }
        }

        private void OnMenuClicked()
        {
            Debug.Log("[GameOverUI] Menu clicked");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadMainMenu();
            }
        }
    }
}