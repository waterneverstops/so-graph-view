using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScriptableObjectGraph.Editor
{
    public sealed class ScriptableObjectGraphWindow : EditorWindow
    {
        private const string ShowDuplicatesPreferenceKey = "ScriptableObjectGraph.Editor.ShowDuplicates";

        private ScriptableObject _root;
        private ScriptableObjectGraphView _graphView;
        private ToolbarToggle _showDuplicatesToggle;

        private string _lastSignature;
        private double _nextSignatureCheckTime;
        private bool _firstBuildDone;
        private bool _showDuplicates = true;

        public static void Open(ScriptableObject root)
        {
            var window = GetWindow<ScriptableObjectGraphWindow>();
            window.titleContent = new GUIContent("SO Graph View");
            window.SetRoot(root);
            window.Show();
        }

        private void OnEnable()
        {
            _showDuplicates = EditorPrefs.GetBool(ShowDuplicatesPreferenceKey, true);

            EditorApplication.update += OnEditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;

            if (_graphView == null)
            {
                CreateUi();
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

        private void CreateUi()
        {
            rootVisualElement.Clear();

            var toolbar = new Toolbar();
            _showDuplicatesToggle = new ToolbarToggle
            {
                text = "Show Duplicates",
                value = _showDuplicates
            };
            _showDuplicatesToggle.RegisterValueChangedCallback(OnShowDuplicatesChanged);
            toolbar.Add(_showDuplicatesToggle);
            rootVisualElement.Add(toolbar);

            _graphView = new ScriptableObjectGraphView();
            _graphView.style.flexGrow = 1f;
            rootVisualElement.Add(_graphView);
        }

        private void OnShowDuplicatesChanged(ChangeEvent<bool> evt)
        {
            _showDuplicates = evt.newValue;
            EditorPrefs.SetBool(ShowDuplicatesPreferenceKey, _showDuplicates);

            if (_root != null)
            {
                RebuildGraph(frame: false);
            }
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
            _graphView.Rebuild(graph, _showDuplicates);

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
