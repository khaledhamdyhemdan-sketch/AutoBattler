using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace AutoBattler.Rendering
{
    /// <summary>
    /// Unity 6.3 URP Post-Processing feature using Render Graph API
    /// Applies team-based color grading and combat effects
    /// </summary>
    public class BattlePostProcessingFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Header("Combat Effects")]
            public bool enableCombatVignette = true;
            public float vignetteIntensity = 0.3f;
            
            [Header("Color Grading")]
            public bool enableTeamColorGrading = true;
            public float colorGradingStrength = 0.2f;

            [Header("Render Event")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public Settings settings = new Settings();
        private BattlePostProcessingPass postProcessPass;

        public override void Create()
        {
            postProcessPass = new BattlePostProcessingPass(settings);
            postProcessPass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(postProcessPass);
        }

        protected override void Dispose(bool disposing)
        {
            postProcessPass?.Dispose();
        }

        class BattlePostProcessingPass : ScriptableRenderPass
        {
            private Settings settings;
            private Material postProcessMaterial;
            private new ProfilingSampler profilingSampler;

            // Shader property IDs
            private static readonly int VignetteIntensityID = Shader.PropertyToID("_VignetteIntensity");
            private static readonly int ColorGradingStrengthID = Shader.PropertyToID("_ColorGradingStrength");

            public BattlePostProcessingPass(Settings settings)
            {
                this.settings = settings;
                profilingSampler = new ProfilingSampler("Battle Post-Processing");

                // Create post-processing material
                Shader shader = Shader.Find("Hidden/AutoBattler/BattlePostProcess");
                if (shader != null)
                {
                    postProcessMaterial = new Material(shader);
                }
            }

            private class PassData
            {
                public TextureHandle source;
                public TextureHandle destination;
                public Material material;
                public float vignetteIntensity;
                public float colorGradingStrength;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                if (postProcessMaterial == null) return;

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Battle Post-Process", out var passData, profilingSampler))
                {
                    // Get resources
                    UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                    UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();

                    // Source texture (active color)
                    passData.source = resourceData.activeColorTexture;

                    // Create temporary destination texture
                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    desc.depthBufferBits = 0;
                    passData.destination = UniversalRenderer.CreateRenderGraphTexture(
                        renderGraph, 
                        desc, 
                        "PostProcessTemp", 
                        false
                    );

                    // Pass data
                    passData.material = postProcessMaterial;
                    passData.vignetteIntensity = settings.vignetteIntensity;
                    passData.colorGradingStrength = settings.colorGradingStrength;

                    // Use textures
                    builder.UseTexture(passData.source, AccessFlags.Read);
                    builder.UseTexture(passData.destination, AccessFlags.Write);
                    builder.SetRenderAttachment(passData.destination, 0);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }
            }

            static void ExecutePass(PassData data, RasterGraphContext context)
            {
                if (data.material == null) return;

                // Set material properties
                data.material.SetFloat(VignetteIntensityID, data.vignetteIntensity);
                data.material.SetFloat(ColorGradingStrengthID, data.colorGradingStrength);

                // Blit with post-processing
                Blitter.BlitTexture(
                    context.cmd, 
                    data.source, 
                    new Vector4(1, 1, 0, 0), 
                    data.material, 
                    0
                );
            }

            public void Dispose()
            {
                if (postProcessMaterial != null)
                {
                    CoreUtils.Destroy(postProcessMaterial);
                }
            }
        }
    }
}
