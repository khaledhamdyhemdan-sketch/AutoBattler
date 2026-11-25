using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.EditorTools
{
    /// <summary>
    /// Batchmode battle screenshot rig: renders the first Broken Nile battle
    /// (stage 1, starter squad) at several timestamps into PNG files under
    /// vfx-captures/, so the real battle composition - arena, backdrop, hero
    /// rigs - can be reviewed from CI artifacts without a device build. Run:
    ///   Unity -batchmode -nographics -executeMethod MythicChronicles.EditorTools.ChroniclesBattleScreenshotRig.Capture -quit
    /// </summary>
    public static class ChroniclesBattleScreenshotRig
    {
        public static void Capture()
        {
            int shots = CaptureFrames();
            EditorApplication.Exit(shots >= 5 ? 0 : 1);
        }

        // Writes battle_stage1_*.png frames (arena view) plus
        // battle_panel_stage1_*.png frames (panel view, the default battle
        // presentation) and returns how many were captured. Does not exit the
        // editor, so sibling rigs can run in the same session.
        public static int CaptureFrames()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "vfx-captures");
            Directory.CreateDirectory(outDir);
            int shots = CaptureFrames("battle_stage1_", false, outDir);
            shots += CaptureFrames("battle_panel_stage1_", true, outDir);
            return shots;
        }

        private static int CaptureFrames(string prefix, bool panel, string outDir)
        {
            StageDefinition stage = WorldsCatalog.GetStage(1);
            var squad = new List<OwnedHero>();
            foreach (string id in WorldsCatalog.StartingHeroes)
                squad.Add(new OwnedHero { id = id, level = 3, stars = 1 });

            var battleGo = new GameObject("battle capture");
            ChroniclesBattle battle = battleGo.AddComponent<ChroniclesBattle>();
            battle.PanelLayout = panel;
            battle.Initialize(stage, squad, new PlayerSave(), won => { });

            Camera cam = null;
            foreach (Camera candidate in Object.FindObjectsByType<Camera>(
                FindObjectsSortMode.None))
            {
                if (cam == null) cam = candidate;
                if (candidate.gameObject.name == "Chronicles Portrait Camera")
                    cam = candidate;
            }
            if (cam == null)
            {
                Debug.LogError("BattleScreenshotRig: no battle camera found");
                Object.DestroyImmediate(battleGo);
                return 0;
            }

            // Game time advances at the Update clamp: .05s per Tick at Speed 1.
            const float step = .12f;
            float[] captureAt = { .5f, 3f, 6f, 9f, 12f };
            RenderTexture rt = new RenderTexture(720, 960, 24);
            float t = 0f;
            int shot = 0;
            int guard = 0;
            while (shot < captureAt.Length && guard < 2000)
            {
                battle.Tick(step, step);
                t += .05f;
                guard++;
                if (t + 1e-4f < captureAt[shot]) continue;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                png.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                File.WriteAllBytes(Path.Combine(outDir,
                    prefix + "t" + captureAt[shot].ToString("0.0") + "s.png"),
                    png.EncodeToPNG());
                Object.DestroyImmediate(png);
                shot++;
            }
            rt.Release();
            Object.DestroyImmediate(battleGo);
            Debug.Log("BattleScreenshotRig: wrote " + shot + " frames to " + outDir);
            return shot;
        }
    }
}
