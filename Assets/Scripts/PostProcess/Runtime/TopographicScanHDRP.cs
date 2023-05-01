using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable, VolumeComponentMenu("Post-processing/TopographicScan")]
public class TopographicScanHDRP : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Header("Scan")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    [Tooltip("The distance of the scan")]
    public FloatParameter distance = new MinFloatParameter(0f, 0f);
    [Tooltip("The highlighted distance")]
    public FloatParameter width = new MinFloatParameter(10f, 0f);
    public FloatParameter arcAngle = new ClampedFloatParameter(135f, 0f, 360f);

    [Header("Edge Glow")]
    [Tooltip("Normalized value of the edge glow distance (over the total distance)")]
    public ClampedFloatParameter edgeGlowDistance = new ClampedFloatParameter(0.1f, 0f, 1f);
    [Tooltip("Smooth the transition at the side edges of the scan")]
    public ClampedFloatParameter sideEdgeGradient = new ClampedFloatParameter(0.1f, 0.01f, 1f);

    [Header("Dakren")]
    [Tooltip("Darken the area at the edge of the scan")]
    public ClampedFloatParameter darkenFactor = new ClampedFloatParameter(0.5f, 0f, 1f);
    [Tooltip("Distance where darken effect starts to fade in, in meters")]
    public FloatParameter darkenFadeInDistance = new MinFloatParameter(0f, 0f);

    [Header("Scan Line")]
    [Tooltip("Color of the scan lines")]
    public ColorParameter color = new ColorParameter(Color.white, true, true, true);
    [InspectorName("Texture")]
    public TextureParameter scanLineTexture = new TextureParameter(null);
    [Tooltip("Tiling of the detailed emissive channel")]
    public FloatParameter textureTiling = new FloatParameter(1f);
    [Tooltip("Intensity of the scan line at the farthest point")]
    public FloatParameter farthestLineIntensity = new FloatParameter(1f);
    [InspectorName("Near Fade Distance")]
    [Tooltip("Controls the fade distance of the scan lines fade from the near end of the scan")]
    public Vector2Parameter scanLineNearFadeRange = new Vector2Parameter(Vector2.right);
    [InspectorName("Far Fade Distance")]
    [Tooltip("Controls the fade distance of the scan lines from the far end of the scan")]
    public Vector2Parameter scanLineFarFadeRange = new Vector2Parameter(Vector2.up);
    [InspectorName("Depth Range"), Tooltip("Distance range where edge detection is visible, in meters")]
    public FloatRangeParameter sobelDepthRange = new FloatRangeParameter(new Vector2(5f, 10f), 0, 100f);

    [Header("Silhouette")]
    [Tooltip("Define the contrast of the silhouette")]
    public ClampedIntParameter silhouetteThickness = new ClampedIntParameter(1, 1, 4);
    [Tooltip("Control the brightness of the silhouette")]
    public MinFloatParameter silhouetteIntensity = new MinFloatParameter(1f, 0f);

    [Header("Positions")]
    [Tooltip("Control the scanner's origin position manually or via script")]
    public Vector3Parameter origin1 = new Vector3Parameter(Vector3.zero);
    [Tooltip("The instantaneous camera forward direction at the moment of the scan")]
    public Vector2Parameter forward = new Vector2Parameter(Vector2.up);

    Material m_Material;
    Color m_RGBIntensity;

    static class ShaderIDs
    {
        internal static readonly int Distance = Shader.PropertyToID("_Distance");
        internal static readonly int Width = Shader.PropertyToID("_Width");
        internal static readonly int Tint = Shader.PropertyToID("_Tint");
        internal static readonly int ArcAngle = Shader.PropertyToID("_ArcAngle");
        internal static readonly int SideEdgeGradient = Shader.PropertyToID("_SideEdgeGradient");
        internal static readonly int Forward = Shader.PropertyToID("_Forward");
        internal static readonly int EmissiveTex = Shader.PropertyToID("_EmissiveTex");
        internal static readonly int TextureTiling = Shader.PropertyToID("_TextureTiling");
        internal static readonly int OutlineAlphaCutoff = Shader.PropertyToID("_OutlineAlphaCutoff");
        internal static readonly int ScanLineFadeRange = Shader.PropertyToID("_EdgeDetectionNearFadeRange");
        internal static readonly int ScanLineFarFadeRange = Shader.PropertyToID("_ScanLineFarFadeRange");
        internal static readonly int FarthestLineIntensity = Shader.PropertyToID("_FarthestLineIntensity");
        internal static readonly int SobelDepthRange = Shader.PropertyToID("_SobelDepthRange");
        internal static readonly int EdgeGlowDistance = Shader.PropertyToID("_EdgeGlowDistance");
        internal static readonly int DarkenFactor = Shader.PropertyToID("_DarkenFactor");
        internal static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        internal static readonly int OutlineIntensity = Shader.PropertyToID("_OutlineIntensity");
        internal static readonly int ScannerPos = Shader.PropertyToID("_ScannerPos");
    }

    public bool IsActive()
    {
        return m_Material != null && intensity.value > 0f && distance.value > 0f && arcAngle.value > 0f;
    }

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforeTAA;

    public override void Setup()
    {
        m_Material = CoreUtils.CreateEngineMaterial("Hidden/TopographicScanHDRP");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        m_Material.SetTexture("_MainTex", source);

        // Scan
        m_Material.SetFloat(ShaderIDs.Distance, distance.value);
        m_Material.SetFloat(ShaderIDs.Width, width.value);
        // pre-apply the cosine to save dot product in shader
        m_Material.SetFloat(ShaderIDs.ArcAngle, Mathf.Cos(Mathf.Deg2Rad * arcAngle.value * 0.5f));
        m_Material.SetVector(ShaderIDs.Forward, forward.value.normalized);

        // Emission
        m_RGBIntensity.r = color.value.r;
        m_RGBIntensity.g = color.value.g;
        m_RGBIntensity.b = color.value.b;
        m_RGBIntensity.a = intensity.value;
        m_Material.SetColor(ShaderIDs.Tint, m_RGBIntensity);

        // Scan Line
        if (scanLineTexture.value != null)
        {
            m_Material.SetTexture(ShaderIDs.EmissiveTex, scanLineTexture.value);
        }
        m_Material.SetFloat(ShaderIDs.TextureTiling, textureTiling.value);
        m_Material.SetFloat(ShaderIDs.FarthestLineIntensity, farthestLineIntensity.value);
        m_Material.SetVector(ShaderIDs.ScanLineFadeRange, scanLineNearFadeRange.value);
        m_Material.SetVector(ShaderIDs.ScanLineFarFadeRange, scanLineFarFadeRange.value);

        // Darken
        float darkenFactorFadeIn = Mathf.Clamp01((distance.value - darkenFadeInDistance.value) / width.value);
        m_Material.SetFloat(ShaderIDs.DarkenFactor, darkenFactor.value * darkenFactorFadeIn);
        
        // Edge Glow
        m_Material.SetFloat(ShaderIDs.EdgeGlowDistance, edgeGlowDistance.value);
        m_Material.SetFloat(ShaderIDs.SideEdgeGradient, sideEdgeGradient.value);

        // Silhouette
        m_Material.SetVector(ShaderIDs.SobelDepthRange, sobelDepthRange.value);
        m_Material.SetInt(ShaderIDs.OutlineThickness, silhouetteThickness.value);
        m_Material.SetFloat(ShaderIDs.OutlineIntensity, silhouetteIntensity.value);

        m_Material.SetVector(ShaderIDs.ScannerPos, origin1.value);

        // Draw to screen
        HDUtils.DrawFullScreen(cmd, m_Material, destination, null, 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}