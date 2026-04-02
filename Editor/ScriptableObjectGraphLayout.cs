using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    public static class ScriptableObjectGraphLayout
    {
        public struct NodeLayout
        {
            public Rect Rect;
            public float CenterY;
        }

        public static Dictionary<ScriptableObject, NodeLayout> Calculate(
            ScriptableObjectGraphData graph,
            bool showDuplicates = true,
            IReadOnlyDictionary<ScriptableObject, float> nodeHeights = null,
            float startX = 80f,
            float startY = 80f,
            float nodeWidth = 360f,
            float nodeMinHeight = 240f,
            float horizontalSpacing = 140f,
            float verticalSpacing = 40f)
        {
            var result = new Dictionary<ScriptableObject, NodeLayout>();

            if (graph == null || graph.Root == null) return result;

            var childrenMap = new Dictionary<ScriptableObject, List<ScriptableObject>>();
            foreach (var node in graph.Nodes.Keys)
            {
                childrenMap[node] = new List<ScriptableObject>();
            }

            foreach (var edge in graph.GetDisplayEdges(showDuplicates))
            {
                if (edge.From == null || edge.To == null) continue;

                if (!childrenMap.TryGetValue(edge.From, out var list))
                {
                    list = new List<ScriptableObject>();
                    childrenMap[edge.From] = list;
                }

                if (!list.Contains(edge.To)) list.Add(edge.To);
            }

            foreach (var key in childrenMap.Keys.ToList())
            {
                childrenMap[key] = childrenMap[key]
                    .OrderBy(x => x.name)
                    .ThenBy(GetStableKey)
                    .ToList();
            }

            var placed = new HashSet<ScriptableObject>();
            var nextLeafY = 0f;

            float GetNodeHeight(ScriptableObject node)
            {
                if (node == null) return nodeMinHeight;
                if (nodeHeights != null && nodeHeights.TryGetValue(node, out var height))
                {
                    return Mathf.Max(nodeMinHeight, height);
                }

                return nodeMinHeight;
            }

            float LayoutNode(ScriptableObject node, int depth)
            {
                if (node == null) return nextLeafY;

                if (!placed.Add(node))
                {
                    return result[node].CenterY;
                }

                var nodeHeight = GetNodeHeight(node);
                var children = childrenMap.TryGetValue(node, out var c) ? c : new List<ScriptableObject>();
                var unplacedChildren = children.Where(x => x != null && !placed.Contains(x)).ToList();

                float centerY;

                if (unplacedChildren.Count == 0)
                {
                    centerY = nextLeafY + nodeHeight * 0.5f;
                    nextLeafY += nodeHeight + verticalSpacing;
                }
                else
                {
                    var childCenters = new List<float>();

                    foreach (var child in unplacedChildren)
                    {
                        var childCenter = LayoutNode(child, depth + 1);
                        childCenters.Add(childCenter);
                    }

                    centerY = (childCenters.First() + childCenters.Last()) * 0.5f;
                }

                var x = startX + depth * (nodeWidth + horizontalSpacing);
                var y = startY + centerY - nodeHeight * 0.5f;

                result[node] = new NodeLayout
                {
                    CenterY = centerY,
                    Rect = new Rect(x, y, nodeWidth, nodeHeight)
                };

                return centerY;
            }

            LayoutNode(graph.Root, 0);

            foreach (var node in graph.Nodes.Keys)
            {
                if (node == null || result.ContainsKey(node))
                    continue;

                var nodeHeight = GetNodeHeight(node);
                var centerY = nextLeafY + nodeHeight * 0.5f;

                result[node] = new NodeLayout
                {
                    CenterY = centerY,
                    Rect = new Rect(startX, startY + nextLeafY, nodeWidth, nodeHeight)
                };

                nextLeafY += nodeHeight + verticalSpacing;
            }

            ResolveColumnOverlaps(graph, result, verticalSpacing);

            return result;
        }

        private static void ResolveColumnOverlaps(
            ScriptableObjectGraphData graph,
            Dictionary<ScriptableObject, NodeLayout> layouts,
            float verticalSpacing)
        {
            foreach (var depthGroup in graph.Nodes
                         .Where(pair => pair.Key != null && layouts.ContainsKey(pair.Key))
                         .GroupBy(pair => pair.Value.Depth))
            {
                var orderedNodes = depthGroup
                    .Select(pair => pair.Key)
                    .OrderBy(node => layouts[node].Rect.y)
                    .ToList();

                var nextY = float.NegativeInfinity;
                foreach (var node in orderedNodes)
                {
                    var layout = layouts[node];
                    if (layout.Rect.y < nextY)
                    {
                        layout.Rect.y = nextY;
                        layout.CenterY = layout.Rect.center.y;
                        layouts[node] = layout;
                    }

                    nextY = layout.Rect.y + layout.Rect.height + verticalSpacing;
                }
            }
        }

        private static string GetStableKey(ScriptableObject so)
        {
            var path = AssetDatabase.GetAssetPath(so);

            if (!string.IsNullOrEmpty(path)) return path;

            return so.GetInstanceID().ToString();
        }
    }
}
