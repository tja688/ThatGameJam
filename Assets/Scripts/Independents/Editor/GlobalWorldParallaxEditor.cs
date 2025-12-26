using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 为 GlobalWorldParallaxManager 提供自定义 Inspector 界面
/// 修复了 PropertyField 参数错误及数组大小设置错误
/// </summary>
[CustomEditor(typeof(GlobalWorldParallaxManager))]
public class GlobalWorldParallaxEditor : Editor
{
    private string[] _sortingLayerNames;

    private void OnEnable()
    {
        // 获取项目中定义的所有排序图层名称
        _sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. 核心驱动设置", EditorStyles.boldLabel);
        // 修正：PropertyField 并不强求参数名，直接传入 Property 即可
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Player"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LevelStartX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("LevelEndX"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. 排序图层视差配置", EditorStyles.boldLabel);

        SerializedProperty list = serializedObject.FindProperty("LayerConfigs");

        // 一键填充功能
        if (GUILayout.Button("一键添加项目中所有排序图层"))
        {
            FillAllLayers(list);
        }

        EditorGUILayout.Space();

        // 遍历视差配置列表
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = element.FindPropertyRelative("SortingLayerName");
            SerializedProperty ampProp = element.FindPropertyRelative("Amplitude");
            SerializedProperty speedProp = element.FindPropertyRelative("Speed");

            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // 绘制下拉框
            int currentIndex = System.Array.IndexOf(_sortingLayerNames, nameProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;
            
            int newIndex = EditorGUILayout.Popup("目标排序图层", currentIndex, _sortingLayerNames);
            nameProp.stringValue = _sortingLayerNames[newIndex];

            // 绘制数值属性
            EditorGUILayout.PropertyField(ampProp);
            EditorGUILayout.PropertyField(speedProp);

            // 移除按钮
            GUI.color = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("移除此配置", EditorStyles.miniButton))
            {
                list.DeleteArrayElementAtIndex(i);
                break; // 数组结构改变，跳出循环待下一帧重绘
            }
            GUI.color = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("手动添加新配置"))
        {
            // 修正：使用 arraySize++ 增加长度
            list.arraySize++;
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    /// <summary>
    /// 将项目中尚未配置的排序图层全部加入列表
    /// </summary>
    private void FillAllLayers(SerializedProperty list)
    {
        HashSet<string> existingLayers = new HashSet<string>();
        for (int i = 0; i < list.arraySize; i++)
        {
            existingLayers.Add(list.GetArrayElementAtIndex(i).FindPropertyRelative("SortingLayerName").stringValue);
        }

        foreach (var layerName in _sortingLayerNames)
        {
            if (!existingLayers.Contains(layerName))
            {
                int index = list.arraySize;
                list.arraySize++;
                var element = list.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("SortingLayerName").stringValue = layerName;
                element.FindPropertyRelative("Amplitude").floatValue = 5f;
                element.FindPropertyRelative("Speed").floatValue = 5f;
            }
        }
    }
}