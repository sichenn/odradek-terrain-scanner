using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Note: only executes when Game View is active
/// </summary>
[RequireComponent(typeof(HDAdditionalCameraData))]
[ExecuteAlways]
public class ReplacementPass : MonoBehaviour
{
    public Camera renderCamera { get; private set; }
    [SerializeField]
    private List<ReplacementRenderer> renderers = new List<ReplacementRenderer>();

    public UnityEvent onRenderEnd;
    [SerializeField, HideInInspector]
    private HDAdditionalCameraData m_CameraData;
    public HDAdditionalCameraData cameraData => m_CameraData;
    private int m_NumCompletedRenders = 0;
    public ReplacementRenderer test;
    public int numRenderers => renderers.Count;
    private RenderTexture m_PrevTargetTexture;

    private void OnValidate()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].m_ReplacementPass = this;
            renderers[i].m_CameraData = m_CameraData;
        }
    }

    private void OnEnable()
    {
        if (!m_CameraData)
        {
            m_CameraData = GetComponent<HDAdditionalCameraData>();
        }
#if UNITY_EDITOR
        else
        {
            // update HDAdditionalCameraData reference if copied to another GameObject
            HDAdditionalCameraData cameraData = GetComponent<HDAdditionalCameraData>();
            if (m_CameraData != cameraData)
            {
                m_CameraData = cameraData;
            }
        }
#endif

        if (!renderCamera)
        {
            renderCamera = GetComponent<Camera>();
        }
#if UNITY_EDITOR
        else
        {
            // update Camera reference if copied to another GameObject
            Camera camera = GetComponent<Camera>();
            if (renderCamera != camera)
            {
                renderCamera = camera;
            }
        }
#endif

        //m_PrevTargetTexture = renderCamera.targetTexture;


        bool isAnyRendererEnabled = false;
        for (int i = 0; i < renderers.Count; i++)
        {
            if(renderers[i].enabled)
            {
                isAnyRendererEnabled = true;
                break;
            }
        }

        if(isAnyRendererEnabled && !m_CameraData.hasCustomRender)
        {
            // Editor just started up and camera data needs to register render events again
            for (int i = 0; i < renderers.Count; i++)
            {
                if(renderers[i].enabled)
                {
                    ForceActivateRender(renderers[i]);
                }
            }
        }
        for (int i = 0; i < renderers.Count; i++)
        {
            renderers[i].m_ReplacementPass = this;
            renderers[i].m_CameraData = m_CameraData;
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            DeactivateRender(renderers[i]);
        }
    }

    public void ActivateRender(ReplacementRenderer renderer)
    {
        if (!renderer.enabled)
        {
            m_CameraData.customRender += renderer.ReplacementRender;
        }
    }

    public void DeactivateRender(ReplacementRenderer renderer)
    {
        if (renderer.enabled)
        {
            m_CameraData.customRender -= renderer.ReplacementRender;
        }
    }

    public void AddRenderer(ReplacementRenderer renderer)
    {
        renderers.Add(renderer);
        // TODO: Maybe here we should detect whether the renderer is enabled, if so, activate the render
    }

    public void RemoveRenderer(ReplacementRenderer renderer)
    {
        int index = renderers.IndexOf(renderer);
        if(index != -1)
        {
            RemoveRenderer(index);
        }
    }

    public void RemoveRenderer(int index)
    {
        if(index < 0 || index >= renderers.Count)
        {
            throw new System.ArgumentOutOfRangeException(
                index.ToString(), "Removing renderer out of range");
        }
        
        DeactivateRender(renderers[index]);
        renderers.RemoveAt(index);
    }

    public ReplacementRenderer GetRenderer(int index)
    {
        return renderers[index];
    }

    public void MarkRenderComplete()
    {
        m_NumCompletedRenders++;
        if (m_NumCompletedRenders >= renderers.Count)
        {
            if (onRenderEnd != null)
            {
                onRenderEnd.Invoke();
                for (int i = 0; i < renderers.Count; i++)
                {
                    renderers[i].ReleaseResources();
                }
                //renderCamera.targetTexture = m_PrevTargetTexture;
            }
            m_NumCompletedRenders = 0;
        }
    }

    private void ForceActivateRender(ReplacementRenderer renderer)
    {
        m_CameraData.customRender += renderer.ReplacementRender;
    }

}
