using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    public static class ScriptableObjectGraphBuilder
    {
        public static ScriptableObjectGraphData Build(ScriptableObject root, int maxDepth = 32)
        {
            var graph = new ScriptableObjectGraphData
            {
                Root = root
            };

            if (root == null) return graph;

            var visited = new HashSet<ScriptableObject>();

            BuildRecursive(root, depth: 0, graph, visited, maxDepth);
            return graph;
        }

        private static void BuildRecursive(ScriptableObject current, int depth, ScriptableObjectGraphData graph, HashSet<ScriptableObject> visited, int maxDepth)
        {
            if (current == null || depth > maxDepth) return;

            if (!graph.Nodes.ContainsKey(current))
            {
                graph.Nodes[current] = new GraphNodeData
                {
                    Target = current,
                    Depth = depth
                };
            }
            else
            {
                graph.Nodes[current].Depth = Mathf.Min(graph.Nodes[current].Depth, depth);
            }

            if (!visited.Add(current)) return;

            using var serializedObject = new SerializedObject(current);
            var iterator = serializedObject.GetIterator();

            while (iterator.Next(true))
            {
                if (iterator.name == "m_Script") continue;

                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;

                var referenced = iterator.objectReferenceValue as ScriptableObject;
                if (referenced == null) continue;

                graph.Edges.Add(new GraphEdgeData
                {
                    From = current,
                    To = referenced,
                    PropertyPath = iterator.propertyPath,
                });

                if (!graph.Nodes.ContainsKey(referenced))
                {
                    graph.Nodes[referenced] = new GraphNodeData
                    {
                        Target = referenced,
                        Depth = depth + 1
                    };
                }
                else
                {
                    graph.Nodes[referenced].Depth = Mathf.Min(graph.Nodes[referenced].Depth, depth + 1);
                }

                BuildRecursive(referenced, depth + 1, graph, visited, maxDepth);
            }
        }
    }
}