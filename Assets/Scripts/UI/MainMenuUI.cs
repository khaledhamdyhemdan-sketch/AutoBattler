using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Core;

namespace AutoBattler.UI
{
    /// <summary>
    /// Main Menu UI - The Void style with medieval iron/gold aesthetic
    /// Programmatic UI construction for Unity 6.3 reliability
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private SoulForgeUI soulForgeUI;

        // Colors (matching the-void medieval theme)
        private static readonly Color MedievalGold = new Color(0.77f, 0.63f, 0.35f); // #C5A059
        private static readonly Color Stone900 = new Color(0.11f, 0.10f, 0.09f); // #1c1917
        private static readonly Color Stone800 = new Color(0.16f, 0.14f, 0.14f); // #292524
        private static readonly Color Stone700 = new Color(0.27f, 0.25f, 0.24f); // #44403c
        private static readonly Color Stone500 = new Color(0.47f, 0.44f, 0.42f); // #78716c
        private static readonly Color Stone400 = new Color(0.66f, 0.64f, 0.62f); // #a8a29e
        private static readonly Color BloodDeep = new Color(0.50f, 0.11f, 0.11f); // #7f1d1d
        private static readonly Color DangerRed = new Color(0.94f, 0.27f, 0.27f); // #ef4444

        // UI Elements
        private VisualElement root;
        private Button playButton;
        private Button soulForgeButton;
        private Button settingsButton;
        private Button quitButton;
        private VisualElement loadingPanel;
        private ProgressBar loadingBar;
        private Label loadingLabel;

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            
            // Auto-find SoulForgeUI if not assigned
            if (soulForgeUI == null) soulForgeUI = GetComponent<SoulForgeUI>();
            if (soulForgeUI == null) soulForgeUI = FindFirstObjectByType<SoulForgeUI>();

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
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.SpaceBetween;
            root.style.paddingTop = 60;
            root.style.paddingBottom = 40;

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("UI/MainMenu");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            // Decorative border frame
            var borderFrame = new VisualElement();
            borderFrame.style.position = Position.Absolute;
            borderFrame.style.left = 24;
            borderFrame.style.top = 24;
            borderFrame.style.right = 24;
            borderFrame.style.bottom = 24;
            borderFrame.style.borderTopWidth = 2;
            borderFrame.style.borderBottomWidth = 2;
            borderFrame.style.borderLeftWidth = 2;
            borderFrame.style.borderRightWidth = 2;
            borderFrame.style.borderTopColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            borderFrame.style.borderBottomColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            borderFrame.style.borderLeftColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            borderFrame.style.borderRightColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
            borderFrame.style.borderTopLeftRadius = 8;
            borderFrame.style.borderTopRightRadius = 8;
            borderFrame.style.borderBottomLeftRadius = 8;
            borderFrame.style.borderBottomRightRadius = 8;
            borderFrame.pickingMode = PickingMode.Ignore;
            root.Add(borderFrame);

            // Build sections
            BuildHeader();
            BuildMenuButtons();
            BuildFooter();
            BuildLoadingPanel();

            Debug.Log("[MainMenuUI] UI built successfully");
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Column;
            header.style.alignItems = Align.Center;
            header.style.marginTop = 80;

            // Logo/Icon
            var logo = new Label("⚔️");
            logo.style.fontSize = 80;
            logo.style.color = MedievalGold;
            logo.style.marginBottom = 24;
            header.Add(logo);

            // Game Title
            var title = new Label("AUTOBATTLER");
            title.style.fontSize = 72;
            title.style.color = MedievalGold;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 12;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(title);

            // Subtitle
            var subtitle = new Label("STRATEGIC COMBAT");
            subtitle.style.fontSize = 16;
            subtitle.style.color = Stone500;
            subtitle.style.letterSpacing = 6;
            subtitle.style.marginTop = 16;
            subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.Add(subtitle);

            // Decorative divider
            var divider = new VisualElement();
            divider.style.width = 200;
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.2f);
            divider.style.marginTop = 32;
            header.Add(divider);

            root.Add(header);
        }

        private void BuildMenuButtons()
        {
            var menuContainer = new VisualElement();
            menuContainer.style.flexDirection = FlexDirection.Column;
            menuContainer.style.alignItems = Align.Center;
            menuContainer.style.justifyContent = Justify.Center;
            menuContainer.style.flexGrow = 1;
            menuContainer.style.minWidth = 400;

            // Play Button (Primary - Gold)
            playButton = CreateMenuButton("PLAY GAME", ButtonStyle.Primary);
            playButton.clicked += OnPlayClicked;
            menuContainer.Add(playButton);

            // Soul Forge Button (Primary - Gold)
            soulForgeButton = CreateMenuButton("SOUL FORGE", ButtonStyle.Primary);
            soulForgeButton.clicked += OnSoulForgeClicked;
            menuContainer.Add(soulForgeButton);

            // Settings Button (Secondary - Stone)
            settingsButton = CreateMenuButton("SETTINGS", ButtonStyle.Secondary);
            settingsButton.clicked += OnSettingsClicked;
            menuContainer.Add(settingsButton);

            // Quit Button (Danger - Blood)
            quitButton = CreateMenuButton("QUIT", ButtonStyle.Danger);
            quitButton.clicked += OnQuitClicked;
            menuContainer.Add(quitButton);

            root.Add(menuContainer);
        }

        private void BuildFooter()
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Column;
            footer.style.alignItems = Align.Center;
            footer.style.marginBottom = 20;

            var credits = new Label("Built with Unity 6.3 • Strategic Auto-Battler Framework");
            credits.style.fontSize = 12;
            credits.style.color = Stone700;
            credits.style.letterSpacing = 2;
            footer.Add(credits);

            // Version
            var version = new Label("v1.0.0");
            version.style.fontSize = 10;
            version.style.color = Stone800;
            version.style.marginTop = 8;
            footer.Add(version);

            root.Add(footer);
        }

        private void BuildLoadingPanel()
        {
            loadingPanel = new VisualElement();
            loadingPanel.style.position = Position.Absolute;
            loadingPanel.style.left = 0;
            loadingPanel.style.top = 0;
            loadingPanel.style.right = 0;
            loadingPanel.style.bottom = 0;
            loadingPanel.style.backgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.95f);
            loadingPanel.style.alignItems = Align.Center;
            loadingPanel.style.justifyContent = Justify.Center;
            loadingPanel.style.display = DisplayStyle.None;

            var loadingContainer = new VisualElement();
            loadingContainer.style.flexDirection = FlexDirection.Column;
            loadingContainer.style.alignItems = Align.Center;

            loadingLabel = new Label("LOADING...");
            loadingLabel.style.fontSize = 24;
            loadingLabel.style.color = MedievalGold;
            loadingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            loadingLabel.style.letterSpacing = 4;
            loadingLabel.style.marginBottom = 24;
            loadingContainer.Add(loadingLabel);

            // Progress bar background
            var barBg = new VisualElement();
            barBg.style.width = 320;
            barBg.style.height = 8;
            barBg.style.backgroundColor = Stone900;
            barBg.style.borderTopLeftRadius = 4;
            barBg.style.borderTopRightRadius = 4;
            barBg.style.borderBottomLeftRadius = 4;
            barBg.style.borderBottomRightRadius = 4;

            loadingBar = new ProgressBar();
            loadingBar.style.width = 320;
            loadingBar.style.height = 8;
            loadingBar.lowValue = 0;
            loadingBar.highValue = 1;
            loadingBar.value = 0;
            loadingContainer.Add(loadingBar);

            loadingPanel.Add(loadingContainer);
            root.Add(loadingPanel);
        }

        #endregion

        #region Button Creation

        private enum ButtonStyle { Primary, Secondary, Danger }

        private Button CreateMenuButton(string text, ButtonStyle style)
        {
            var btn = new Button();
            btn.text = text;
            btn.style.height = 64;
            btn.style.minWidth = 320;
            btn.style.fontSize = 18;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.letterSpacing = 3;
            btn.style.borderTopLeftRadius = 8;
            btn.style.borderTopRightRadius = 8;
            btn.style.borderBottomLeftRadius = 8;
            btn.style.borderBottomRightRadius = 8;
            btn.style.borderTopWidth = 2;
            btn.style.borderBottomWidth = 2;
            btn.style.borderLeftWidth = 2;
            btn.style.borderRightWidth = 2;
            btn.style.marginBottom = 16;
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
                    btn.style.color = Stone400;
                    break;

                case ButtonStyle.Danger:
                    btn.style.backgroundColor = new Color(BloodDeep.r, BloodDeep.g, BloodDeep.b, 0.3f);
                    btn.style.borderTopColor = BloodDeep;
                    btn.style.borderBottomColor = BloodDeep;
                    btn.style.borderLeftColor = BloodDeep;
                    btn.style.borderRightColor = BloodDeep;
                    btn.style.color = DangerRed;
                    break;
            }

            // Hover effects via callbacks
            btn.RegisterCallback<MouseEnterEvent>(e =>
            {
                btn.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
            });
            btn.RegisterCallback<MouseLeaveEvent>(e =>
            {
                btn.style.scale = new Scale(Vector3.one);
            });

            return btn;
        }

        #endregion

        #region Event Binding

        private void BindEvents()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnSceneLoadStarted += OnSceneLoadStarted;
                SceneLoader.Instance.OnLoadProgress += OnLoadProgress;
                SceneLoader.Instance.OnSceneLoadComplete += OnSceneLoadComplete;
            }
        }

        private void UnbindEvents()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnSceneLoadStarted -= OnSceneLoadStarted;
                SceneLoader.Instance.OnLoadProgress -= OnLoadProgress;
                SceneLoader.Instance.OnSceneLoadComplete -= OnSceneLoadComplete;
            }

            if (playButton != null) playButton.clicked -= OnPlayClicked;
            if (soulForgeButton != null) soulForgeButton.clicked -= OnSoulForgeClicked;
            if (settingsButton != null) settingsButton.clicked -= OnSettingsClicked;
            if (quitButton != null) quitButton.clicked -= OnQuitClicked;
        }

        #endregion

        #region Button Handlers

        private void OnPlayClicked()
        {
            Debug.Log("[MainMenuUI] Play button clicked");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadUnitSelection();
            }
        }

        private void OnSoulForgeClicked()
        {
            Debug.Log("[MainMenuUI] Soul Forge button clicked");
            
            if (soulForgeUI == null)
            {
                soulForgeUI = GetComponent<SoulForgeUI>();
                if (soulForgeUI == null) soulForgeUI = FindFirstObjectByType<SoulForgeUI>();
            }

            if (soulForgeUI != null)
            {
                soulForgeUI.Show();
            }
            else
            {
                Debug.LogError("[MainMenuUI] Soul Forge UI reference is missing!");
            }
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuUI] Settings button clicked");
            SettingsUI.ShowSettings(root, () =>
            {
                Debug.Log("[MainMenuUI] Settings panel closed");
            });
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] Quit button clicked");
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        #endregion

        #region Loading Events

        private void OnSceneLoadStarted(string sceneName)
        {
            if (loadingPanel != null)
            {
                loadingPanel.style.display = DisplayStyle.Flex;
                if (loadingLabel != null) loadingLabel.text = $"LOADING {sceneName.ToUpper()}...";
            }
            if (loadingBar != null) loadingBar.value = 0;
        }

        private void OnLoadProgress(float progress)
        {
            if (loadingBar != null) loadingBar.value = progress;
        }

        private void OnSceneLoadComplete(string sceneName)
        {
            if (loadingPanel != null)
            {
                loadingPanel.style.display = DisplayStyle.None;
            }
        }

        #endregion
    }
}
