#if UNITY_2019_4_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GraphProcessor;
using System;
using UnityEditor;

public class CustomToolbarGraphView : BaseGraphView
{
	public CustomToolbarGraphView(EditorWindow window) : base(window) {}
}
#endif