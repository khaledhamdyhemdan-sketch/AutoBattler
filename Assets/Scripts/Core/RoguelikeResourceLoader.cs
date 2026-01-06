using UnityEngine;
using UnityEngine.UIElements;
using AutoBattler.Data;

namespace AutoBattler.Core
{
    /// <summary>
    /// Runtime helper to load and assign resources to RoguelikeSystemSetup
    /// Attach this to the same GameObject as RoguelikeSystemSetup
    /// </summary>
    [RequireComponent(typeof(RoguelikeSystemSetup))]
    public class RoguelikeResourceLoader : MonoBehaviour
    {
        private void Awake()
        {
            var setup = GetComponent<RoguelikeSystemSetup>();
            if (setup == null) return;

            // Load resources at runtime
            var panelSettings = Resources.Load<PanelSettings>("Settings/GameHUDPanelSettings");
            var draftUXML = Resources.Load<VisualTreeAsset>("UI/Draft/DraftUI");
            var lotteryUXML = Resources.Load<VisualTreeAsset>("UI/Lottery/LotteryUI");
            var equipmentDB = Resources.Load<EquipmentDatabaseSO>("ScriptableObjects/EquipmentDatabase");

            // Use reflection to assign
            var setupType = typeof(RoguelikeSystemSetup);
            
            var panelSettingsField = setupType.GetField("panelSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var draftUXMLField = setupType.GetField("draftUXML", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lotteryUXMLField = setupType.GetField("lotteryUXML", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var equipmentDBField = setupType.GetField("equipmentDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (panelSettings != null) panelSettingsField?.SetValue(setup, panelSettings);
            if (draftUXML != null) draftUXMLField?.SetValue(setup, draftUXML);
            if (lotteryUXML != null) lotteryUXMLField?.SetValue(setup, lotteryUXML);
            if (equipmentDB != null) equipmentDBField?.SetValue(setup, equipmentDB);

            Debug.Log($"[RoguelikeResourceLoader] Loaded resources: Panel={panelSettings!=null}, Draft={draftUXML!=null}, Lottery={lotteryUXML!=null}, DB={equipmentDB!=null}");
        }
    }
}
