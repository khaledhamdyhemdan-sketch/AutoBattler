using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace AutoBattler.UI
{
    /// <summary>
    /// Lightweight navigator that cycles focus among elements with the 'focusable' class.
    /// Use arrow keys or WASD or gamepad axes to move focus. This is simple but effective for menus.
    /// Add this script to a GameObject in scenes with UI (or to a manager object).
    /// </summary>
    [DisallowMultipleComponent]
    public class GamepadNavigator : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private List<VisualElement> focusables = new List<VisualElement>();
        private int currentIndex = -1;

        private void Start()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            RefreshFocusables();
            if (focusables.Count > 0)
            {
                currentIndex = 0;
                focusables[0].Focus();
            }
        }

        private void RefreshFocusables()
        {
            focusables.Clear();
            var root = uiDocument?.rootVisualElement;
            if (root == null) return;
            var list = root.Query().Class("focusable").Build();
            foreach (var el in list)
            {
                focusables.Add(el);
            }
        }

        private void Update()
        {
            if (focusables.Count == 0) return;

            bool right = (Keyboard.current != null && (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)) ||
                         (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame);

            bool left = (Keyboard.current != null && (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)) ||
                        (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame);

            bool submit = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
                          (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

            bool tab = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;

            if (right)
            {
                MoveNext();
            }
            else if (left)
            {
                MovePrev();
            }
            else if (submit)
            {
                var el = focusables[currentIndex];
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = el;
                    el?.SendEvent(clickEvent);
                }
            }

            // Refresh focusables when pressing Tab (useful after UI updates)
            if (tab)
            {
                RefreshFocusables();
            }
        }

        private void MoveNext()
        {
            currentIndex = (currentIndex + 1) % focusables.Count;
            focusables[currentIndex].Focus();
        }

        private void MovePrev()
        {
            currentIndex = (currentIndex - 1 + focusables.Count) % focusables.Count;
            focusables[currentIndex].Focus();
        }
    }
}