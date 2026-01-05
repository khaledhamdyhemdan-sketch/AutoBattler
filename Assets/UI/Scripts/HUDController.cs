using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Label _phaseLabel;
    private Label _timerLabel;
    private Label _goldLabel;
    private Label _roundLabel;
    private Button _fightButton;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("HUDController: No UIDocument found!");
            return;
        }

        var root = _uiDocument.rootVisualElement;

        // Query Elements
        _phaseLabel = root.Q<Label>("PhaseLabel");
        _timerLabel = root.Q<Label>("TimerLabel");
        _goldLabel = root.Q<Label>("GoldLabel");
        _roundLabel = root.Q<Label>("RoundLabel");
        _fightButton = root.Q<Button>("FightButton");

        // Hook up events
        if (_fightButton != null)
        {
            _fightButton.clicked += OnFightClicked;
        }
    }

    private void OnDisable()
    {
        if (_fightButton != null)
        {
            _fightButton.clicked -= OnFightClicked;
        }
    }

    private void OnFightClicked()
    {
        Debug.Log("Fight Button Clicked!");
        // TODO: Notify Game Manager
    }

    // Public methods to update UI from other systems
    public void UpdateTimer(float timeRemaining)
    {
        if (_timerLabel != null)
            _timerLabel.text = Mathf.CeilToInt(timeRemaining).ToString();
    }

    public void UpdateGold(int amount)
    {
        if (_goldLabel != null)
            _goldLabel.text = amount.ToString();
    }

    public void UpdatePhase(string phaseName)
    {
        if (_phaseLabel != null)
            _phaseLabel.text = phaseName.ToUpper();
    }

    public void UpdateRound(int round)
    {
        if (_roundLabel != null)
            _roundLabel.text = $"Round {round}";
    }
}
