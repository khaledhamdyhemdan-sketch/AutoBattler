using UnityEngine;
using UnityEngine.UIElements;

namespace AutoBattler.UI
{
    /// <summary>
    /// Simple UI factory for creating styled elements programmatically or by loading UXML templates.
    /// Keep lightweight to avoid complicating editor-time asset dependencies.
    /// </summary>
    public static class UIFactory
    {
        public static Button CreateButton(string text, string variant = "primary")
        {
            var btn = new Button();
            btn.text = text;
            btn.AddToClassList("button");
            btn.AddToClassList($"button-{variant}");
            btn.AddToClassList("focusable");

            // Add simple click pop feedback
            btn.clicked += () => AutoBattler.UIAnimator.Pop(btn, 0.16f);

            // Mouse/keyboard focus pulse
            btn.RegisterCallback<MouseEnterEvent>((e) => AutoBattler.UIAnimator.Pulse(btn, 0.22f));

            return btn;
        }

        public static VisualElement CreatePanel(string variant = "default")
        {
            var panel = new VisualElement();
            panel.AddToClassList("panel");
            if (variant == "light") panel.AddToClassList("panel--light");
            if (variant == "metal") panel.AddToClassList("panel--metal");
            return panel;
        }

        public static VisualElement LoadTemplate(string resourcePath)
        {
            var vta = Resources.Load<VisualTreeAsset>(resourcePath);
            if (vta != null)
            {
                return vta.CloneTree();
            }
            Debug.LogWarning($"[UIFactory] Template not found at Resources/{resourcePath}");
            return new VisualElement();
        }
    }
}