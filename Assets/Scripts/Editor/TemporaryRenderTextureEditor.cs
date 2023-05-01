using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TemporaryRenderTexture))]
public class TemporaryRenderTextureEditor : Editor
{
    static float s_Padding = 2f;
    static float s_AssetLabelsHeight = (EditorGUIUtility.singleLineHeight + s_Padding) * 3;
    static float s_InspectorHeaderHeight = (EditorGUIUtility.singleLineHeight + s_Padding) * 3;
    private RenderTexture m_LastActiveTexture;
    private TemporaryRenderTexture m_Target;
    private bool m_LivePreview = false;
    private static GUIContent s_LivePreviewGUI =
        new GUIContent("Live", "On: show the current texture\nOff: show the texture in its last active state");
    private static UnityEngine.Texture s_EnableLiveIcon;
    private static UnityEngine.Texture s_DisableLiveIcon;

    private SerializedProperty m_Width,
                                m_Height,
                                m_AntiAliasing,
                                m_Format,
                                m_DepthBuffer,
                                m_UseMipMap,
                                m_AutoGenerateMips,
                                m_WrapMode,
                                m_FilterMode,
                                m_ReadWrite;

    private void OnEnable()
    {
        m_Target = (TemporaryRenderTexture)serializedObject.targetObject;

        m_Width = serializedObject.FindProperty("width");
        m_Height = serializedObject.FindProperty("height");
        m_AntiAliasing = serializedObject.FindProperty("m_AntiAliasing");
        m_Format = serializedObject.FindProperty("format");
        m_DepthBuffer = serializedObject.FindProperty("m_DepthBuffer");
        m_UseMipMap = serializedObject.FindProperty("useMipMap");
        m_AutoGenerateMips = serializedObject.FindProperty("autoGenerateMips");
        m_WrapMode = serializedObject.FindProperty("wrapMode");
        m_FilterMode = serializedObject.FindProperty("filterMode");
        m_ReadWrite = serializedObject.FindProperty("readWrite");

        if (s_EnableLiveIcon == null || s_DisableLiveIcon == null)
        {
            s_EnableLiveIcon = EditorGUIUtility.IconContent("d_Linked").image;
            s_DisableLiveIcon = EditorGUIUtility.IconContent("d_Unlinked").image;
        }

        if (!m_LastActiveTexture)
        {
            m_LastActiveTexture = new RenderTexture(m_Target.width, m_Target.height, m_Target.depthBuffer, m_Target.format);
            //Color blackColor = Color.black;
            //Color[] resetColorArray = m_LastActiveTexture.GetPixels();
            //for (int i = 0; i < resetColorArray.Length; i++)
            //{
            //    resetColorArray[i] = blackColor;
            //}
            //m_LastActiveTexture.SetPixels(resetColorArray);
            //m_LastActiveTexture.Apply();
        }

        m_Target.onTextureRender += UpdateLastActiveTextureEditor;
    }

    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_Width);
        EditorGUILayout.PropertyField(m_Height);
        EditorGUILayout.PropertyField(m_AntiAliasing);
        EditorGUILayout.PropertyField(m_Format);
        EditorGUILayout.PropertyField(m_DepthBuffer);
        EditorGUILayout.PropertyField(m_UseMipMap);
        EditorGUI.BeginDisabledGroup(!m_UseMipMap.boolValue);
        EditorGUILayout.PropertyField(m_AutoGenerateMips);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.PropertyField(m_WrapMode);
        EditorGUILayout.PropertyField(m_FilterMode);
        if (EditorGUI.EndChangeCheck())
        {
            DestroyImmediate(m_LastActiveTexture);
            m_LastActiveTexture = new RenderTexture(m_Target.width, m_Target.height, m_Target.depthBuffer, m_Target.format);
            m_LastActiveTexture.useMipMap = m_Target.useMipMap;
            m_LastActiveTexture.autoGenerateMips = m_Target.autoGenerateMips;
            m_LastActiveTexture.filterMode= m_Target.filterMode;
            m_LastActiveTexture.wrapMode = m_Target.wrapMode;
            EditorUtility.SetDirty(m_Target);
        }
        EditorGUILayout.PropertyField(m_ReadWrite);

        float textureAspect = m_Target.width / m_Target.height;


        float editorHeight = Screen.height - s_AssetLabelsHeight - s_InspectorHeaderHeight;
        float editorWidth = Screen.width;
        float previewTextureWidth = Mathf.Min(EditorGUIUtility.currentViewWidth - 2 * s_Padding, editorHeight - 2 * s_Padding);
        float previeTextureHeight = previewTextureWidth / textureAspect;

        var drawRect = EditorGUILayout.GetControlRect(false, previeTextureHeight);
        drawRect.width = previewTextureWidth;
        drawRect.height = previeTextureHeight;
        drawRect.x = (editorWidth - 2 * s_Padding - previewTextureWidth) / 2f + s_Padding;

        if (m_Target.texture)
        {
            // TODO: Add RGBA view
            UpdateLastActiveTexture();
            EditorGUI.DrawPreviewTexture(drawRect, m_LivePreview ? m_Target.texture : m_LastActiveTexture);
        }
        else
        {
            EditorGUI.DrawPreviewTexture(drawRect, (m_LivePreview || !m_LastActiveTexture) ?Texture2D.blackTexture: m_LastActiveTexture);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        s_LivePreviewGUI.image = m_LivePreview ? s_EnableLiveIcon : s_DisableLiveIcon;
        m_LivePreview = GUILayout.Toggle(m_LivePreview, s_LivePreviewGUI, EditorStyles.miniButton);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void OnDisable()
    {
        m_Target.onTextureRender -= UpdateLastActiveTextureEditor;
        DestroyImmediate(m_LastActiveTexture);
    }

    private void UpdateLastActiveTexture()
    {
        //RenderTexture prevActive = RenderTexture.active;
        //RenderTexture.active = m_Target.texture;
        //m_LastActiveTexture.ReadPixels(new Rect(0, 0, m_Target.width, m_Target.height), 0, 0);
        //m_LastActiveTexture.Apply();
        //RenderTexture.active = prevActive;
        Graphics.CopyTexture(m_Target.texture, m_LastActiveTexture);
        //Graphics.Blit(m_Target.texture, m_LastActiveTexture);
    }

    private void UpdateLastActiveTextureEditor()
    {
        UpdateLastActiveTexture();
        this.Repaint();
    }
}
