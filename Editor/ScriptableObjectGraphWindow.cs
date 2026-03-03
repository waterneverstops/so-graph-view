using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScriptableObjectGraph.Editor
{
    public sealed class ScriptableObjectGraphWindow : EditorWindow
    {
        private ScriptableObject _root;
        private ScriptableObjectGraphView _graphView;

        private string _lastSignature;
        private double _nextSignatureCheckTime;
        private bool _firstBuildDone;

        public static void Open(ScriptableObject root)
        {
            var window = GetWindow<ScriptableObjectGraphWindow>();
            window.titleContent = new GUIContent("SO Graph View");
            window.SetRoot(root);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;

            if (_graphView == null)
            {
                _graphView = new ScriptableObjectGraphView();
                rootVisualElement.Add(_graphView);
            }

            if (_root == null) return;
            RebuildGraph(frame: true);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void SetRoot(ScriptableObject root)
        {
            _root = root;

            if (_graphView == null) return;
            RebuildGraph(frame: true);
        }

        private void OnUndoRedo()
        {
            if (_root == null) return;
            RebuildGraph(frame: false);
        }

        private void OnEditorUpdate()
        {
            if (_root == null || _graphView == null) return;

            if (EditorApplication.timeSinceStartup < _nextSignatureCheckTime) return;

            _nextSignatureCheckTime = EditorApplication.timeSinceStartup + 0.25d;

            var newSignature = BuildReferenceSignature(_root);
            if (newSignature != _lastSignature)
            {
                RebuildGraph(frame: false);
            }
        }

        private void RebuildGraph(bool frame)
        {
            if (_root == null || _graphView == null) return;

            var oldSelectedGuid = _graphView.TryGetSelectedNodeGuid();

            var graph = ScriptableObjectGraphBuilder.Build(_root);
            _graphView.Rebuild(graph);

            _lastSignature = BuildReferenceSignature(_root);

            if (!_firstBuildDone || frame)
            {
                _graphView.FrameAll();
                _firstBuildDone = true;
            }
            else
            {
                _graphView.TryRestoreSelection(oldSelectedGuid);
            }
        }

        private static string BuildReferenceSignature(ScriptableObject root)
        {
            var graph = ScriptableObjectGraphBuilder.Build(root);

            return string.Join("|", graph.Edges
                    .Where(e => e.From != null)
                    .Select(e =>
                    {
                        var fromGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.From));
                        var toGuid = e.To != null
                            ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(e.To))
                            : "null";

                        return $"{fromGuid}->{toGuid}@{e.PropertyPath}";
                    })
                    .OrderBy(x => x));
        }
    }
}