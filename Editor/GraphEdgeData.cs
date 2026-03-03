using System;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    [Serializable]
    public sealed class GraphEdgeData
    {
        public ScriptableObject From;
        public ScriptableObject To;
        public string PropertyPath;
    }
}