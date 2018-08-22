using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;


public class CustomLWPipe : MonoBehaviour, IRendererSetup
{
    private SetupForwardRenderingPass m_SetupForwardRenderingPass;
    private CreateLightweightRenderTexturesPass m_CreateLightweightRenderTexturesPass;
    private SetupLightweightConstanstPass m_SetupLightweightConstants;
    private RenderOpaqueForwardPass m_RenderOpaqueForwardPass;

    [NonSerialized]
    private bool m_Initialized = false;

    private void Init()
    {
        if (m_Initialized)
            return;

        m_SetupForwardRenderingPass = new SetupForwardRenderingPass();
        m_CreateLightweightRenderTexturesPass = new CreateLightweightRenderTexturesPass();
        m_SetupLightweightConstants = new SetupLightweightConstanstPass();
        m_RenderOpaqueForwardPass = new RenderOpaqueForwardPass();

        m_Initialized = true;
    }

    public void Setup(ScriptableRenderer renderer, ref ScriptableRenderContext context,
        ref CullResults cullResults, ref RenderingData renderingData)
    {
        Init();

        renderer.Clear();

        renderer.SetupPerObjectLightIndices(ref cullResults, ref renderingData.lightData);
        RenderTextureDescriptor baseDescriptor = renderer.CreateRTDesc(ref renderingData.cameraData);
        RenderTextureDescriptor shadowDescriptor = baseDescriptor;
        shadowDescriptor.dimension = TextureDimension.Tex2D;

        renderer.EnqueuePass(m_SetupForwardRenderingPass);

        RenderTargetHandle colorHandle = RenderTargetHandle.CameraTarget;
        RenderTargetHandle depthHandle = RenderTargetHandle.CameraTarget;
        
        var sampleCount = (SampleCount)renderingData.cameraData.msaaSamples;
        m_CreateLightweightRenderTexturesPass.Setup(baseDescriptor, colorHandle, depthHandle, sampleCount);
        renderer.EnqueuePass(m_CreateLightweightRenderTexturesPass);

        Camera camera = renderingData.cameraData.camera;
        RendererConfiguration rendererConfiguration = renderer.GetRendererConfiguration(renderingData.lightData.totalAdditionalLightsCount);

        m_SetupLightweightConstants.Setup(renderer.maxVisibleLocalLights, renderer.perObjectLightIndices);
        renderer.EnqueuePass(m_SetupLightweightConstants);

        m_RenderOpaqueForwardPass.Setup(baseDescriptor, colorHandle, depthHandle, renderer.GetCameraClearFlag(camera), camera.backgroundColor, rendererConfiguration);
        renderer.EnqueuePass(m_RenderOpaqueForwardPass);
    }
}
