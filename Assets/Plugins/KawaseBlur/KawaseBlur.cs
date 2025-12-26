using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2, 15)]
        public int blurPasses = 5;

        [Range(1, 4)]
        public int downsample = 2;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();
    CustomRenderPass scriptablePass;

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blurMaterial;
        public int passes;
        public int downsample;
        public bool copyToFramebuffer;
        public string targetName;
        string profilerTag;

        int tmpId1;
        int tmpId2;

        RTHandle source; 

        public CustomRenderPass(string profilerTag)
        {
            this.profilerTag = profilerTag;
            tmpId1 = Shader.PropertyToID("tmpBlurRT1");
            tmpId2 = Shader.PropertyToID("tmpBlurRT2");
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Initial attempt to get the camera target
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 1. Safety Check: Material
            if (blurMaterial == null) return;

            // 2. CRITICAL FIX: Ensure source is valid before Blit
            // If OnCameraSetup failed or wasn't called yet, try to fetch the handle again
            if (source == null)
            {
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            // If it is still null, we must return to avoid the "RTHandle:op_Implicit" error
            if (source == null) return;
            
            // 3. Safety Check: Skip Preview cameras (prevents scene view errors)
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            
            // 4. FIX: Safer downsampling (prevents 0 width/height errors)
            opaqueDesc.width = Mathf.Max(1, opaqueDesc.width / downsample);
            opaqueDesc.height = Mathf.Max(1, opaqueDesc.height / downsample);

            // Get Temporary RTs
            cmd.GetTemporaryRT(tmpId1, opaqueDesc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(tmpId2, opaqueDesc, FilterMode.Bilinear);

            // First pass: Copy Screen (Source) -> Tmp1
            cmd.SetGlobalFloat("_offset", 1.5f);
            cmd.Blit(source, tmpId1, blurMaterial);

            // Loop passes
            for (var i = 1; i < passes - 1; i++)
            {
                cmd.SetGlobalFloat("_offset", 0.5f + i);
                
                // Ping Pong between 1 and 2
                if(i % 2 == 1)
                    cmd.Blit(tmpId1, tmpId2, blurMaterial);
                else
                    cmd.Blit(tmpId2, tmpId1, blurMaterial);
            }

            // Final Pass
            cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);

            if (copyToFramebuffer)
            {
                // Blit back to screen
                if (passes % 2 == 1) // If we ended on Tmp1
                    cmd.Blit(tmpId1, source, blurMaterial);
                else // If we ended on Tmp2
                    cmd.Blit(tmpId2, source, blurMaterial);
            }
            else
            {
                // Set global texture for other shaders to use
                if (passes % 2 == 1)
                    cmd.SetGlobalTexture(targetName, tmpId1);
                else
                    cmd.SetGlobalTexture(targetName, tmpId2);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tmpId1);
            cmd.ReleaseTemporaryRT(tmpId2);
        }
    }

    public override void Create()
    {
        scriptablePass = new CustomRenderPass("KawaseBlur");
        scriptablePass.blurMaterial = settings.blurMaterial;
        scriptablePass.passes = settings.blurPasses;
        scriptablePass.downsample = settings.downsample;
        scriptablePass.targetName = settings.targetName;
        scriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blurMaterial == null)
        {
            Debug.LogWarning("KawaseBlur: Missing Material");
            return;
        }

        renderer.EnqueuePass(scriptablePass);
    }
}