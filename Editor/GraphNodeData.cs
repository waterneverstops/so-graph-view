using System;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    [Serializable]
    public sealed class GraphNodeData
    {
        public ScriptableObject Target;
        public int Depth;
    }
}