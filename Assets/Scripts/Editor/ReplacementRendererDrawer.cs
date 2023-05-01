
using UnityEngine;
using UnityEditor;


[CustomPropertyDrawer(typeof(ReplacementRenderer), true)]
public class ReplacementRendererDrawer : PropertyDrawer
{
    private SerializedProperty m_EnabledProp;
    private SerializedProperty m_NameProp;
    private SerializedProperty m_ReplacementShaderMaterialProp;
    private SerializedProperty m_ShaderPassIndexProp;
    private SerializedProperty m_OutputProp;
    private SerializedProperty m_CullingMaskProp;
    private SerializedProperty m_RenderQueueRangeProp;
    private SerializedProperty m_TargetTextureProp;
    private SerializedProperty m_TempRenderTextureProp;
    private SerializedProperty m_SetCamerAspectToTextureProp;
    private ReplacementPass m_ReplacementPass;
    private static int s_linePadding = 2;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(m_ReplacementPass == null)
        {
            m_ReplacementPass = (ReplacementPass)property.serializedObject.targetObject;
        }

        FindProperties(property);

        EditorGUI.BeginProperty(position, label, property);

        label.text = string.IsNullOrEmpty(m_NameProp.stringValue) ? label.text : m_NameProp.stringValue;
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        position.height = EditorGUIUtility.singleLineHeight;

        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, string.Empty, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, m_EnabledProp);
            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
            if(EditorGUI.EndChangeCheck())
            {
                // Note that this assumes the property is drawn as a child of ReplacementPass
                // it breaks otherwise
                int propertyArrayIndex = GetArrayElementIndex(property.propertyPath);
                m_ReplacementPass.GetRenderer(propertyArrayIndex).enabled = 
                    m_EnabledProp.boolValue;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }

            EditorGUI.PropertyField(position, m_NameProp);
            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;

            EditorGUI.PropertyField(position, m_ReplacementShaderMaterialProp);
            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;


            if (m_ReplacementShaderMaterialProp.objectReferenceValue != null)
            {
                Material replacementShaderMaterial = (Material)m_ReplacementShaderMaterialProp.objectReferenceValue;
                Shader replacementShader = replacementShaderMaterial.shader;
                ShaderData shaderData = UnityEditor.ShaderUtil.GetShaderData(replacementShader);
                GUIContent[] shaderPasses = new GUIContent[shaderData.GetSubshader(0).PassCount];
                for (int i = 0; i < shaderPasses.Length; i++)
                {
                    shaderPasses[i] = new GUIContent(shaderData.GetSubshader(0).GetPass(i).Name);
                }
                m_ShaderPassIndexProp.intValue = EditorGUI.Popup(position,
                    new GUIContent("Shader Pass", m_ShaderPassIndexProp.tooltip),
                    m_ShaderPassIndexProp.intValue, shaderPasses);
                position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
            }
            else
            {
                m_ShaderPassIndexProp.intValue = 0;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.Popup(position,
                    new GUIContent("Shader Pass", m_ShaderPassIndexProp.tooltip),
                    0, new GUIContent[] { new GUIContent("None") });
                position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.PropertyField(position, m_RenderQueueRangeProp);
            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
            EditorGUI.PropertyField(position, m_CullingMaskProp);
            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
            EditorGUI.PropertyField(position, m_OutputProp);
            position.y += EditorGUIUtility.singleLineHeight + s_linePadding;

            var outputTarget = (ReplacementRenderer.OutputTarget)m_OutputProp.enumValueIndex;
            switch (outputTarget)
            {
                case ReplacementRenderer.OutputTarget.Camera:
                    break;
                case ReplacementRenderer.OutputTarget.RenderTexture:
                    EditorGUI.PropertyField(position, m_TargetTextureProp);
                    position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
                    EditorGUI.PropertyField(position, m_SetCamerAspectToTextureProp);
                    position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
                    break;
                case ReplacementRenderer.OutputTarget.TemporaryRenderTexture:
                    EditorGUI.PropertyField(position, m_TempRenderTextureProp);
                    position.y += EditorGUIUtility.singleLineHeight + s_linePadding;

                    position.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(position, m_SetCamerAspectToTextureProp);
                    position.y += EditorGUIUtility.singleLineHeight + s_linePadding;
                    break;
                default:
                    break;
            }

            EditorGUI.indentLevel--;
        }
    }

    private int GetNumPropertyChildren(SerializedProperty property)
    {
        return property.Copy().CountInProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GetPropertyHeight(property);
    }

    public static float GetPropertyHeight(SerializedProperty property)
    {
        SerializedProperty outputProp = property.FindPropertyRelative("output");
        float height = EditorGUI.GetPropertyHeight(property);
        if (property.isExpanded)
        {
            var outputTarget = (ReplacementRenderer.OutputTarget)outputProp.enumValueIndex;
            float targetTextureHeight = EditorGUIUtility.singleLineHeight + s_linePadding;
            float tmpRenderTextureHeight = EditorGUIUtility.singleLineHeight + s_linePadding;
            float setCameraAspectToTextureHeight = EditorGUIUtility.singleLineHeight + s_linePadding;
            switch (outputTarget)
            {
                case ReplacementRenderer.OutputTarget.Camera:
                    height -= targetTextureHeight;
                    height -= tmpRenderTextureHeight;
                    height -= setCameraAspectToTextureHeight;
                    break;
                case ReplacementRenderer.OutputTarget.RenderTexture:
                    height -= tmpRenderTextureHeight;
                    break;
                case ReplacementRenderer.OutputTarget.TemporaryRenderTexture:
                    height -= targetTextureHeight;
                    break;
                default:
                    break;
            }
        }
        return height;
    }

    private void FindProperties(SerializedProperty property)
    {
        m_EnabledProp = property.FindPropertyRelative("m_Enabled");
        m_NameProp = property.FindPropertyRelative("m_Name");
        m_ReplacementShaderMaterialProp = property.FindPropertyRelative("m_ReplacementShaderMaterial");
        m_ShaderPassIndexProp = property.FindPropertyRelative("m_ShaderPassIndex");
        m_OutputProp = property.FindPropertyRelative("output");
        m_CullingMaskProp = property.FindPropertyRelative("cullingMask");
        m_RenderQueueRangeProp = property.FindPropertyRelative("renderQueueRange");
        m_TargetTextureProp = property.FindPropertyRelative("m_RenderTexture");
        m_TempRenderTextureProp = property.FindPropertyRelative("tempRenderTexture");
        m_SetCamerAspectToTextureProp = property.FindPropertyRelative("setCameraAspectToTexture");
    }

    private static int GetArrayElementIndex(string propertyPath)
    {
        propertyPath = propertyPath.TrimEnd(']');
        propertyPath = propertyPath.Remove(0, propertyPath.IndexOf('[') + 1);
        
        return System.Int32.Parse(propertyPath);
    }
}
