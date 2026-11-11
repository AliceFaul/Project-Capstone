using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Selection outline for the objects listed in OutlineRegistry.
/// Pass 1 "Outline Mask":      draws the outlined renderers into a small mask texture
///                             (hidden part without depth test, visible part with a depth test).
/// Pass 2 "Outline Composite": fullscreen pass that draws a ring of N pixels around the mask
///                             onto the camera color.
/// 
/// Nothing is enqueued when the registry is empty, so it costs nothing while nothing is hovered.
/// Only game cameras that render to the screen are affected (the minimap camera and the Scene view are skipped).
/// </summary>
public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Tooltip("Outline width in pixels at 1080p screen height. Scales with the resolution.")]
        [Range(0.5f, 8f)] public float widthPixels = 2f;
        
        [Tooltip("Tolerance of the visibility test. Raise it if the outline flickers on the objects itself.")]
        [Range(0f, 0.01f)] public float depthBias = 0.0005f;
        
        [Tooltip("Draw the raw mask over the screen (for debugging).")]
        public bool debugMask;
        
        [Tooltip("Drag OutlineMask.shader here (falls back to Shader.Find).")]
        public Shader maskShader;
        
        [Tooltip("Drag OutlineComposite.shader here (falls back to Shader.Find).")]
        public Shader compositeShader;
    }
    
    public Settings settings = new Settings();

    private Material maskMaterial;
    private Material compositeMaterial;
    private OutlinePass pass;
    
    public override void Create()
    {
        if(settings.maskShader == null) settings.maskShader = Shader.Find("Hidden/Mhieu/OutlineMask");
        if(settings.compositeShader == null) settings.compositeShader = Shader.Find("Hidden/Mhieu/OutlineComposite");
        
        CoreUtils.Destroy(maskMaterial);
        CoreUtils.Destroy(compositeMaterial);
        maskMaterial = settings.maskShader != null ? CoreUtils.CreateEngineMaterial(settings.maskShader) : null;
        compositeMaterial = settings.compositeShader != null ? CoreUtils.CreateEngineMaterial(settings.compositeShader) : null;

        pass = new OutlinePass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(pass == null || maskMaterial == null || compositeMaterial == null) return;
        
        // free while nothing is hovered
        if(OutlineRegistry.Targets.Count == 0) return;
        
        var cameraData = renderingData.cameraData;
        if(cameraData.camera.cameraType != CameraType.Game) return;
        if(cameraData.camera.targetTexture != null) return;
        
        pass.Setup(maskMaterial, compositeMaterial, settings);
        pass.ConfigureInput(ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(maskMaterial);
        CoreUtils.Destroy(compositeMaterial);
        maskMaterial = null;
        compositeMaterial = null;
        pass = null;
    }

    private class OutlinePass : ScriptableRenderPass
    {
        private struct DrawItem
        {
            public Renderer renderer;
            public Vector3 color;
            public float intensity;
            public float hiddenOpacity;
            public int materialCount;
        }

        private class MaskPassData
        {
            public Material material;
            public List<DrawItem> items;
            public float depthBias;
        }

        private class CompositePassData
        {
            public Material material;
            public TextureHandle mask;
            public float widthPixels;
            public int shaderPass;
        }

        private static readonly int ColorIntensityId = Shader.PropertyToID("_OutlineColorIntensity");
        private static readonly int HiddenOpacityId = Shader.PropertyToID("_OutlineHiddenOpacity");
        private static readonly int DepthBiasId = Shader.PropertyToID("_OutlineDepthBias");
        private static readonly int WidthPxId = Shader.PropertyToID("_OutlineWidthPx");

        private static readonly List<Material> TempMaterials = new();
        private readonly List<DrawItem> items = new();
        
        private Material maskMaterial;
        private Material compositeMaterial;
        private Settings settings;
        private bool warnedNoDepth;

        public void Setup(Material mask, Material composite, Settings s)
        {
            maskMaterial = mask;
            compositeMaterial = composite;
            settings = s;
            renderPassEvent = s.renderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            
            CollectItems();
            if(items.Count == 0) return;

            if (!resourceData.cameraDepthTexture.IsValid())
            {
                if(warnedNoDepth) return;
                Debug.LogWarning($"[OutlineRendererFeature] Camera depth texture is not available, outline skipped.");
                warnedNoDepth = true;
            }
            
            // mask texture (same size as the camera target, no MSAA)
            var camDesc = cameraData.cameraTargetDescriptor;
            var maskDesc = new TextureDesc(camDesc.width, camDesc.height)
            {
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                depthBufferBits = DepthBits.None,
                msaaSamples = MSAASamples.None,
                clearBuffer = true,
                clearColor = Color.clear,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "_OutlineMask"
            };
            TextureHandle mask = renderGraph.CreateTexture(maskDesc);
            
            // pass 1: mask
            using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Outline Mask", out var passData))
            {
                passData.material = maskMaterial;
                passData.items = items;
                passData.depthBias = settings.depthBias;

                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((MaskPassData data, RasterGraphContext ctx) => ExecuteMask(data, ctx));
            }

            // pass 2: composite onto the camera color
            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Outline Composite", out var passData))
            {
                passData.material = compositeMaterial;
                passData.mask = mask;
                passData.widthPixels = settings.widthPixels * (camDesc.height / 1080f);
                passData.shaderPass = settings.debugMask ? 1 : 0;
                
                builder.UseTexture(mask, AccessFlags.Read);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                
                builder.SetRenderFunc((CompositePassData data, RasterGraphContext ctx) => ExecuteComposite(data, ctx));
            }
        }

        private void CollectItems()
        {
            items.Clear();

            var targets = OutlineRegistry.Targets;
            var linear = QualitySettings.activeColorSpace == ColorSpace.Linear;

            for (int i = 0; i < targets.Count; i++)
            {
                OutlineTarget target = targets[i];
                if(target == null || target.Style == null || target.Intensity <= 0f) continue;

                Color c = linear ? target.Style.color.linear : target.Style.color;
                float hidden = target.Style.HiddenOpacity;
                
                var renderers = target.Renderers;
                for (int r = 0; r < renderers.Count; r++)
                {
                    Renderer renderer = renderers[r];
                    if(renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                    
                    renderer.GetSharedMaterials(TempMaterials);
                    
                    items.Add(new DrawItem
                    {
                        renderer = renderer,
                        color = new Vector3(c.r, c.g, c.b),
                        intensity = target.Intensity,
                        hiddenOpacity = hidden,
                        materialCount = Mathf.Max(1, TempMaterials.Count),
                    });
                }
            }
        }

        private static void ExecuteMask(MaskPassData data, RasterGraphContext ctx)
        {
            RasterCommandBuffer cmd = ctx.cmd;
            cmd.SetGlobalFloat(DepthBiasId, data.depthBias);
            
            List<DrawItem> list = data.items;
            for (int i = 0; i < list.Count; i++)
            {
                DrawItem it = list[i];
                cmd.SetGlobalVector(ColorIntensityId, new Vector4(it.color.x, it.color.y, it.color.z, it.intensity));
                
                // pass 0: hidden part (whole silhouette, no depth test)
                if (it.hiddenOpacity > 0f)
                {
                    cmd.SetGlobalFloat(HiddenOpacityId, it.hiddenOpacity);
                    for (int s = 0; s < it.materialCount; s++)
                        cmd.DrawRenderer(it.renderer, data.material, s, 0);
                }
                
                // pass 1: visible part (depth tested)
                for(int s = 0; s < it.materialCount; s++)
                    cmd.DrawRenderer(it.renderer, data.material, s, 1);
            }
        }

        private static void ExecuteComposite(CompositePassData data, RasterGraphContext ctx)
        {
            data.material.SetFloat(WidthPxId, data.widthPixels);
            Blitter.BlitTexture(ctx.cmd, data.mask, new Vector4(1, 1, 0, 0), data.material, data.shaderPass);
        }
    }
}