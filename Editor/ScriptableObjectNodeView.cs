using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    public sealed class ScriptableObjectNodeView : Node
    {
        private readonly ScriptableObject _target;

        private SerializedObject _serializedObject;

        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public string NodeGuid => _target == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_target));

        public ScriptableObjectNodeView(ScriptableObject target)
        {
            _target = target;
            title = target != null ? target.name : "<null>";

            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Copiable;
            capabilities &= ~Capabilities.Groupable;
            capabilities &= ~Capabilities.Renamable;

            CreatePorts();
            BuildInspectorUI();

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CreatePorts()
        {
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(ScriptableObject));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(ScriptableObject));

            OutputPort.portName = "";
            outputContainer.Add(OutputPort);
        }

        private void BuildInspectorUI()
        {
            if (_target == null) return;

            _serializedObject = new SerializedObject(_target);
            _serializedObject.Update();

            var iterator = _serializedObject.GetIterator();
            var enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script") continue;

                var copy = iterator.Copy();
                var field = new PropertyField(copy);
                field.Bind(_serializedObject);

                field.RegisterValueChangeCallback(_ =>
                {
                    _serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_target);
                });

                extensionContainer.Add(field);
            }
        }
    }
}