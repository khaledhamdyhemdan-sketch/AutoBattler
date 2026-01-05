# UI System — Usage Guide

This project uses Unity UI Toolkit (UXML + USS + C#) with a medieval theme.

Quick start:

- Base styles are in `Assets/Resources/UI/Base.uss` — these variables are loaded at runtime by each screen.
- Component styles are in `Assets/Resources/UI/Components/*.uss` (Button, Panel).
- UXML templates are available in `Assets/Resources/UI/Templates/` and can be loaded via `Resources.Load<VisualTreeAsset>("UI/Templates/ButtonTemplate")`.
- Use `AutoBattler.UI.UIFactory` to programmatically create styled elements (buttons/panels) without needing UXML.

Examples:

- Creating a button in code:

  var btn = UIFactory.CreateButton("Play", "primary");
  btn.clicked += OnPlayClicked;
  root.Add(btn);

- Loading a UXML template:

  var banner = UIFactory.LoadTemplate("UI/Templates/BannerTemplate");
  root.Add(banner);

- Creating Unit Cards for shops and selection screens:

  var cardElement = UIFactory.LoadTemplate("UI/Templates/UnitCardTemplate");
  var card = new UnitCardUI(cardElement, index);
  card.UpdateFromData("Swordsman", 20, "Frontline melee unit.");
  root.Add(cardElement);

- Gamepad / keyboard navigation:

  Add the `GamepadNavigator` component to the `UIDocument` game object (or it is automatically added by many screen controllers). Mark buttons with the `focusable` class to include them in focus order.

Design notes:
- The medieval palette avoids blues/purples and uses gold/red/wood tones.
- Keep common tokens in `Base.uss` for consistency.

Next steps:
- Generate VisualTreeAsset variants for complex components (UnitCard) and wire accessibility attributes.
- Add sample scenes demonstrating responsive layouts and gamepad navigation.
