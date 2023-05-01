using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using UnityEngine.Experimental.Rendering;

public class ScannerHDRP : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField]
    private ReplacementPass m_ReplacementPass;
    [SerializeField]
    private Volume m_GlobalVolume;
    [SerializeField]
    private VisualEffect m_HolographVFX;
    private TopographicScanHDRP m_Scan;
    [Header("Effect")]
    public float delay = 0.5f;
    [Tooltip("The scan's total distance")]
    public float distance = 100f;
    [Tooltip("Angle (in degree) of the scan field")]
    public float arcAngle = 135f;
    [SerializeField, Tooltip("Control the scan's distance over time. x-axis: normalized time, y-axis: normalized distance")]
    private AnimationCurve distanceCurve = new AnimationCurve(
       new Keyframe(0, 0),
       new Keyframe(1, 1)
   );
    [SerializeField, Tooltip("Control the scan's emissive intensity over time. x-axis: normalized time, y-axis: normalized distance")]
    private AnimationCurve emissiveIntensityCurve = new AnimationCurve(
        new Keyframe(0, 1),
        new Keyframe(1, 1)
    );
    private VolumeProfile m_Profile;
    [Min(0.1f)]
    public float duration = 5.0f;
    [Min(0.1f)]
    public float fadeDuration = 5.0f;
    private Coroutine m_ScanCoroutine;
    [Header("SFX")]
    public AudioSource sfx;
    public float sfxDuration = 30f;
    private bool m_IsScanning = false;

    static class ShaderIDs
    {
        internal static readonly int StartEvent = Shader.PropertyToID("Start");
        internal static readonly int StopEvent = Shader.PropertyToID("Stop");
        internal static readonly int ScanRadius = Shader.PropertyToID("Scan Radius");
        internal static readonly int CurrentDistance = Shader.PropertyToID("Current Distance");
        internal static readonly int DistanceCurve = Shader.PropertyToID("Distance Curve");
        internal static readonly int DepthTexture = Shader.PropertyToID("Depth Texture");
        internal static readonly int NormalTexture = Shader.PropertyToID("Normal Texture");
        internal static readonly int IdTexture = Shader.PropertyToID("Special ID Texture");
        internal static readonly int SpecialDepthTexture = Shader.PropertyToID("Special Depth Texture");
        internal static readonly int CameraHeight = Shader.PropertyToID("Camera Height");
        internal static readonly int CameraNearPlane = Shader.PropertyToID("Camera Near Plane");
        internal static readonly int CameraFarPlane = Shader.PropertyToID("Camera Far Plane");
    }

    private void OnValidate()
    {
        if (distanceCurve.keys.Length < 1)
        {
            distanceCurve.AddKey(0f, 1f);
        }
        if (emissiveIntensityCurve.keys.Length < 1)
        {
            emissiveIntensityCurve.AddKey(0f, 1f);
        }
    }

    private void Start()
    {
        m_Profile = m_GlobalVolume.profile;
        m_Profile.TryGet(out m_Scan);

        m_Scan.active = false;
        if (m_HolographVFX)
        {
            m_HolographVFX.Stop();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Scan();
        }
    }

    public void ResetScanner()
    {
        m_Scan.distance.Override(0f);
    }

    private void OnDestroy()
    {
        Destroy(m_Profile);
    }

    public void Scan()
    {
        m_IsScanning = true;
        PlaySFX();
        StartCoroutine(ScanCoroutine());
    }

    private IEnumerator ScanCoroutine()
    {
        float coroutineTime = 0f;
        while (coroutineTime < delay)
        {
            coroutineTime += Time.deltaTime;
            yield return null;
        }
        m_ReplacementPass.renderCamera.enabled = true;
        m_ReplacementPass.onRenderEnd.AddListener(PlayVFX);
    }

    private void PlayVFX()
    {
        // replacement pass no longer needed. Disable it.
        m_ReplacementPass.onRenderEnd.RemoveListener(PlayVFX);
        m_ReplacementPass.renderCamera.enabled = false;


        if (m_HolographVFX)
        {
            m_HolographVFX.Reinit(); // Reset VFX time
            m_HolographVFX.Stop();

            m_HolographVFX.SetFloat(ShaderIDs.CameraNearPlane, m_ReplacementPass.renderCamera.nearClipPlane);
            m_HolographVFX.SetFloat(ShaderIDs.CameraFarPlane, m_ReplacementPass.renderCamera.farClipPlane);
            m_HolographVFX.SetFloat(ShaderIDs.CameraHeight,
                (m_ReplacementPass.renderCamera.transform.position - transform.position).y);
            m_HolographVFX.SetAnimationCurve(ShaderIDs.DistanceCurve, distanceCurve);

            m_HolographVFX.Play();

            // Start the effect and cleanup temporary data
            m_HolographVFX.SendEvent(ShaderIDs.StartEvent);

            if (m_ScanCoroutine != null)
            {
                StopCoroutine(m_ScanCoroutine);
            }
            m_ScanCoroutine = StartCoroutine(VFXCoroutine());
        }

        m_Scan.active = true;
    }

    private void PlaySFX()
    {
        if (sfx)
        {
            if (sfx.enabled)
            {
                sfx.enabled = false;
            }
            sfx.enabled = true;
        }
    }

    private IEnumerator VFXCoroutine()
    {
        // Capture terrain depth and normal
        m_Scan.active = true;
        float totalDuration = duration + fadeDuration;
        float oldR = m_Scan.color.value.r;
        float oldG = m_Scan.color.value.g;
        float oldB = m_Scan.color.value.b;
        float oldIntensity = m_Scan.intensity.value;

        m_Scan.origin1.Override(transform.position);
        m_Scan.forward.Override(new Vector2(transform.forward.x, transform.forward.z));
        m_Scan.distance.overrideState = true;

        // Scan
        m_Scan.color.overrideState = true;
        m_Scan.intensity.overrideState = true;
        float time = 0;
        float progress = 0;
        Color scanColor = m_Scan.color.value;
        while (time <= duration)
        {
            progress = time / duration;
            // Interpolate distance
            m_Scan.distance.value = distanceCurve.Evaluate(progress) * distance;
            m_HolographVFX.SetFloat(ShaderIDs.CurrentDistance, m_Scan.distance.value);

            // Interpolate emissive intensity
            float intensity = emissiveIntensityCurve.Evaluate(progress);
            scanColor.r = Mathf.Lerp(oldR, intensity * oldR, progress);
            scanColor.g = Mathf.Lerp(oldG, intensity * oldG, progress);
            scanColor.b = Mathf.Lerp(oldB, intensity * oldB, progress);
            m_Scan.color.value = scanColor;
            time += Time.deltaTime;
            yield return null;
        }

        // Fade out
        while (time <= totalDuration)
        {
            m_Scan.intensity.value = Mathf.Lerp(oldIntensity, 0f, (time - duration) / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }

        // End scan
        scanColor.r = oldR;
        scanColor.g = oldG;
        scanColor.b = oldB;
        m_Scan.intensity.value = oldIntensity;
        m_Scan.color.value = scanColor;
        m_Scan.active = false;
        m_ScanCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, distance);
    }
}