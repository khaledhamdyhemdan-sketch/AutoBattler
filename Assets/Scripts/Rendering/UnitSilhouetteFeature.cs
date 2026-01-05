using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace AutoBattler.Rendering
{
    /// <summary>
    /// Unity 6.3 URP Renderer Feature for unit silhouettes using Render Graph API
    /// Provides team-based visual identification through outline rendering
    /// </summary>
    public class UnitSilhouetteFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material silhouetteMaterial;
            public Color playerTeamColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
            public Color enemyTeamColor = new Color(0.8f, 0.2f, 0.2f, 1f);  // Red
            public float outlineWidth = 0.03f;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public Settings settings = new Settings();
        private UnitSilhouettePass silhouettePass;

        public override void Create()
        {
            silhouettePass = new UnitSilhouettePass(settings);
            silhouettePass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.silhouetteMaterial == null)
            {
                Debug.LogWarning("[UnitSilhouetteFeature] Silhouette material not assigned!");
                return;
            }

            renderer.EnqueuePass(silhouettePass);
        }

        protected override void Dispose(bool disposing)
        {
            silhouettePass?.Dispose();
        }

        /// <summary>
        /// Render Graph-based pass for drawing unit silhouettes
        /// </summary>
        class UnitSilhouettePass : ScriptableRenderPass
        {
            private Settings settings;
            private new ProfilingSampler profilingSampler;

            public UnitSilhouettePass(Settings settings)
            {
                this.settings = settings;
                profilingSampler = new ProfilingSampler("Unit Silhouette Pass");
            }

            private class PassData
            {
                public RendererListHandle playerUnitsHandle;
                public RendererListHandle enemyUnitsHandle;
                public Material silhouetteMaterial;
                public Color playerColor;
                public Color enemyColor;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Unit Silhouettes", out var passData, profilingSampler))
                {
                    // Get rendering data
                    UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                    UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                    UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                    UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

                    // Setup drawing settings for units
                    ShaderTagId shaderTag = new ShaderTagId("UniversalForward");
                    DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(
                        shaderTag, 
                        renderingData, 
                        cameraData, 
                        lightData, 
                        SortingCriteria.CommonOpaque
                    );

                    // Override with silhouette material
                    drawSettings.overrideMaterial = settings.silhouetteMaterial;
                    drawSettings.overrideMaterialPassIndex = 0;

                    // Create filtering settings for player units (Layer 6)
                    FilteringSettings playerFilter = new FilteringSettings(RenderQueueRange.all, 1 << 6);
                    RendererListParams playerParams = new RendererListParams(renderingData.cullResults, drawSettings, playerFilter);
                    passData.playerUnitsHandle = renderGraph.CreateRendererList(playerParams);

                    // Create filtering settings for enemy units (Layer 7)
                    FilteringSettings enemyFilter = new FilteringSettings(RenderQueueRange.all, 1 << 7);
                    RendererListParams enemyParams = new RendererListParams(renderingData.cullResults, drawSettings, enemyFilter);
                    passData.enemyUnitsHandle = renderGraph.CreateRendererList(enemyParams);

                    // Pass settings
                    passData.silhouetteMaterial = settings.silhouetteMaterial;
                    passData.playerColor = settings.playerTeamColor;
                    passData.enemyColor = settings.enemyTeamColor;

                    // Configure render targets
                    builder.UseRendererList(passData.playerUnitsHandle);
                    builder.UseRendererList(passData.enemyUnitsHandle);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                    // Set render function
                    builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
                }
            }

            static void ExecutePass(PassData data, RasterGraphContext context)
            {
                // Set player team color and draw
                if (data.silhouetteMaterial != null)
                {
                    data.silhouetteMaterial.SetColor("_OutlineColor", data.playerColor);
                }
                context.cmd.DrawRendererList(data.playerUnitsHandle);

                // Set enemy team color and draw
                if (data.silhouetteMaterial != null)
                {
                    data.silhouetteMaterial.SetColor("_OutlineColor", data.enemyColor);
                }
                context.cmd.DrawRendererList(data.enemyUnitsHandle);
            }

            public void Dispose()
            {
                // Cleanup if needed
            }
        }
    }
}
