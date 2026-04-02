using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScriptableObjectGraph.Editor
{
    public sealed class ScriptableObjectGraphView : GraphView
    {
        private const float DefaultStartX = 80f;
        private const float DefaultStartY = 80f;
        private const float DefaultNodeWidth = 360f;
        private const float DefaultNodeMinHeight = 260f;
        private const float DefaultHorizontalSpacing = 140f;
        private const float DefaultVerticalSpacing = 40f;

        private readonly Dictionary<ScriptableObject, ScriptableObjectNodeView> _nodeViews = new();

        private bool _allowGraphMutationsInternally;
        private bool _isApplyingAutoLayout;
        private bool _layoutRefreshQueued;
        private ScriptableObjectGraphData _graphData;

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
                _graphData = graphData;

                if (graphData == null || graphData.Root == null) return;

                var layouts = ScriptableObjectGraphLayout.Calculate(
                    graphData,
                    startX: DefaultStartX,
                    startY: DefaultStartY,
                    nodeWidth: DefaultNodeWidth,
                    nodeMinHeight: DefaultNodeMinHeight,
                    horizontalSpacing: DefaultHorizontalSpacing,
                    verticalSpacing: DefaultVerticalSpacing);

                foreach (var pair in graphData.Nodes)
                {
                    var nodeData = pair.Value;
                    if (nodeData.Target == null) continue;

                    var node = new ScriptableObjectNodeView(nodeData.Target);
                    node.LayoutChanged += OnNodeLayoutChanged;

                    if (layouts.TryGetValue(nodeData.Target, out var nodeLayout))
                    {
                        node.SetPosition(nodeLayout.Rect);
                    }
                    else
                    {
                        node.SetPosition(new Rect(DefaultStartX, DefaultStartY, DefaultNodeWidth, DefaultNodeMinHeight));
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

                QueueAutoLayoutRefresh();
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

        private void OnNodeLayoutChanged()
        {
            if (_isApplyingAutoLayout) return;
            QueueAutoLayoutRefresh();
        }

        private void QueueAutoLayoutRefresh()
        {
            if (_layoutRefreshQueued || _graphData == null) return;

            _layoutRefreshQueued = true;
            schedule.Execute(() =>
            {
                _layoutRefreshQueued = false;
                ApplyAutoLayout();
            }).StartingIn(0);
        }

        private void ApplyAutoLayout()
        {
            if (_graphData == null || _nodeViews.Count == 0) return;

            var heights = new Dictionary<ScriptableObject, float>();
            foreach (var pair in _nodeViews)
            {
                var node = pair.Value;
                var height = node.layout.height;
                if (height <= 0f)
                {
                    height = node.GetPosition().height;
                }

                heights[pair.Key] = Mathf.Max(DefaultNodeMinHeight, height);
            }

            var layouts = ScriptableObjectGraphLayout.Calculate(
                _graphData,
                heights,
                startX: DefaultStartX,
                startY: DefaultStartY,
                nodeWidth: DefaultNodeWidth,
                nodeMinHeight: DefaultNodeMinHeight,
                horizontalSpacing: DefaultHorizontalSpacing,
                verticalSpacing: DefaultVerticalSpacing);

            _isApplyingAutoLayout = true;
            try
            {
                foreach (var pair in _nodeViews)
                {
                    if (!layouts.TryGetValue(pair.Key, out var nodeLayout)) continue;
                    pair.Value.SetPosition(nodeLayout.Rect);
                }
            }
            finally
            {
                _isApplyingAutoLayout = false;
            }
        }

        private void ClearGraphInternal()
        {
            ClearSelection();

            foreach (var node in _nodeViews.Values)
            {
                node.LayoutChanged -= OnNodeLayoutChanged;
            }

            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            foreach (var node in nodes.ToList())
            {
                RemoveElement(node);
            }

            _nodeViews.Clear();
            _layoutRefreshQueued = false;
        }
    }
}
