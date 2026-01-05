using System;
using UnityEngine.UIElements;

namespace AutoBattler.UI
{
    public class UnitCardUI
    {
        public event Action<UnitCardUI> OnCardClicked;

        public VisualElement Root { get; }
        private Label nameLabel;
        private Label costLabel;
        private Label descLabel;
        private Label roleLabel;
        private Label hpLabel;
        private Label dmgLabel;
        private Button addButton;

        public int Id { get; private set; }
        public bool IsSquadCard { get; private set; }

        public UnitCardUI(VisualElement root, int id, bool isSquadCard = false)
        {
            Root = root;
            Id = id;
            IsSquadCard = isSquadCard;

            nameLabel = Root.Q<Label>("unit-name");
            costLabel = Root.Q<Label>("unit-cost");
            descLabel = Root.Q<Label>("unit-desc");
            roleLabel = Root.Q<Label>("unit-role");
            hpLabel = Root.Q<Label>("unit-hp");
            dmgLabel = Root.Q<Label>("unit-dmg");
            addButton = Root.Q<Button>("add-button");

            if (addButton != null)
            {
                addButton.clicked += () => OnCardClicked?.Invoke(this);
            }
            else
            {
                Root.RegisterCallback<ClickEvent>(_ => OnCardClicked?.Invoke(this));
            }
        }

        public void UpdateFromData(string unitName, int cost, string role, string desc, int hp, int dmg)
        {
            if (nameLabel != null) nameLabel.text = unitName;
            if (costLabel != null) costLabel.text = $"{cost}g";
            if (roleLabel != null) roleLabel.text = role;
            if (descLabel != null) descLabel.text = desc;
            if (hpLabel != null) hpLabel.text = hp > 0 ? hp.ToString() : "--";
            if (dmgLabel != null) dmgLabel.text = dmg > 0 ? dmg.ToString() : "--";
        }

        public void SetUnit(AutoBattler.Data.UnitStatsSO unit, string displayNameOverride = null, string roleDescriptor = null)
        {
            if (unit == null) { Clear(); return; }
            string name = string.IsNullOrWhiteSpace(displayNameOverride) ? unit.unitName : displayNameOverride;
            int cost = unit.goldCost;
            string desc = unit.rarity.ToString();
            string role = string.IsNullOrWhiteSpace(roleDescriptor)
                ? (string.IsNullOrWhiteSpace(displayNameOverride) ? "Specialist" : displayNameOverride)
                : roleDescriptor;
            UpdateFromData(name, cost, role, desc, unit.maxHealth, unit.attackDamage);

            // Attempt to set icon if available
            var preview = Root.Q<VisualElement>("unit-preview");
            if (preview != null && unit.icon != null)
            {
                preview.style.backgroundImage = new StyleBackground(unit.icon);
            }

            SetInteractable(true);
        }

        public void Clear()
        {
            UpdateFromData("Empty", 0, "Unassigned", "", 0, 0);
            Root.AddToClassList("empty");
            SetInteractable(false);
        }

        public void SetInteractable(bool enabled)
        {
            if (addButton != null) addButton.SetEnabled(enabled);
            if (enabled) Root.RemoveFromClassList("empty");
        }
    }
}