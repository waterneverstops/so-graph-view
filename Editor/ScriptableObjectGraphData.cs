using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    [Serializable]
    public sealed class ScriptableObjectGraphData
    {
        public ScriptableObject Root;
        public readonly Dictionary<ScriptableObject, GraphNodeData> Nodes = new();
        public readonly List<GraphEdgeData> Edges = new();
    }
}