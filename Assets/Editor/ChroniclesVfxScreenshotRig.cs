using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.EditorTools
{
    /// <summary>
    /// Batchmode screenshot rig: renders the generic skill/ultimate effect for
    /// one hero of each rarity tier at several timestamps into PNG files under
    /// vfx-captures/, so effect quality can be reviewed from CI artifacts
    /// without a device build. Run:
    ///   Unity -batchmode -nographics -executeMethod MythicChronicles.EditorTools.ChroniclesVfxScreenshotRig.Capture -quit
    /// </summary>
    public static class ChroniclesVfxScreenshotRig
    {
        public static void Capture()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "vfx-captures");
            Directory.CreateDirectory(outDir);
            // Battle composition frames first; a failure there must not cost
            // the effect-tier captures below.
            try
            {
                ChroniclesBattleScreenshotRig.CaptureFrames();
            }
            catch (System.Exception battleError)
            {
                Debug.LogError("VfxScreenshotRig: battle capture failed: " +
                    battleError.Message);
            }
            Rarity[] tiers = { Rarity.R, Rarity.SR, Rarity.SSR, Rarity.UR };
            foreach (Rarity tier in tiers)
                foreach (bool ultimate in new[] { false, true })
                    CaptureOne(tier, ultimate, outDir);
            Debug.Log("VfxScreenshotRig: captures written to " + outDir);
            EditorApplication.Exit(0);
        }

        private static void CaptureOne(Rarity tier, bool ultimate, string outDir)
        {
            GameObject scene = new GameObject("vfx rig scene");
            Camera cam = new GameObject("rig camera").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.05f, .04f, .09f, 1f);
            cam.transform.position = new Vector3(0f, 2.2f, -6f);
            cam.transform.LookAt(new Vector3(0f, 1.2f, 0f));
            cam.fieldOfView = 50f;
            // Simple ground plate so effects read against a surface.
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(2f, 1f, 2f);

            GameObject root = new GameObject("test effect");
            HeroSkillEffects effect = root.AddComponent<HeroSkillEffects>();
            var init = typeof(HeroSkillEffects).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // HeroSkillEffects tiers are 1-4 (R=1, SR=2, SSR=3, UR/EX/SSRP=4).
            init.Invoke(effect, new object[] {
                HeroSkillEffects.ColorFor(HeroElement.Fire), ultimate, (int)tier + 1 });

            const float dt = .12f;
            const int frames = 5;
            var elapsedField = typeof(HeroSkillEffects).GetField("elapsed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            RenderTexture rt = new RenderTexture(720, 960, 24);
            for (int f = 0; f < frames; f++)
            {
                // Advance the effect's clock directly: batchmode deltaTime is
                // unreliable for deterministic captures.
                elapsedField.SetValue(effect, (float)elapsedField.GetValue(effect) + dt);
                effect.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                png.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                File.WriteAllBytes(Path.Combine(outDir,
                    tier + (ultimate ? "_ultimate" : "_skill") + "_f" + f + ".png"),
                    png.EncodeToPNG());
                Object.DestroyImmediate(png);
            }
            rt.Release();
            Object.DestroyImmediate(scene);
            Object.DestroyImmediate(cam.gameObject);
            Object.DestroyImmediate(ground);
        }
    }
}
