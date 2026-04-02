using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    [Serializable]
    public sealed class ScriptableObjectGraphData
    {
        public ScriptableObject Root;
        public readonly Dictionary<ScriptableObject, GraphNodeData> Nodes = new();
        public readonly List<GraphEdgeData> Edges = new();

        public IReadOnlyList<GraphEdgeData> GetDisplayEdges(bool showDuplicates)
        {
            if (showDuplicates)
            {
                return Edges;
            }

            var uniqueEdges = new List<GraphEdgeData>();
            var seenPairs = new HashSet<(ScriptableObject From, ScriptableObject To)>();

            foreach (var edge in Edges)
            {
                if (edge.From == null || edge.To == null) continue;
                if (!seenPairs.Add((edge.From, edge.To))) continue;
                uniqueEdges.Add(edge);
            }

            var outgoingMap = uniqueEdges
                .GroupBy(edge => edge.From)
                .ToDictionary(group => group.Key, group => group.Select(edge => edge.To).ToList());

            var filteredEdges = new List<GraphEdgeData>();

            foreach (var edge in uniqueEdges)
            {
                var hasOuterDuplicate = uniqueEdges.Any(candidate =>
                    candidate != edge &&
                    candidate.To == edge.To &&
                    candidate.From != null &&
                    edge.From != null &&
                    IsReachable(candidate.From, edge.From, outgoingMap));

                if (!hasOuterDuplicate)
                {
                    filteredEdges.Add(edge);
                }
            }

            return filteredEdges;
        }

        private static bool IsReachable(
            ScriptableObject start,
            ScriptableObject target,
            IReadOnlyDictionary<ScriptableObject, List<ScriptableObject>> outgoingMap)
        {
            if (start == null || target == null || start == target)
            {
                return false;
            }

            if (!outgoingMap.TryGetValue(start, out var initialChildren) || initialChildren.Count == 0)
            {
                return false;
            }

            var visited = new HashSet<ScriptableObject> { start };
            var stack = new Stack<ScriptableObject>(initialChildren);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null || !visited.Add(current)) continue;
                if (current == target) return true;

                if (!outgoingMap.TryGetValue(current, out var children)) continue;

                foreach (var child in children)
                {
                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }
            }

            return false;
        }
    }
}
