using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Events;


[System.Serializable]
public class ReplacementRenderer
{
    public enum OutputTarget
    {
        Camera, // outputs to attached camera
        RenderTexture, // output to Render Texture on disc
        TemporaryRenderTexture // outputs to a temporary runtime Render Texture
    }

    public enum RenderQueueRange
    {
        All,
        Opaque,
        Transparent
    }

    public bool enabled
    {
        get => m_Enabled;
        set
        {
            if (value)
            {
                m_ReplacementPass.ActivateRender(this);
            }
            else
            {
                m_ReplacementPass.DeactivateRender(this);
            }
            m_Enabled = value;
        }
    }
    public string name => m_Name;
    [SerializeField]
    private bool m_Enabled = false; // TODO: detect change and add/remove renderer from pass
    [SerializeField]
    private string m_Name;
    [SerializeField]
    private Material m_ReplacementShaderMaterial;
    [SerializeField]
    private int m_ShaderPassIndex = 0;
    public OutputTarget output = OutputTarget.Camera;
    public RenderQueueRange renderQueueRange = RenderQueueRange.All;
    public LayerMask cullingMask = -1;
    [SerializeField]
    private RenderTexture m_RenderTexture;
    //public RenderTextureParameters tempRenderTextureParams = RenderTextureParameters.Default;
    public TemporaryRenderTexture tempRenderTexture;
    public bool setCameraAspectToTexture = true;
    /// <summary>
    /// Currently active target texture. This depends on the output type
    /// </summary>
    public RenderTexture targetTexture
    {
        get
        {
            if (output == OutputTarget.RenderTexture)
            {
                return m_RenderTexture;
            }
            else if (output == OutputTarget.TemporaryRenderTexture)
            {
                return tempRenderTexture.texture;
            }
            return null;
        }
    }
    [NonSerialized]
    internal ReplacementPass m_ReplacementPass;
    [NonSerialized]
    internal HDAdditionalCameraData m_CameraData;

    private ReplacementRenderer() { }
    public ReplacementRenderer(ReplacementPass replacementPass)
    {
        m_ReplacementPass = replacementPass;
        m_CameraData = replacementPass.cameraData;
    }


    /// <summary>
    /// Render to designated camera/texture using replacement shader
    /// </summary>
    /// <param name="context"></param>
    /// <param name="camera"></param>
    public void ReplacementRender(ScriptableRenderContext context, HDCamera camera)
    {
        if ((output == OutputTarget.RenderTexture && m_RenderTexture == null)
            || (output == OutputTarget.TemporaryRenderTexture && tempRenderTexture == null))
        {
            return;
        }

        float prevCameraAspect = camera.camera.aspect;

        CommandBuffer cmd = null;
        ConfigureOutput(ref cmd, camera);

        // Clear everything to black
        cmd.ClearRenderTarget(true, true, Color.clear);
        cmd.BeginSample("Replacement Render");
        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();

        context.SetupCameraProperties(camera.camera);

        camera.camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
        cullingParameters.cullingMask = (uint)cullingMask.value;

        CullingResults cullingResults = context.Cull(ref cullingParameters);

        DrawingSettings drawingSettings = new DrawingSettings();



        drawingSettings.SetShaderPassName(0, new ShaderTagId("DepthOnly")); // Allow the context to draw to depth

        if (m_ReplacementShaderMaterial)
        {
            drawingSettings.overrideMaterial = m_ReplacementShaderMaterial;
            drawingSettings.overrideMaterialPassIndex = m_ShaderPassIndex;
        }

        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        switch (renderQueueRange)
        {
            case RenderQueueRange.All:
                filteringSettings = new FilteringSettings(UnityEngine.Rendering.RenderQueueRange.all);
                break;
            case RenderQueueRange.Opaque:
                filteringSettings = new FilteringSettings(UnityEngine.Rendering.RenderQueueRange.opaque);
                break;
            case RenderQueueRange.Transparent:
                filteringSettings = new FilteringSettings(UnityEngine.Rendering.RenderQueueRange.transparent);
                break;
            default:
                break;
        }

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // Uncomment if you want skybox included
        //context.DrawSkybox(camera.camera);

        cmd.EndSample("Replacement Render");
        context.ExecuteCommandBuffer(cmd);

        cmd.Clear();
        context.Submit();

        //if (output == OutputTarget.TemporaryRenderTexture && tempRenderTexture)
        //{
        //    tempRenderTexture.texture = m_TempRenderTexture;
        //}

        m_ReplacementPass.MarkRenderComplete();

        camera.camera.targetTexture = null;
        camera.camera.aspect = prevCameraAspect;
    }

    private void ConfigureOutput(ref CommandBuffer cmd, HDCamera camera)
    {
        if (output == OutputTarget.RenderTexture)
        {
            if (m_RenderTexture == null) { return; }
            else
            {
                cmd = new CommandBuffer
                {
                    name = "Replacement Render to Texture"
                };

                if (setCameraAspectToTexture)
                {
                    camera.camera.aspect = (float)targetTexture.width / targetTexture.height;
                }
                cmd.SetRenderTarget(m_RenderTexture.colorBuffer, m_RenderTexture.depthBuffer);
                camera.camera.targetTexture = m_RenderTexture;
            }
        }
        else if (output == OutputTarget.TemporaryRenderTexture)
        {
            if (tempRenderTexture == null) { return; }
            cmd = new CommandBuffer
            {
                name = "Replacement Render to Temporary Render Texture"
            };
            if (setCameraAspectToTexture)
            {
                camera.camera.aspect = (float)tempRenderTexture.width / tempRenderTexture.height;
            }
            tempRenderTexture.Initialize();
            tempRenderTexture.texture.name = tempRenderTexture.name;

            cmd.SetRenderTarget(tempRenderTexture.texture.colorBuffer, tempRenderTexture.texture.depthBuffer);
            camera.camera.targetTexture = tempRenderTexture.texture;
        }
        else
        {
            cmd = new CommandBuffer
            {
                name = "Replacement Render to Camera"
            };
        }
        cmd.name = this.m_Name;
    }

    /// <summary>
    /// Release temporary render textures if there are any
    /// </summary>
    internal void ReleaseResources()
    {
        if (m_Enabled && output == OutputTarget.TemporaryRenderTexture)
        {
            tempRenderTexture.Release();
        }
    }
}
