using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.Rendering.HighDefinition;

[CustomEditor(typeof(ReplacementPass))]
public class ReplacementPassEditor : Editor
{
    private SerializedProperty m_Renderers;
    private SerializedProperty m_OnRendererEnd;
    private ReorderableList m_RenderersList;
    private ReplacementPass m_ReplacementPass;
    private HDAdditionalCameraData m_CameraData;
    private ReorderableList.Defaults m_ReorderableListDefaults;
    private static System.Type s_GameViewType;

    private void OnEnable()
    {
        m_ReplacementPass = (ReplacementPass)target;
        m_CameraData = m_ReplacementPass.GetComponent<HDAdditionalCameraData>();
        m_Renderers = serializedObject.FindProperty("renderers");
        m_OnRendererEnd = serializedObject.FindProperty("onRenderEnd");
        m_RenderersList = new ReorderableList(serializedObject, m_Renderers);
        m_RenderersList.drawHeaderCallback = DrawListHeader;
        m_RenderersList.drawElementCallback = DrawListItem;
        m_RenderersList.elementHeightCallback = GetListItemHeight;
        m_RenderersList.onAddCallback += AddRenderer;
        m_RenderersList.onRemoveCallback += RemoveRenderer;

        s_GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    }

    public override void OnInspectorGUI()
    {
        // always gauranteed to have zero or one instance
        var gameViewWindow = Resources.FindObjectsOfTypeAll(s_GameViewType);
        if((gameViewWindow.Length == 0 || !((EditorWindow)gameViewWindow[0]).hasFocus))
        {
            EditorGUILayout.HelpBox("Replacement Pass only works when Game View is active", MessageType.Warning);
        }

        //EditorWindow gameView = EditorWindow.GetWindow(, false, null, false);
        //EditorGUILayout.LabelField("gameview active?", gameView.hasFocus ? "yes" : "no");

        serializedObject.Update();
        m_RenderersList.DoLayoutList();
        EditorGUI.BeginChangeCheck();
        //int numRenderers = m_Renderers.arraySize;
        //EditorGUILayout.PropertyField(m_Renderers);
        //if(EditorGUI.EndChangeCheck() && m_ReplacementPass.enabled)
        //{
        //    if(m_Renderers.arraySize != numRenderers)
        //    {
        //        m_ReplacementPass.enabled = false;
        //        m_ReplacementPass.enabled = true;
        //    }
        //}
        EditorGUILayout.PropertyField(m_OnRendererEnd);
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.LabelField("Has Custom Render: ",
            m_CameraData.hasCustomRender ? "True" : "False");
    }

    private void DrawListHeader(Rect rect)
    {
        EditorGUI.LabelField(rect,
            new GUIContent(m_Renderers.displayName, m_Renderers.tooltip));
    }

    private void DrawListItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element =
            m_RenderersList.serializedProperty.GetArrayElementAtIndex(index);
        rect.x += 8f;
        rect.width -= 8f;
        EditorGUI.PropertyField(rect, element);
    }

    private float GetListItemHeight(int index)
    {
        if (m_RenderersList.serializedProperty.arraySize == 0)
        {
            // for some reason this method is called even when there are no elements
            return 0f;
        }
        SerializedProperty element = m_RenderersList.serializedProperty.GetArrayElementAtIndex(index);
        return EditorGUI.GetPropertyHeight(element);
    }

    private void AddRenderer(ReorderableList list)
    {
        m_ReplacementPass.AddRenderer(new ReplacementRenderer(m_ReplacementPass));
    }

    private void RemoveRenderer(ReorderableList list)
    {
        m_ReplacementPass.RemoveRenderer(list.index);
        list.serializedProperty.DeleteArrayElementAtIndex(list.index);
    }

    // TODO: toggle enable to activate/deactivate
    // now we're not removing renderers correctly
}
