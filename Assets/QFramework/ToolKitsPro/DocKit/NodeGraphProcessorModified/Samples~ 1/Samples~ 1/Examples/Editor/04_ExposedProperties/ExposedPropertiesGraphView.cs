#if UNITY_2019_4_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GraphProcessor;
using System;
using UnityEditor;

public class ExposedPropertiesGraphView : BaseGraphView
{
	public ExposedPropertiesGraphView(EditorWindow window) : base(window) {}
}
#endif