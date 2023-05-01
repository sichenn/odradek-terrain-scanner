using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SurfaceProperties : MonoBehaviour
{
    public enum ID
    {
        Layer,
        Custom
    }

    [SerializeField]
    private Renderer[] m_Renderers;
    public ID id;
    [Range(0, 255)]
    public int customID = 0;
    private MaterialPropertyBlock m_MatPropertyBlock;
    private const float kMaxNumLayers = 32f;

    private void OnValidate()
    {
        Renderer[] currentRenderers = GetComponentsInChildren<Renderer>();
        if (m_Renderers == null)
        {
            m_Renderers = currentRenderers;
        }
        if (m_MatPropertyBlock != null)
        {
            UpdateProperties();
            SetProperties();
        }
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            m_Renderers[i].SetPropertyBlock(m_MatPropertyBlock);
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (m_Renderers == null)
        {
            Renderer[] currentRenderers = GetComponentsInChildren<Renderer>();
            m_Renderers = currentRenderers;
        }
#endif
        if (m_MatPropertyBlock == null)
        {
            m_MatPropertyBlock = new MaterialPropertyBlock();
        }

        UpdateProperties();
        SetProperties();
    }

    private void OnDisable()
    {
        m_MatPropertyBlock.Clear();
        SetProperties();
    }

    private void UpdateProperties()
    {
        float layerValue = gameObject.layer / kMaxNumLayers;
        m_MatPropertyBlock.SetFloat("_ID", id == ID.Layer ? layerValue : customID / 255f);
    }

    private void SetProperties()
    {

        for (int i = 0; i < m_Renderers.Length; i++)
        {
            m_Renderers[i].SetPropertyBlock(m_MatPropertyBlock);
        }
    }
}
