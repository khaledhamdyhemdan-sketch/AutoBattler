#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesUXVerification
    {
        [MenuItem("Tools/Chronicles/Test Portrait Layout And Onboarding")]
        public static void Run()
        {
            Check(ChroniclesTutorial.Prompts.Length == ChroniclesTutorial.StepCount,
                "Onboarding step copy is missing.");
            Rect tall = ChroniclesViewport.ContentRect(new Rect(0, 0, 1080, 2400),
                1080, 2400);
            Check(Mathf.Abs(tall.x) < 1f && Mathf.Abs(tall.y - 240f) < 1f &&
                Mathf.Abs(tall.width - 1080f) < 1f &&
                Mathf.Abs(tall.height - 1920f) < 1f,
                "Extra-tall portrait devices must preserve the 9:16 canvas centered.");
            Rect notched = ChroniclesViewport.ContentRect(
                new Rect(0, 110, 1080, 2160), 1080, 2400);
            Check(notched.xMin >= 0 && notched.yMin >= 0 &&
                notched.xMax <= 1080f && notched.yMax <= 2400f,
                "Safe area must contain all portrait controls.");
            Rect tablet = ChroniclesViewport.ContentRect(
                new Rect(0, 0, 1600, 1200), 1600, 1200);
            Check(Mathf.Abs(tablet.height - 1200f) < 1f &&
                tablet.xMin > 0f && tablet.xMax <= 1600f,
                "Landscape/tablet safety must not crop the portrait UI.");
            Rect invalid = ChroniclesViewport.ContentRect(Rect.zero, 360, 640);
            Check(invalid.width > 0 && invalid.height > 0,
                "Invalid OS safe areas must fall back to the full screen.");

            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_onboarding_" + Guid.NewGuid().ToString("N"));
            try
            {
                var progress = new PlayerProgressService(folder);
                Check(progress.Data.version >= 2 &&
                    !progress.Data.tutorialCompleted &&
                    progress.Data.tutorialStep == 0,
                    "New players must start at guide step zero.");
                for (int i = 0; i < ChroniclesTutorial.StepCount; i++)
                    Check(progress.AdvanceTutorial() &&
                        progress.Data.tutorialStep == i + 1,
                        "Tutorial step advancement failed.");
                Check(progress.Data.tutorialCompleted &&
                    !progress.AdvanceTutorial(),
                    "Completed tutorial must be a no-op.");
                var reloaded = new PlayerProgressService(folder);
                Check(reloaded.Data.tutorialCompleted &&
                    reloaded.Data.tutorialStep == ChroniclesTutorial.StepCount,
                    "Tutorial completion must persist.");
                Check(reloaded.RestartTutorial() &&
                    reloaded.Data.tutorialStep == 0 &&
                    !reloaded.Data.tutorialCompleted,
                    "Tutorial must be replayable in Settings.");
                Check(reloaded.SkipTutorial() &&
                    reloaded.Data.tutorialCompleted,
                    "Skipping onboarding must persist an explicit completed state.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception e) { Debug.LogWarning(e.Message); }
            }
            Debug.Log("[Chronicles] UI safe-area and five-step tutorial source tests passed.");
        }

        private static void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException(description);
        }
    }
}
#endif
