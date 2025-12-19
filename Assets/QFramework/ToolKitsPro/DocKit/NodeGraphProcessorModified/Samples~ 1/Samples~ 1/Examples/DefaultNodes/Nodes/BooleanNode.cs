#if UNITY_2019_4_OR_NEWER
using GraphProcessor;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif
using UnityEngine.UIElements;

[System.Serializable, NodeMenuItem("Primitives/Boolean")]
public class BooleanNode : BaseNode
{
    [Output("Out")]
    public bool		output;
	
    [Input("In")]
    public bool		input;

    public override string name => "Boolean";

    protected override void Process() => output = input;
}

#if UNITY_EDITOR
[NodeCustomEditor(typeof(BooleanNode))]
public class BooleanNodeView : BaseNodeView
{
    public override void Enable()
    {
        var booleanNode = nodeTarget as BooleanNode;

         var toolbarField = new Toggle()
        {
            value = booleanNode.input
        };

         booleanNode.onProcessed += () => toolbarField.value = booleanNode.input;

         toolbarField.RegisterValueChangedCallback((v) => {
            owner.RegisterCompleteObjectUndo("Updated floatNode input");
            booleanNode.input = v.newValue;
        });

        controlsContainer.Add(toolbarField);
    }
}

#endif

#endif