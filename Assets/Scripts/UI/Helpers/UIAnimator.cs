using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace AutoBattler
{
    /// <summary>
    /// Simple runtime UI animator that toggles USS classes (e.g., .pop, .pulse) for short durations.
    /// Avoids any hard dependency on a tweening library and works reliably in playmode.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIAnimator : MonoBehaviour
    {
        private static UIAnimator _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnLoad()
        {
            if (_instance != null) return;
            var go = new GameObject("_UIAnimator");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UIAnimator>();
        }

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else if (_instance != this) Destroy(gameObject);
        }

        public static void Pop(VisualElement element, float duration = 0.16f)
        {
            if (element == null || _instance == null) return;
            _instance.StartCoroutine(_instance.PopRoutine(element, duration));
        }

        public static void Pulse(VisualElement element, float duration = 0.35f)
        {
            if (element == null || _instance == null) return;
            _instance.StartCoroutine(_instance.PulseRoutine(element, duration));
        }

        public static void AddTemporaryClass(VisualElement element, string className, float duration)
        {
            if (element == null || _instance == null) return;
            _instance.StartCoroutine(_instance.TemporaryClassRoutine(element, className, duration));
        }

        private IEnumerator PopRoutine(VisualElement el, float duration)
        {
            el.AddToClassList("pop");
            yield return new WaitForSeconds(duration);
            el.RemoveFromClassList("pop");
        }

        private IEnumerator PulseRoutine(VisualElement el, float duration)
        {
            el.AddToClassList("pulse");
            yield return new WaitForSeconds(duration);
            el.RemoveFromClassList("pulse");
        }

        private IEnumerator TemporaryClassRoutine(VisualElement el, string className, float duration)
        {
            el.AddToClassList(className);
            yield return new WaitForSeconds(duration);
            el.RemoveFromClassList(className);
        }
    }
}