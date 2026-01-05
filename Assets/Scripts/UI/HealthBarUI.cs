using UnityEngine;
using UnityEngine.UIElements;

namespace AutoBattler.UI
{
    /// <summary>
    /// Unity 6.3 UI Toolkit health bar - programmatic construction for reliability
    /// Medieval iron/gold theme matching The Void aesthetic
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private bool worldSpaceUI = true;
        [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);

        [Header("Appearance")]
        [SerializeField] private float barWidth = 60f;
        [SerializeField] private float barHeight = 6f;
        [SerializeField] private bool showText = false;

        // Colors
        private static readonly Color HealthHigh = new Color(0.2f, 0.8f, 0.4f); // Bright Green
        private static readonly Color HealthMedium = new Color(1f, 0.8f, 0.2f); // Bright Yellow
        private static readonly Color HealthLow = new Color(1f, 0.3f, 0.3f); // Bright Red
        private static readonly Color BorderColor = new Color(0f, 0f, 0f, 0.8f); // Black border
        private static readonly Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        // UI Elements
        private VisualElement root;
        private VisualElement healthBarContainer;
        private VisualElement healthBarBackground;
        private VisualElement healthBarFill;
        private Label healthText;

        private int maxHealth = 100;
        private int currentHealth = 100;

        private Camera mainCamera;

        #region Unity Lifecycle

        private void Awake()
        {
            mainCamera = Camera.main;

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            InitializeHealthBar();
        }

        private void LateUpdate()
        {
            if (worldSpaceUI && healthBarContainer != null)
            {
                UpdateWorldSpacePosition();
            }
        }

        #endregion

        #region Initialization

        private void InitializeHealthBar()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            // Ensure PanelSettings is assigned
            if (uiDocument.panelSettings == null)
            {
                uiDocument.panelSettings = Resources.Load<PanelSettings>("Settings/GameHUDPanelSettings");
                if (uiDocument.panelSettings == null)
                {
                    Debug.LogWarning("[HealthBarUI] No PanelSettings found!");
                }
            }

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            root.Clear();

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("UI/Styles/HealthBar");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            // Build UI programmatically for reliability
            BuildHealthBar();
            UpdateHealthBar();
        }

        private void BuildHealthBar()
        {
            // Container
            healthBarContainer = new VisualElement();
            healthBarContainer.name = "health-bar-container";
            healthBarContainer.AddToClassList("health-bar-container");
            healthBarContainer.style.width = barWidth;
            healthBarContainer.style.height = barHeight + (showText ? 16 : 0);
            healthBarContainer.style.flexDirection = FlexDirection.Column;
            healthBarContainer.style.alignItems = Align.Center;
            healthBarContainer.style.justifyContent = Justify.Center;
            healthBarContainer.style.position = Position.Absolute;

            // Background
            healthBarBackground = new VisualElement();
            healthBarBackground.name = "health-bar-background";
            healthBarBackground.style.width = new Length(100, LengthUnit.Percent);
            healthBarBackground.style.height = barHeight;
            healthBarBackground.style.backgroundColor = BackgroundColor;
            healthBarBackground.style.borderTopLeftRadius = 2;
            healthBarBackground.style.borderTopRightRadius = 2;
            healthBarBackground.style.borderBottomLeftRadius = 2;
            healthBarBackground.style.borderBottomRightRadius = 2;
            healthBarBackground.style.borderTopWidth = 1;
            healthBarBackground.style.borderBottomWidth = 1;
            healthBarBackground.style.borderLeftWidth = 1;
            healthBarBackground.style.borderRightWidth = 1;
            healthBarBackground.style.borderTopColor = BorderColor;
            healthBarBackground.style.borderBottomColor = BorderColor;
            healthBarBackground.style.borderLeftColor = BorderColor;
            healthBarBackground.style.borderRightColor = BorderColor;
            healthBarBackground.style.overflow = Overflow.Hidden;
            healthBarBackground.style.flexDirection = FlexDirection.Row;
            healthBarBackground.style.alignItems = Align.Stretch;

            // Fill bar
            healthBarFill = new VisualElement();
            healthBarFill.name = "health-bar-fill";
            healthBarFill.style.width = new Length(100, LengthUnit.Percent);
            healthBarFill.style.height = new Length(100, LengthUnit.Percent);
            healthBarFill.style.backgroundColor = HealthHigh;
            
            healthBarBackground.Add(healthBarFill);
            healthBarContainer.Add(healthBarBackground);

            // Text label
            if (showText)
            {
                healthText = new Label();
                healthText.name = "health-text";
                healthText.AddToClassList("health-bar-text");
                healthText.text = $"{currentHealth}/{maxHealth}";
                healthText.style.fontSize = 11;
                healthText.style.color = Color.white;
                healthText.style.unityFontStyleAndWeight = FontStyle.Bold;
                healthText.style.unityTextAlign = TextAnchor.MiddleCenter;
                healthText.style.marginTop = 2;
                healthText.style.letterSpacing = 1;
                healthBarContainer.Add(healthText);
            }

            root.Add(healthBarContainer);
        }

        #endregion

        #region Health Management

        public void SetMaxHealth(int max)
        {
            maxHealth = Mathf.Max(1, max);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            UpdateHealthBar();
        }

        public void SetHealth(int health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            UpdateHealthBar();
        }

        public void SetHealthImmediate(int health, int max)
        {
            maxHealth = Mathf.Max(1, max);
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            UpdateHealthBar();
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill == null || maxHealth <= 0) return;

            float healthPercent = (float)currentHealth / maxHealth;

            // Update fill width
            healthBarFill.style.width = new Length(healthPercent * 100, LengthUnit.Percent);

            // Update color based on health percentage
            Color barColor = GetHealthColor(healthPercent);
            healthBarFill.style.backgroundColor = barColor;

            // Update text
            if (healthText != null)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }
        }

        private Color GetHealthColor(float percent)
        {
            if (percent > 0.6f)
            {
                return HealthHigh; // Green
            }
            else if (percent > 0.3f)
            {
                // Lerp from yellow to green
                float t = (percent - 0.3f) / 0.3f;
                return Color.Lerp(HealthMedium, HealthHigh, t);
            }
            else
            {
                // Lerp from red to yellow
                float t = percent / 0.3f;
                return Color.Lerp(HealthLow, HealthMedium, t);
            }
        }

        #endregion

        #region World Space Positioning

        private void UpdateWorldSpacePosition()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null || healthBarContainer == null || uiDocument == null || uiDocument.rootVisualElement == null) return;

            // Position above the unit's head
            Vector3 worldPos = transform.position + offset;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // Hide if behind camera
            if (screenPos.z < 0)
            {
                healthBarContainer.style.display = DisplayStyle.None;
                return;
            }

            healthBarContainer.style.display = DisplayStyle.Flex;

            // Convert screen position to UI Toolkit coordinates
            var panel = uiDocument.rootVisualElement.panel;
            if (panel == null) return;

            // UI Toolkit uses top-left origin, screen uses bottom-left
            Vector2 localPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPos.x, Screen.height - screenPos.y));

            // Center the health bar on the position
            healthBarContainer.style.left = localPos.x - (barWidth / 2);
            healthBarContainer.style.top = localPos.y - ((barHeight + (showText ? 16 : 0)) / 2);

            // Optional: Scale based on distance for perspective
            float distance = screenPos.z;
            float scale = Mathf.Clamp(10f / distance, 0.5f, 1.5f);
            healthBarContainer.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        #endregion

        #region Public Accessors

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        #endregion
    }
}
