using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;


[RequireComponent(typeof(VisualEffect))]
[ExecuteAlways]
public class VFXDataBinder : MonoBehaviour
{
    [System.Serializable]
    public struct TemporaryRenderTextureData
    {
        public TemporaryRenderTexture data;
        public string propertyName;
    }

    public TemporaryRenderTextureData[] temporaryRenderTextureData;
    private VisualEffect m_VFX;

    private void Start()
    {
        if (m_VFX == null)
        {
            m_VFX = GetComponent<VisualEffect>();
        }
    }

    /// <summary>
    /// Bind all data to VFXGraph attached to the same GameObject
    /// </summary>
    public void Bind()
    {
        for (int i = 0; i < temporaryRenderTextureData.Length; i++)
        {
            if (temporaryRenderTextureData[i].data.texture != null)
            {
                m_VFX.SetTexture(temporaryRenderTextureData[i].propertyName, temporaryRenderTextureData[i].data.texture);
            }
        }
    }
}
