using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScriptableObjectGraph.Editor
{
    public sealed class ScriptableObjectGraphView : GraphView
    {
        private readonly Dictionary<ScriptableObject, ScriptableObjectNodeView> _nodeViews = new();

        private bool _allowGraphMutationsInternally;

        public ScriptableObjectGraphView()
        {
            style.flexGrow = 1f;

            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            graphViewChanged = OnGraphViewChanged;

            this.AddManipulator(new ContextualMenuManipulator(evt => { evt.menu.MenuItems().Clear(); }));
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (_allowGraphMutationsInternally) return change;

            change.elementsToRemove?.Clear();
            change.edgesToCreate?.Clear();
            change.movedElements?.Clear();

            return change;
        }

        public void Rebuild(ScriptableObjectGraphData graphData)
        {
            _allowGraphMutationsInternally = true;
            try
            {
                ClearGraphInternal();

                if (graphData == null || graphData.Root == null) return;

                var layouts = ScriptableObjectGraphLayout.Calculate(graphData, startX: 80f, startY: 80f, nodeWidth: 360f, nodeMinHeight: 260f,
                    horizontalSpacing: 140f, verticalSpacing: 40f);

                foreach (var pair in graphData.Nodes)
                {
                    var nodeData = pair.Value;
                    if (nodeData.Target == null) continue;

                    var node = new ScriptableObjectNodeView(nodeData.Target);

                    if (layouts.TryGetValue(nodeData.Target, out var nodeLayout))
                    {
                        node.SetPosition(nodeLayout.Rect);
                    }
                    else
                    {
                        node.SetPosition(new Rect(80, 80, 360, 260));
                    }

                    _nodeViews[nodeData.Target] = node;
                    AddElement(node);
                }

                foreach (var edgeData in graphData.Edges)
                {
                    if (edgeData.From == null || edgeData.To == null) continue;

                    if (!_nodeViews.TryGetValue(edgeData.From, out var fromNode)) continue;

                    if (!_nodeViews.TryGetValue(edgeData.To, out var toNode)) continue;

                    var edge = new Edge
                    {
                        output = fromNode.OutputPort,
                        input = toNode.InputPort
                    };

                    edge.capabilities &= ~Capabilities.Deletable;
                    edge.capabilities &= ~Capabilities.Selectable;
                    edge.capabilities &= ~Capabilities.Copiable;

                    edge.output.Connect(edge);
                    edge.input.Connect(edge);

                    AddElement(edge);
                }
            }
            finally
            {
                _allowGraphMutationsInternally = false;
            }
        }

        public string TryGetSelectedNodeGuid()
        {
            foreach (var item in selection)
            {
                if (item is ScriptableObjectNodeView node)
                    return node.NodeGuid;
            }

            return null;
        }

        public void TryRestoreSelection(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;

            ClearSelection();

            var node = _nodeViews.Values.FirstOrDefault(n => n.NodeGuid == guid);
            if (node == null) return;
            AddToSelection(node);
        }

        private void ClearGraphInternal()
        {
            ClearSelection();

            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            foreach (var node in nodes.ToList())
            {
                RemoveElement(node);
            }

            _nodeViews.Clear();
        }
    }
}