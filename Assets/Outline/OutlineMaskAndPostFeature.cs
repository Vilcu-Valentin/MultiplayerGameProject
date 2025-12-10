using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class OutlineMaskAndPostFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material maskOverrideMaterial;
        public Material outlineCompositeMaterial;
        public Color outlineColor = Color.yellow;
        [Range(0.1f, 10f)] public float edgeStrength = 1f;
        [Min(1f)] public float outlineWidth = 1f;
    }

    public Settings settings = new();

    class OutlinePass : ScriptableRenderPass
    {
        const string k_ProfilerTag = "OutlineMaskAndPost";
        private readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_ProfilerTag);

        private Material maskOverrideMaterial;
        private Material outlineCompositeMaterial;

        private RTHandle maskHandle;
        private Color outlineColor;
        private float edgeStrength;
        private float outlineWidth;

        public OutlinePass(Material maskMat, Material outlineMat)
        {
            maskOverrideMaterial = maskMat;
            outlineCompositeMaterial = outlineMat;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public void Setup(Color outlineCol, float edgeStr, float width)
        {
            outlineColor = outlineCol;
            edgeStrength = edgeStr;
            outlineWidth = width;
        }

        private void EnsureMaskHandle(RenderTextureDescriptor baseDescriptor)
        {
            var desc = baseDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            if (maskHandle == null || maskHandle.rt.width != desc.width || maskHandle.rt.height != desc.height)
            {
                if (maskHandle != null)
                    RTHandles.Release(maskHandle);

                maskHandle = RTHandles.Alloc(desc.width, desc.height,
                    depthBufferBits: 0,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm,
                    dimension: TextureDimension.Tex2D,
                    useDynamicScale: false,
                    name: "_SelectionMaskTexture");

                if (maskHandle != null && maskHandle.rt != null)
                {
                    maskHandle.rt.wrapMode = TextureWrapMode.Clamp;
                    maskHandle.rt.filterMode = FilterMode.Bilinear; // or Point if you want a crisper mask
                }
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            if (SelectionOutlineManager.Instance == null)
                return;

            var selected = SelectionOutlineManager.Instance.GetSelectedRenderers();
            if (selected == null || selected.Count == 0)
                return;

            // Safe to get cameraColorTargetHandle here
            RTHandle cameraColorHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
            if (cameraColorHandle == null || cameraColorHandle.rt == null)
                return;

            // Ensure mask is sized properly
            EnsureMaskHandle(renderingData.cameraData.cameraTargetDescriptor);

            CommandBuffer cmd = CommandBufferPool.Get(k_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                // 1. Render selected renderers into mask (white on black)
                CoreUtils.SetRenderTarget(cmd, maskHandle, ClearFlag.All, Color.black);

                foreach (var rend in selected)
                {
                    if (rend == null || !rend.enabled)
                        continue;

                    int materialCount = rend.sharedMaterials != null ? rend.sharedMaterials.Length : 1;
                    for (int i = 0; i < materialCount; i++)
                    {
                        cmd.DrawRenderer(rend, maskOverrideMaterial, i);
                    }
                }

                // 2. Configure outline composite material
                outlineCompositeMaterial.SetTexture("_MaskTex", maskHandle);
                outlineCompositeMaterial.SetColor("_OutlineColor", outlineColor);
                outlineCompositeMaterial.SetFloat("_EdgeStrength", edgeStrength);
                outlineCompositeMaterial.SetFloat("_OutlineWidth", outlineWidth);
                Vector2 texel = new Vector2(1f / maskHandle.rt.width, 1f / maskHandle.rt.height);
                outlineCompositeMaterial.SetVector("_TexelSize", texel);

                // 3. Composite: blit camera color through outline shader back into camera color
                int tempID = Shader.PropertyToID("_TempColorForOutline");
                RenderTextureDescriptor camDesc = renderingData.cameraData.cameraTargetDescriptor;
                camDesc.depthBufferBits = 0;
                cmd.GetTemporaryRT(tempID, camDesc, FilterMode.Bilinear);

                cmd.Blit(cameraColorHandle, tempID);
                cmd.Blit(tempID, cameraColorHandle, outlineCompositeMaterial);
                cmd.ReleaseTemporaryRT(tempID); 
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Keep maskHandle alive across frames; don't release here.
        }
    }

    OutlinePass outlinePass;

    public override void Create()
    {
        if (settings.maskOverrideMaterial == null || settings.outlineCompositeMaterial == null)
        {
            Debug.LogError("OutlineMaskAndPostFeature: Materials must be assigned in inspector.");
            return;
        }

        outlinePass = new OutlinePass(settings.maskOverrideMaterial, settings.outlineCompositeMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (outlinePass == null)
            return;

        outlinePass.Setup(settings.outlineColor, settings.edgeStrength, settings.outlineWidth);
        renderer.EnqueuePass(outlinePass);
    }
}
