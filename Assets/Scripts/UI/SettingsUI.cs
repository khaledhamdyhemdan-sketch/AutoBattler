using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AutoBattler.UI
{
    /// <summary>
    /// Settings Panel UI - The Void style with medieval iron/gold aesthetic
    /// Programmatic UI construction for Unity 6.3 reliability
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
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

        // Settings Keys
        private const string KEY_MASTER_VOLUME = "MasterVolume";
        private const string KEY_MUSIC_VOLUME = "MusicVolume";
        private const string KEY_SFX_VOLUME = "SFXVolume";
        private const string KEY_QUALITY = "GraphicsQuality";
        private const string KEY_FULLSCREEN = "Fullscreen";
        private const string KEY_VSYNC = "VSync";
        private const string KEY_SCREEN_SHAKE = "ScreenShake";
        private const string KEY_SHOW_DAMAGE = "ShowDamageNumbers";

        // UI Elements
        private VisualElement root;
        private VisualElement settingsPanel;
        private Slider masterVolumeSlider;
        private Slider musicVolumeSlider;
        private Slider sfxVolumeSlider;
        private Label masterVolumeValue;
        private Label musicVolumeValue;
        private Label sfxVolumeValue;
        private DropdownField qualityDropdown;
        private Toggle fullscreenToggle;
        private Toggle vsyncToggle;
        private Toggle screenShakeToggle;
        private Toggle showDamageToggle;
        private Button closeButton;
        private Button applyButton;
        private Button resetButton;

        // Callback for when settings are closed
        public System.Action OnSettingsClosed;

        #region Public Methods

        public void Show(VisualElement parentRoot)
        {
            root = parentRoot;
            BuildUI();
            LoadSettings();
        }

        public void Hide()
        {
            if (settingsPanel != null && settingsPanel.parent != null)
            {
                settingsPanel.parent.Remove(settingsPanel);
            }
            settingsPanel = null;
        }

        #endregion

        #region UI Construction

        private void BuildUI()
        {
            // Create overlay container
            settingsPanel = new VisualElement();
            settingsPanel.name = "settings-overlay";
            settingsPanel.style.position = Position.Absolute;
            settingsPanel.style.left = 0;
            settingsPanel.style.top = 0;
            settingsPanel.style.right = 0;
            settingsPanel.style.bottom = 0;
            settingsPanel.style.backgroundColor = new Color(0.02f, 0.02f, 0.02f, 0.9f);
            settingsPanel.style.alignItems = Align.Center;
            settingsPanel.style.justifyContent = Justify.Center;

            // Load stylesheet
            var styleSheet = Resources.Load<StyleSheet>("UI/Settings");
            if (styleSheet != null) settingsPanel.styleSheets.Add(styleSheet);

            // Click outside to close (optional)
            settingsPanel.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == settingsPanel) CloseSettings();
            });

            // Main panel
            var panel = new VisualElement();
            panel.name = "settings-panel";
            panel.style.width = 640;
            panel.style.maxHeight = new Length(85, LengthUnit.Percent);
            panel.style.backgroundColor = Stone900;
            panel.style.borderTopWidth = 2;
            panel.style.borderBottomWidth = 2;
            panel.style.borderLeftWidth = 2;
            panel.style.borderRightWidth = 2;
            panel.style.borderTopColor = Stone700;
            panel.style.borderBottomColor = Stone700;
            panel.style.borderLeftColor = Stone700;
            panel.style.borderRightColor = Stone700;
            panel.style.borderTopLeftRadius = 12;
            panel.style.borderTopRightRadius = 12;
            panel.style.borderBottomLeftRadius = 12;
            panel.style.borderBottomRightRadius = 12;
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.overflow = Overflow.Hidden;

            // Build sections
            BuildHeader(panel);
            BuildContent(panel);
            BuildFooter(panel);

            settingsPanel.Add(panel);
            root.Add(settingsPanel);

            Debug.Log("[SettingsUI] Settings panel displayed");
        }

        private void BuildHeader(VisualElement panel)
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.paddingLeft = 32;
            header.style.paddingRight = 32;
            header.style.paddingTop = 24;
            header.style.paddingBottom = 24;
            header.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = Stone700;

            var title = new Label("SETTINGS");
            title.style.fontSize = 28;
            title.style.color = MedievalGold;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 4;
            header.Add(title);

            closeButton = new Button(() => CloseSettings());
            closeButton.text = "✕";
            closeButton.style.width = 40;
            closeButton.style.height = 40;
            closeButton.style.fontSize = 20;
            closeButton.style.color = Stone500;
            closeButton.style.backgroundColor = Color.clear;
            closeButton.style.borderTopWidth = 1;
            closeButton.style.borderBottomWidth = 1;
            closeButton.style.borderLeftWidth = 1;
            closeButton.style.borderRightWidth = 1;
            closeButton.style.borderTopColor = Stone700;
            closeButton.style.borderBottomColor = Stone700;
            closeButton.style.borderLeftColor = Stone700;
            closeButton.style.borderRightColor = Stone700;
            closeButton.style.borderTopLeftRadius = 8;
            closeButton.style.borderTopRightRadius = 8;
            closeButton.style.borderBottomLeftRadius = 8;
            closeButton.style.borderBottomRightRadius = 8;
            closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeButton.RegisterCallback<MouseEnterEvent>(e =>
            {
                closeButton.style.backgroundColor = new Color(BloodDeep.r, BloodDeep.g, BloodDeep.b, 0.3f);
                closeButton.style.borderTopColor = BloodDeep;
                closeButton.style.borderBottomColor = BloodDeep;
                closeButton.style.borderLeftColor = BloodDeep;
                closeButton.style.borderRightColor = BloodDeep;
                closeButton.style.color = DangerRed;
            });
            closeButton.RegisterCallback<MouseLeaveEvent>(e =>
            {
                closeButton.style.backgroundColor = Color.clear;
                closeButton.style.borderTopColor = Stone700;
                closeButton.style.borderBottomColor = Stone700;
                closeButton.style.borderLeftColor = Stone700;
                closeButton.style.borderRightColor = Stone700;
                closeButton.style.color = Stone500;
            });
            header.Add(closeButton);

            panel.Add(header);
        }

        private void BuildContent(VisualElement panel)
        {
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingLeft = 32;
            scrollView.style.paddingRight = 32;
            scrollView.style.paddingTop = 24;
            scrollView.style.paddingBottom = 24;

            // Audio Section
            BuildAudioSection(scrollView);

            // Graphics Section
            BuildGraphicsSection(scrollView);

            // Gameplay Section
            BuildGameplaySection(scrollView);

            // Data Section
            BuildDataSection(scrollView);

            panel.Add(scrollView);
        }

        private void BuildAudioSection(VisualElement parent)
        {
            var section = CreateSection("AUDIO");

            // Master Volume
            var masterRow = CreateSettingRow("Master Volume");
            masterVolumeValue = CreateValueLabel("100%");
            masterVolumeSlider = CreateSlider(0, 100, 100);
            masterVolumeSlider.RegisterValueChangedCallback(e =>
            {
                masterVolumeValue.text = $"{Mathf.RoundToInt(e.newValue)}%";
            });
            masterRow.Add(masterVolumeValue);
            masterRow.Add(masterVolumeSlider);
            section.Add(masterRow);

            // Music Volume
            var musicRow = CreateSettingRow("Music Volume");
            musicVolumeValue = CreateValueLabel("80%");
            musicVolumeSlider = CreateSlider(0, 100, 80);
            musicVolumeSlider.RegisterValueChangedCallback(e =>
            {
                musicVolumeValue.text = $"{Mathf.RoundToInt(e.newValue)}%";
            });
            musicRow.Add(musicVolumeValue);
            musicRow.Add(musicVolumeSlider);
            section.Add(musicRow);

            // SFX Volume
            var sfxRow = CreateSettingRow("Sound Effects");
            sfxVolumeValue = CreateValueLabel("100%");
            sfxVolumeSlider = CreateSlider(0, 100, 100);
            sfxVolumeSlider.RegisterValueChangedCallback(e =>
            {
                sfxVolumeValue.text = $"{Mathf.RoundToInt(e.newValue)}%";
            });
            sfxRow.Add(sfxVolumeValue);
            sfxRow.Add(sfxVolumeSlider);
            section.Add(sfxRow);

            parent.Add(section);
        }

        private void BuildGraphicsSection(VisualElement parent)
        {
            var section = CreateSection("GRAPHICS");

            // Quality
            var qualityRow = CreateSettingRow("Quality");
            qualityDropdown = new DropdownField();
            qualityDropdown.choices = new List<string> { "Low", "Medium", "High", "Ultra" };
            qualityDropdown.value = GetQualityName(QualitySettings.GetQualityLevel());
            qualityDropdown.style.width = 200;
            qualityDropdown.style.height = 40;
            qualityDropdown.style.backgroundColor = Stone800;
            qualityDropdown.style.borderTopWidth = 1;
            qualityDropdown.style.borderBottomWidth = 1;
            qualityDropdown.style.borderLeftWidth = 1;
            qualityDropdown.style.borderRightWidth = 1;
            qualityDropdown.style.borderTopColor = Stone700;
            qualityDropdown.style.borderBottomColor = Stone700;
            qualityDropdown.style.borderLeftColor = Stone700;
            qualityDropdown.style.borderRightColor = Stone700;
            qualityDropdown.style.borderTopLeftRadius = 6;
            qualityDropdown.style.borderTopRightRadius = 6;
            qualityDropdown.style.borderBottomLeftRadius = 6;
            qualityDropdown.style.borderBottomRightRadius = 6;
            qualityDropdown.style.color = Stone400;
            qualityRow.Add(qualityDropdown);
            section.Add(qualityRow);

            // Fullscreen
            var fullscreenRow = CreateSettingRow("Fullscreen");
            fullscreenToggle = CreateToggle(Screen.fullScreen);
            fullscreenRow.Add(fullscreenToggle);
            section.Add(fullscreenRow);

            // VSync
            var vsyncRow = CreateSettingRow("VSync");
            vsyncToggle = CreateToggle(QualitySettings.vSyncCount > 0);
            vsyncRow.Add(vsyncToggle);
            section.Add(vsyncRow);

            parent.Add(section);
        }

        private void BuildGameplaySection(VisualElement parent)
        {
            var section = CreateSection("GAMEPLAY");

            // Screen Shake
            var shakeRow = CreateSettingRow("Screen Shake");
            screenShakeToggle = CreateToggle(true);
            shakeRow.Add(screenShakeToggle);
            section.Add(shakeRow);

            // Show Damage Numbers
            var damageRow = CreateSettingRow("Damage Numbers");
            showDamageToggle = CreateToggle(true);
            damageRow.Add(showDamageToggle);
            section.Add(damageRow);

            parent.Add(section);
        }

        private void BuildDataSection(VisualElement parent)
        {
            var section = CreateSection("DATA");

            // Reset Progress
            var resetRow = new VisualElement();
            resetRow.style.flexDirection = FlexDirection.Column;
            resetRow.style.paddingTop = 12;
            resetRow.style.paddingBottom = 12;

            var resetLabel = new Label("Reset all game progress and unlocks");
            resetLabel.style.fontSize = 14;
            resetLabel.style.color = Stone500;
            resetLabel.style.marginBottom = 12;
            resetRow.Add(resetLabel);

            resetButton = CreateButton("RESET PROGRESS", ButtonType.Danger);
            resetButton.clicked += OnResetProgressClicked;
            resetRow.Add(resetButton);

            section.Add(resetRow);
            parent.Add(section);
        }

        private void BuildFooter(VisualElement panel)
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.paddingLeft = 32;
            footer.style.paddingRight = 32;
            footer.style.paddingTop = 20;
            footer.style.paddingBottom = 20;
            footer.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = Stone700;

            var cancelBtn = CreateButton("CANCEL", ButtonType.Secondary);
            cancelBtn.clicked += () => CloseSettings();
            footer.Add(cancelBtn);

            applyButton = CreateButton("APPLY", ButtonType.Primary);
            applyButton.clicked += OnApplyClicked;
            footer.Add(applyButton);

            panel.Add(footer);
        }

        #endregion

        #region UI Helpers

        private VisualElement CreateSection(string title)
        {
            var section = new VisualElement();
            section.style.flexDirection = FlexDirection.Column;
            section.style.marginBottom = 32;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 14;
            titleLabel.style.color = Stone500;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.letterSpacing = 3;
            titleLabel.style.marginBottom = 16;
            titleLabel.style.paddingBottom = 8;
            titleLabel.style.borderBottomWidth = 1;
            titleLabel.style.borderBottomColor = Stone800;
            section.Add(titleLabel);

            return section;
        }

        private VisualElement CreateSettingRow(string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.minHeight = 48;

            var labelElement = new Label(label);
            labelElement.style.fontSize = 16;
            labelElement.style.color = Stone400;
            labelElement.style.flexGrow = 1;
            row.Add(labelElement);

            return row;
        }

        private Label CreateValueLabel(string value)
        {
            var label = new Label(value);
            label.style.fontSize = 14;
            label.style.color = Stone500;
            label.style.minWidth = 60;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.marginRight = 12;
            return label;
        }

        private Slider CreateSlider(float min, float max, float value)
        {
            var slider = new Slider(min, max);
            slider.value = value;
            slider.style.width = 200;
            slider.style.height = 24;
            return slider;
        }

        private Toggle CreateToggle(bool value)
        {
            var toggle = new Toggle();
            toggle.value = value;
            toggle.style.flexDirection = FlexDirection.RowReverse;

            // Style the checkmark
            var checkmark = toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                checkmark.style.width = 48;
                checkmark.style.height = 24;
                checkmark.style.backgroundColor = value ? 
                    new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.3f) : 
                    Stone800;
                checkmark.style.borderTopWidth = 1;
                checkmark.style.borderBottomWidth = 1;
                checkmark.style.borderLeftWidth = 1;
                checkmark.style.borderRightWidth = 1;
                checkmark.style.borderTopColor = value ? MedievalGold : Stone700;
                checkmark.style.borderBottomColor = value ? MedievalGold : Stone700;
                checkmark.style.borderLeftColor = value ? MedievalGold : Stone700;
                checkmark.style.borderRightColor = value ? MedievalGold : Stone700;
                checkmark.style.borderTopLeftRadius = 12;
                checkmark.style.borderTopRightRadius = 12;
                checkmark.style.borderBottomLeftRadius = 12;
                checkmark.style.borderBottomRightRadius = 12;
            }

            toggle.RegisterValueChangedCallback(e =>
            {
                if (checkmark != null)
                {
                    checkmark.style.backgroundColor = e.newValue ? 
                        new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.3f) : 
                        Stone800;
                    checkmark.style.borderTopColor = e.newValue ? MedievalGold : Stone700;
                    checkmark.style.borderBottomColor = e.newValue ? MedievalGold : Stone700;
                    checkmark.style.borderLeftColor = e.newValue ? MedievalGold : Stone700;
                    checkmark.style.borderRightColor = e.newValue ? MedievalGold : Stone700;
                }
            });

            return toggle;
        }

        private enum ButtonType { Primary, Secondary, Danger }

        private Button CreateButton(string text, ButtonType type)
        {
            var btn = new Button();
            btn.text = text;
            btn.style.height = 48;
            btn.style.minWidth = 140;
            btn.style.paddingLeft = 24;
            btn.style.paddingRight = 24;
            btn.style.fontSize = 14;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.letterSpacing = 2;
            btn.style.borderTopWidth = type == ButtonType.Primary ? 2 : 1;
            btn.style.borderBottomWidth = type == ButtonType.Primary ? 2 : 1;
            btn.style.borderLeftWidth = type == ButtonType.Primary ? 2 : 1;
            btn.style.borderRightWidth = type == ButtonType.Primary ? 2 : 1;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;

            Color bgColor, borderColor, textColor;
            Color hoverBgColor, hoverBorderColor;

            switch (type)
            {
                case ButtonType.Primary:
                    bgColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.15f);
                    borderColor = MedievalGold;
                    textColor = MedievalGold;
                    hoverBgColor = new Color(MedievalGold.r, MedievalGold.g, MedievalGold.b, 0.25f);
                    hoverBorderColor = MedievalGold;
                    break;
                case ButtonType.Danger:
                    bgColor = new Color(BloodDeep.r, BloodDeep.g, BloodDeep.b, 0.2f);
                    borderColor = BloodDeep;
                    textColor = DangerRed;
                    hoverBgColor = new Color(BloodDeep.r, BloodDeep.g, BloodDeep.b, 0.4f);
                    hoverBorderColor = DangerRed;
                    break;
                default: // Secondary
                    bgColor = new Color(0, 0, 0, 0.6f);
                    borderColor = Stone700;
                    textColor = Stone400;
                    hoverBgColor = new Color(Stone700.r, Stone700.g, Stone700.b, 0.4f);
                    hoverBorderColor = Stone500;
                    break;
            }

            btn.style.backgroundColor = bgColor;
            btn.style.borderTopColor = borderColor;
            btn.style.borderBottomColor = borderColor;
            btn.style.borderLeftColor = borderColor;
            btn.style.borderRightColor = borderColor;
            btn.style.color = textColor;

            btn.RegisterCallback<MouseEnterEvent>(e =>
            {
                btn.style.backgroundColor = hoverBgColor;
                btn.style.borderTopColor = hoverBorderColor;
                btn.style.borderBottomColor = hoverBorderColor;
                btn.style.borderLeftColor = hoverBorderColor;
                btn.style.borderRightColor = hoverBorderColor;
                if (type == ButtonType.Primary)
                    btn.style.scale = new Scale(new Vector3(1.02f, 1.02f, 1f));
            });

            btn.RegisterCallback<MouseLeaveEvent>(e =>
            {
                btn.style.backgroundColor = bgColor;
                btn.style.borderTopColor = borderColor;
                btn.style.borderBottomColor = borderColor;
                btn.style.borderLeftColor = borderColor;
                btn.style.borderRightColor = borderColor;
                btn.style.scale = new Scale(Vector3.one);
            });

            return btn;
        }

        private string GetQualityName(int level)
        {
            return level switch
            {
                0 => "Low",
                1 => "Medium",
                2 => "High",
                _ => "Ultra"
            };
        }

        private int GetQualityLevel(string name)
        {
            return name switch
            {
                "Low" => 0,
                "Medium" => 1,
                "High" => 2,
                "Ultra" => 3,
                _ => 2
            };
        }

        #endregion

        #region Settings Management

        private void LoadSettings()
        {
            // Load saved settings
            masterVolumeSlider.value = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 100f);
            musicVolumeSlider.value = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 80f);
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 100f);
            
            int qualityLevel = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
            qualityDropdown.value = GetQualityName(qualityLevel);
            
            fullscreenToggle.value = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
            vsyncToggle.value = PlayerPrefs.GetInt(KEY_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
            screenShakeToggle.value = PlayerPrefs.GetInt(KEY_SCREEN_SHAKE, 1) == 1;
            showDamageToggle.value = PlayerPrefs.GetInt(KEY_SHOW_DAMAGE, 1) == 1;

            // Update value labels
            masterVolumeValue.text = $"{Mathf.RoundToInt(masterVolumeSlider.value)}%";
            musicVolumeValue.text = $"{Mathf.RoundToInt(musicVolumeSlider.value)}%";
            sfxVolumeValue.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value)}%";

            Debug.Log("[SettingsUI] Settings loaded");
        }

        private void SaveSettings()
        {
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, masterVolumeSlider.value);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, musicVolumeSlider.value);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolumeSlider.value);
            PlayerPrefs.SetInt(KEY_QUALITY, GetQualityLevel(qualityDropdown.value));
            PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreenToggle.value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_VSYNC, vsyncToggle.value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SCREEN_SHAKE, screenShakeToggle.value ? 1 : 0);
            PlayerPrefs.SetInt(KEY_SHOW_DAMAGE, showDamageToggle.value ? 1 : 0);
            PlayerPrefs.Save();

            // Apply graphics settings immediately
            QualitySettings.SetQualityLevel(GetQualityLevel(qualityDropdown.value));
            Screen.fullScreen = fullscreenToggle.value;
            QualitySettings.vSyncCount = vsyncToggle.value ? 1 : 0;

            // Apply audio settings (if AudioListener or AudioMixer exists)
            AudioListener.volume = masterVolumeSlider.value / 100f;

            Debug.Log("[SettingsUI] Settings saved and applied");
        }

        #endregion

        #region Button Handlers

        private void OnApplyClicked()
        {
            SaveSettings();
            CloseSettings();
        }

        private void OnResetProgressClicked()
        {
            // Show confirmation
            Debug.Log("[SettingsUI] Reset progress requested - clearing all PlayerPrefs");
            
            // Clear all game progress (keep settings)
            float masterVol = masterVolumeSlider.value;
            float musicVol = musicVolumeSlider.value;
            float sfxVol = sfxVolumeSlider.value;
            int quality = GetQualityLevel(qualityDropdown.value);
            int fullscreen = fullscreenToggle.value ? 1 : 0;
            int vsync = vsyncToggle.value ? 1 : 0;
            int shake = screenShakeToggle.value ? 1 : 0;
            int damage = showDamageToggle.value ? 1 : 0;

            // Clear all
            PlayerPrefs.DeleteAll();

            // Restore settings
            PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, masterVol);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, musicVol);
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVol);
            PlayerPrefs.SetInt(KEY_QUALITY, quality);
            PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen);
            PlayerPrefs.SetInt(KEY_VSYNC, vsync);
            PlayerPrefs.SetInt(KEY_SCREEN_SHAKE, shake);
            PlayerPrefs.SetInt(KEY_SHOW_DAMAGE, damage);
            PlayerPrefs.Save();

            Debug.Log("[SettingsUI] Game progress reset, settings preserved");
        }

        private void CloseSettings()
        {
            Hide();
            OnSettingsClosed?.Invoke();
        }

        #endregion

        #region Static Access

        private static SettingsUI _instance;

        public static void ShowSettings(VisualElement parentRoot, System.Action onClosed = null)
        {
            if (_instance == null)
            {
                var go = new GameObject("SettingsUI");
                _instance = go.AddComponent<SettingsUI>();
                DontDestroyOnLoad(go);
            }

            _instance.OnSettingsClosed = onClosed;
            _instance.Show(parentRoot);
        }

        public static void HideSettings()
        {
            if (_instance != null)
            {
                _instance.Hide();
            }
        }

        #endregion
    }
}
