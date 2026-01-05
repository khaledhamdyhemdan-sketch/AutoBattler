using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.UI;

namespace AutoBattler.UI.Tests
{
    /// <summary>
    /// Lightweight smoke test runner for UI - runs at startup in development builds and sanity-checks templates and key screens.
    /// Logs findings to the console for manual verification.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIAutomatedSmokeTest : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Small delay for other systems to initialize
            yield return new WaitForSeconds(0.25f);

            Debug.Log("[UIAutomatedSmokeTest] Starting smoke tests...");

            // Test: Load templates
            var b = UIFactory.LoadTemplate("UI/Templates/ButtonTemplate");
            Debug.Log(b != null ? "ButtonTemplate loaded" : "ButtonTemplate missing");

            var panel = UIFactory.LoadTemplate("UI/Templates/PanelTemplate");
            Debug.Log(panel != null ? "PanelTemplate loaded" : "PanelTemplate missing");

            var unit = UIFactory.LoadTemplate("UI/Templates/UnitCardTemplate");
            Debug.Log(unit != null ? "UnitCardTemplate loaded" : "UnitCardTemplate missing");

            // Test: instantiate templates and ensure they have expected named controls
            var btn = UIFactory.LoadTemplate("UI/Templates/ButtonTemplate");
            var label = btn?.Q<Label>("label");
            Debug.Log(label != null ? $"Button label: {label.text}" : "Button template missing label");

            // Test: try creating a UnitCard via API
            var cardElement = UIFactory.LoadTemplate("UI/Templates/UnitCardTemplate");
            var card = new UnitCardUI(cardElement, 99);
            card.UpdateFromData("Test Knight", 50, "Vanguard", "Testing unit card", 120, 20);
            Debug.Log("UnitCardUI created and updated");

            // UIAnimator quick test
            AutoBattler.UIAnimator.Pop(btn, 0.12f);
            AutoBattler.UIAnimator.Pulse(cardElement, 0.3f);

            // Test: Shop integration with GameManager
            if (AutoBattler.Core.GameManager.Instance == null)
            {
                var go = new GameObject("TestGameManager");
                go.AddComponent<AutoBattler.Core.GameManager>();
                yield return null; // allow Awake to run
            }

            var shopGO = new GameObject("ShopTest");
            var shopDoc = shopGO.AddComponent<UIDocument>();
            var shop = shopGO.AddComponent<AutoBattler.UI.ShopUI>();
            shop.enabled = true;
            yield return new WaitForSeconds(0.1f);

            bool bought = shop.TryBuyCard(0);
            Debug.Log(bought ? "Shop purchase test: success" : "Shop purchase test: failed (expected if not enough gold)");

            // Instantiate Main Menu and HUD to ensure they initialize without errors
            var mainGO = new GameObject("MainMenuTest");
            mainGO.AddComponent<UIDocument>();
            var main = mainGO.AddComponent<AutoBattler.UI.MainMenuUI>();
            main.enabled = true;

            var hudGO = new GameObject("HUDTest");
            hudGO.AddComponent<UIDocument>();
            var hud = hudGO.AddComponent<AutoBattler.UI.GameHUDUI>();
            hud.enabled = true;

            yield return new WaitForSeconds(0.1f);

            Debug.Log("MainMenuUI and GameHUDUI instantiated successfully");

            Debug.Log("[UIAutomatedSmokeTest] Smoke tests finished");
        }
    }
}