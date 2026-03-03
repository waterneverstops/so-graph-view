using UnityEditor;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    public static class ScriptableObjectGraphContextMenu
    {
        [MenuItem("Assets/Open SO Graph View", false, 2000)]
        private static void OpenGraphView()
        {
            var so = Selection.activeObject as ScriptableObject;
            if (so == null) return;

            ScriptableObjectGraphWindow.Open(so);
        }
    
        [MenuItem("Assets/Open SO Graph View", true)]
        private static bool ValidateOpenGraphView()
        {
            return Selection.activeObject is ScriptableObject;
        }
        
        [MenuItem("CONTEXT/ScriptableObject/Open SO Graph View")]
        private static void OpenGraphView(MenuCommand command)
        {
            if (command.context is ScriptableObject so)
            {
                ScriptableObjectGraphWindow.Open(so);
            }
        }
    }
}