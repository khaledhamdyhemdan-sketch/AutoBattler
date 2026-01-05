using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Core;

namespace AutoBattler.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SoulForgeUI : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private static readonly Color MedievalGold = new Color(0.77f, 0.63f, 0.35f);
        private static readonly Color Stone900 = new Color(0.11f, 0.10f, 0.09f);
        private static readonly Color Stone800 = new Color(0.16f, 0.14f, 0.14f);
        private static readonly Color Stone500 = new Color(0.47f, 0.44f, 0.42f);

        private VisualElement root;
        private VisualElement container;

        public void Show()
        {
            Debug.Log($"[SoulForgeUI] Show called. Enabled: {this.enabled}, Container: {container != null}, Root: {root != null}");
            
            // Ensure GameObject is active
            if (!gameObject.activeSelf)
            {
                Debug.Log("[SoulForgeUI] GameObject was inactive, activating...");
                gameObject.SetActive(true);
            }

            this.enabled = true;

            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            
            // Ensure UIDocument is enabled
            if (uiDocument != null && !uiDocument.enabled)
            {
                Debug.Log("[SoulForgeUI] UIDocument was disabled, enabling...");
                uiDocument.enabled = true;
            }

            // Ensure PanelSettings
            if (uiDocument != null && uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[SoulForgeUI] PanelSettings missing, attempting to load...");
                uiDocument.panelSettings = Resources.Load<PanelSettings>("Settings/GameHUDPanelSettings");
                if (uiDocument.panelSettings == null)
                {
                     // Try alternate path
                     uiDocument.panelSettings = Resources.Load<PanelSettings>("Settings/MainMenuPanelSettings");
                }
            }

            if (container == null)
            {
                Debug.Log("[SoulForgeUI] Container is null, building UI...");
                BuildUI();
            }

            // Ensure container is in the hierarchy (in case MainMenuUI cleared it)
            if (container != null && root != null && !root.Contains(container))
            {
                Debug.Log("[SoulForgeUI] Container not in root, adding it back.");
                root.Add(container);
            }
            
            if (container != null)
            {
                container.style.display = DisplayStyle.Flex;
                container.BringToFront();
                Debug.Log("[SoulForgeUI] Container set to Flex and Brought to Front.");
                
                // Force layout update
                container.MarkDirtyRepaint();
            }
            else
            {
                Debug.LogError("[SoulForgeUI] Container is still null after build attempt!");
            }
        }

        public void Hide()
        {
            if (container != null)
            {
                container.style.display = DisplayStyle.None;
            }
            this.enabled = false;
        }

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            
            // Ensure we are on top if we are a separate document
            if (uiDocument != null)
            {
                uiDocument.sortingOrder = 1000; // High sort order to overlay everything
            }

            if (container == null) BuildUI();
        }

        private void BuildUI()
        {
            if (uiDocument == null) return;
            root = uiDocument.rootVisualElement;
            
            // Do not clear root to allow coexistence with MainMenuUI
            // root.Clear();

            // Create main container (overlay)
            container = new VisualElement();
            container.name = "SoulForgeOverlay";
            container.style.position = Position.Absolute;
            container.style.left = 0;
            container.style.top = 0;
            container.style.right = 0;
            container.style.bottom = 0;
            container.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            container.style.justifyContent = Justify.Center;
            container.style.alignItems = Align.Center;
            container.style.display = DisplayStyle.None; // Hidden by default
            root.Add(container);

            var window = new VisualElement();
            window.style.width = 800;
            window.style.height = 600;
            window.style.backgroundColor = Stone900;
            window.style.borderTopWidth = 2;
            window.style.borderBottomWidth = 2;
            window.style.borderLeftWidth = 2;
            window.style.borderRightWidth = 2;
            window.style.borderTopColor = MedievalGold;
            window.style.borderBottomColor = MedievalGold;
            window.style.borderLeftColor = MedievalGold;
            window.style.borderRightColor = MedievalGold;
            window.style.paddingTop = 20;
            window.style.paddingBottom = 20;
            window.style.paddingLeft = 20;
            window.style.paddingRight = 20;
            // Ensure window is visible
            window.style.display = DisplayStyle.Flex;
            window.style.visibility = Visibility.Visible;
            container.Add(window);

            var title = new Label("SOUL FORGE");
            title.style.fontSize = 32;
            title.style.color = MedievalGold;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.alignSelf = Align.Center;
            title.style.marginBottom = 20;
            window.Add(title);

            var description = new Label("Spend shards to unlock permanent upgrades.");
            description.style.fontSize = 16;
            description.style.color = Stone500;
            description.style.alignSelf = Align.Center;
            description.style.marginBottom = 40;
            window.Add(description);

            // Upgrades Grid
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.justifyContent = Justify.Center;
            window.Add(grid);

            // Example Upgrades
            AddUpgrade(grid, "Health Boost", "+10% Max HP", 100);
            AddUpgrade(grid, "Damage Boost", "+5% Damage", 150);
            AddUpgrade(grid, "Gold Start", "+10 Starting Gold", 200);

            // Close Button
            var closeBtn = new Button();
            closeBtn.text = "RETURN TO MENU";
            closeBtn.style.display = DisplayStyle.Flex;
            closeBtn.style.visibility = Visibility.Visible;
            closeBtn.style.marginTop = 40;
            closeBtn.style.width = 200;
            closeBtn.style.height = 50;
            closeBtn.style.alignSelf = Align.Center;
            closeBtn.style.backgroundColor = Stone800;
            closeBtn.style.color = Color.white;
            closeBtn.style.borderTopWidth = 1;
            closeBtn.style.borderBottomWidth = 1;
            closeBtn.style.borderLeftWidth = 1;
            closeBtn.style.borderRightWidth = 1;
            closeBtn.style.borderTopColor = MedievalGold;
            closeBtn.style.borderBottomColor = MedievalGold;
            closeBtn.style.borderLeftColor = MedievalGold;
            closeBtn.style.borderRightColor = MedievalGold;
            closeBtn.clicked += Hide;
            window.Add(closeBtn);
        }

        private void AddUpgrade(VisualElement parent, string name, string desc, int cost)
        {
            Debug.Log($"[SoulForgeUI] Adding upgrade card: {name}");
            var card = new VisualElement();
            card.style.width = 200;
            card.style.height = 150;
            card.style.marginRight = 10;
            card.style.marginBottom = 10;
            card.style.backgroundColor = Stone800;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = Stone500;
            card.style.borderBottomColor = Stone500;
            card.style.borderLeftColor = Stone500;
            card.style.borderRightColor = Stone500;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.justifyContent = Justify.SpaceBetween;

            var nameLabel = new Label(name);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 16;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(nameLabel);

            var descLabel = new Label(desc);
            descLabel.style.color = Stone500;
            descLabel.style.fontSize = 12;
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            card.Add(descLabel);

            var buyBtn = new Button();
            buyBtn.text = $"{cost} SHARDS";
            buyBtn.style.backgroundColor = MedievalGold;
            buyBtn.style.color = Color.black;
            buyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(buyBtn);

            parent.Add(card);
        }
    }
}
