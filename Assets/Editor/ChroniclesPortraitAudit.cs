#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    /// <summary>
    /// Editor-side checklist for imported, original English-language portrait assets.
    /// Running this audit does not generate new AI art or claim release readiness.
    /// </summary>
    public static class ChroniclesPortraitAudit
    {
        private const string StaticRoot =
            "Assets/Resources/Chronicles/Portraits/Static/";
        private const string AnimationRoot =
            "Assets/Resources/Chronicles/Portraits/Animated/";

        [MenuItem("Tools/Chronicles/Audit Hero Portraits And English")]
        public static void Audit()
        {
            int missingIdentity = 0, missingStill = 0, missingAnimation = 0;
            int englishIssues = 0, completePairs = 0;
            var unique = new System.Collections.Generic.HashSet<string>(
                StringComparer.Ordinal);
            var report = new StringBuilder();
            report.AppendLine("Hero ID,Hero Name,Rarity,Static Art,Animation Frames,English Text");

            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
            {
                bool identity = ChroniclesHeroPortrait.HasIdentity(hero.id);
                if (!identity || !unique.Add(hero.id))
                {
                    missingIdentity++;
                    Debug.LogError("[Portrait Audit] Missing/duplicate identity: " + hero.id);
                }

                string stillPng = StaticRoot + hero.id + ".png";
                string stillJpg = StaticRoot + hero.id + ".jpg";
                bool still = AssetDatabase.LoadAssetAtPath<Texture2D>(stillPng) != null ||
                    AssetDatabase.LoadAssetAtPath<Texture2D>(stillJpg) != null;
                if (!still) missingStill++;

                string folder = AnimationRoot + hero.id;
                string[] frames = Directory.Exists(folder)
                    ? Directory.GetFiles(folder, "frame_*.png",
                        SearchOption.TopDirectoryOnly)
                    : new string[0];
                Array.Sort(frames, StringComparer.Ordinal);
                bool validAnimation = frames.Length == 8 || frames.Length == 12;
                if (frames.Length < 8)
                {
                    missingAnimation++;
                    if (frames.Length > 0)
                        Debug.LogWarning("[Portrait Audit] " + hero.id +
                            ": fewer than 8 animation frames.");
                }

                for (int i = 0; i < frames.Length; i++)
                {
                    string expected = "frame_" + i.ToString("D3") + ".png";
                    if (Path.GetFileName(frames[i]) != expected)
                    {
                        Debug.LogError("[Portrait Audit] " + hero.id +
                            ": use consecutive, zero-padded frame names starting at frame_000.png.");
                        missingAnimation++;
                        validAnimation = false;
                        break;
                    }
                }

                int width = 0, height = 0;
                foreach (string framePath in frames)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(framePath);
                    if (texture == null || (width > 0 &&
                        (texture.width != width || texture.height != height)))
                    {
                        validAnimation = false;
                        Debug.LogError("[Portrait Audit] Missing import or inconsistent frame size: " + framePath);
                        continue;
                    }
                    width = texture.width;
                    height = texture.height;
                }

                HeroKit kit = HeroAbilityCatalog.For(hero.id);
                bool english = !ContainsArabic(hero.name) &&
                    !ContainsArabic(hero.story) &&
                    (kit == null || (!ContainsArabic(kit.skillName) &&
                        !ContainsArabic(kit.ultimateName) &&
                        !ContainsArabic(kit.passiveName) &&
                        !ContainsArabic(kit.skillDescription) &&
                        !ContainsArabic(kit.ultimateDescription) &&
                        !ContainsArabic(kit.passiveDescription)));
                if (!english)
                {
                    englishIssues++;
                    Debug.LogError("[English Audit] Arabic character in user-facing hero text: " +
                        hero.id);
                }

                if (still && validAnimation && english) completePairs++;
                report.Append(EscapeCsv(hero.id)).Append(',')
                    .Append(EscapeCsv(hero.name)).Append(',')
                    .Append(EscapeCsv(hero.rarity.ToString())).Append(',')
                    .Append(still ? "YES" : "MISSING").Append(',')
                    .Append(frames.Length).Append(',')
                    .AppendLine(english ? "YES" : "NO");
            }

            if (WorldsCatalog.Heroes.Length != HeroRosterData.HeroCount)
            {
                missingIdentity++;
                Debug.LogError("[Portrait Audit] Catalog does not match the generated hero roster.");
            }

            // Verify English across story content, not just character cards.
            for (int stage = 1; stage <= 60; stage++)
            {
                StoryMoment moment = WorldsStory.Get(stage);
                StageDefinition stageDefinition = WorldsCatalog.GetStage(stage);
                if ((moment != null && (ContainsArabic(moment.speaker) ||
                    ContainsArabic(moment.line))) ||
                    (stageDefinition != null && (ContainsArabic(stageDefinition.name) ||
                        ContainsArabic(stageDefinition.narrative))))
                {
                    englishIssues++;
                    Debug.LogError("[English Audit] Non-English story text in stage " + stage);
                }
            }

            // Scan player-facing C# scripts for Arabic character glyphs.
            // Visual assets must be manually checked for any baked text.
            string sourceFolder = Path.Combine(Application.dataPath, "Chronicles");
            if (Directory.Exists(sourceFolder))
            {
                foreach (string file in Directory.GetFiles(sourceFolder, "*.cs",
                    SearchOption.AllDirectories))
                {
                    if (ContainsArabic(File.ReadAllText(file)))
                    {
                        englishIssues++;
                        Debug.LogError("[English Audit] Arabic glyph in game script: " +
                            Path.GetFileName(file));
                    }
                }
            }

            string destination = "Assets/Chronicles/PortraitArtAudit.csv";
            File.WriteAllText(destination, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Portrait Audit] " + completePairs + "/" + WorldsCatalog.Heroes.Length + " structurally valid imported pairs (visual approval separate). " +
                "Missing static=" + missingStill + ", animations=" + missingAnimation +
                ", identity issues=" + missingIdentity + ", English issues=" + englishIssues +
                ". Missing production assets use the distinct procedural fallback." +
                " CSV: " + destination);
        }

        private static string EscapeCsv(string s)
        {
            if (s == null) return "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char ch in text)
                if ((ch >= '\u0600' && ch <= '\u06FF') ||
                    (ch >= '\u0750' && ch <= '\u077F') ||
                    (ch >= '\u08A0' && ch <= '\u08FF'))
                    return true;
            return false;
        }
    }
}
#endif
