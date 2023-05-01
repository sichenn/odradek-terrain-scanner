using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Temporary Render Texture", menuName = "Rendering/Temporary Render Texture", order = 1)]
public class TemporaryRenderTexture : ScriptableObject
{
    public enum DepthBuffer
    {
        None = 0,
        x16 = 16,
        x24 = 24
    }
    public enum AntiAliasing
    {
        None = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8
    }

    public int width = 256, height = 256;
    [SerializeField, Tooltip("Number of antialiasing samples to store in the texture.")]
    private AntiAliasing m_AntiAliasing = AntiAliasing.None;
    public RenderTextureFormat format = RenderTextureFormat.Default;
    [SerializeField, Tooltip("Depth buffer bits. Note that only 24 bit depth has stencil buffer.")]
    private DepthBuffer m_DepthBuffer = DepthBuffer.x16;
    [Tooltip("Color space conversion mode.")]
    public RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default;
    public bool useMipMap = false;
    public bool autoGenerateMips = true;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    public FilterMode filterMode = FilterMode.Bilinear;
    public event OnTextureRendererd onTextureRender;
    public int depthBuffer
    {
        get => (int)m_DepthBuffer;
        set
        {
            if (value != 0 && value != 16 && value != 24)
            {
                throw new System.ArgumentException("valid values for depth buffer are 0, 16, and 24");
            }
            m_DepthBuffer = (DepthBuffer)value;
        }
    }
    public int antiAliasing
    {
        get => (int)m_AntiAliasing;
        set
        {
            if (value != 1 && value != 2 && value != 4 && value != 8)
            {
                throw new System.ArgumentException("valid values for anti-aliasing are 1, 2, 4, and 8");
            }
            m_AntiAliasing = (AntiAliasing)value;
        }
    }
    public RenderTexture texture
    {
        get => m_Texture;
        set
        {
            m_Texture = texture;
            if (onTextureRender != null)
            {
                onTextureRender();
            }
        }
    }
    private RenderTexture m_Texture;

    /// <summary>
    /// Initialize the temporary render texture by getting it from Unity's temporary render texture pool
    /// Should be called after texture has been set
    /// When unneeded, make sure to release it
    /// </summary>
    public void Initialize()
    {
        if (m_Texture) { return; }
        m_Texture = RenderTexture.GetTemporary(
                 width, height, depthBuffer, format, readWrite, antiAliasing);
        m_Texture.wrapMode = wrapMode;
        m_Texture.filterMode = filterMode;
        m_Texture.useMipMap = useMipMap;
        m_Texture.autoGenerateMips = autoGenerateMips;
    }

    public void Release()
    {
        RenderTexture.ReleaseTemporary(m_Texture);
    }

    public delegate void OnTextureRendererd();
}
