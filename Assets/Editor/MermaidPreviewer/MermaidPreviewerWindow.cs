using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MermaidPreviewer
{
    public sealed class MermaidPreviewerWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Editor/MermaidPreviewer/MermaidPreviewerWindow.uxml";
        private const string UssPath = "Assets/Editor/MermaidPreviewer/MermaidPreviewerWindow.uss";
        private const float MinScale = 0.1f;
        private const float MaxScale = 8f;

        private Label _pathLabel;
        private Label _statusLabel;
        private Image _image;
        private VisualElement _viewport;
        private VisualElement _content;
        private ToastController _toast;
        private Slider _renderScaleSlider;
        private DropdownField _themeDropdown;
        private TextField _backgroundField;
        private Button _clearButton;

        private string _currentSource;
        private string _currentPath;
        private Texture2D _currentTexture;
        private int _renderRequestId;

        private float _scale = 1f;
        private Vector2 _translation;
        private bool _isPanning;
        private Vector2 _lastPointerPosition;
        private double _nextRenderTime;
        private bool _renderQueued;

        private static readonly List<string> ThemeOptions = new List<string>
        {
            "default",
            "neutral",
            "dark",
            "forest",
            "base"
        };

        [MenuItem("Window/Mermaid Previewer")]
        public static void ShowWindow()
        {
            GetWindow<MermaidPreviewerWindow>("Mermaid Previewer");
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (visualTree == null || styleSheet == null)
            {
                root.Add(new Label("Missing UI assets for Mermaid Previewer."));
                return;
            }

            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            _pathLabel = root.Q<Label>("pathLabel");
            _statusLabel = root.Q<Label>("statusLabel");
            _image = root.Q<Image>("previewImage");
            _viewport = root.Q<VisualElement>("viewport");
            _content = root.Q<VisualElement>("content");

            var refreshButton = root.Q<Button>("refreshButton");
            var fitButton = root.Q<Button>("fitButton");
            var openButton = root.Q<Button>("openButton");

            var toastRoot = root.Q<VisualElement>("toast");
            var toastLabel = root.Q<Label>("toastLabel");
            _toast = new ToastController(toastRoot, toastLabel);

            _renderScaleSlider = root.Q<Slider>("scaleSlider");
            if (_renderScaleSlider != null)
            {
                _renderScaleSlider.value = MermaidPreviewerPrefs.RenderScale;
                _renderScaleSlider.RegisterValueChangedCallback(OnRenderScaleChanged);
            }

            _themeDropdown = root.Q<DropdownField>("themeDropdown");
            if (_themeDropdown != null)
            {
                _themeDropdown.choices = new List<string>(ThemeOptions);
                var currentTheme = MermaidPreviewerPrefs.Theme;
                if (!_themeDropdown.choices.Contains(currentTheme))
                {
                    _themeDropdown.choices.Add(currentTheme);
                }

                _themeDropdown.value = currentTheme;
                _themeDropdown.RegisterValueChangedCallback(OnThemeChanged);
            }

            _backgroundField = root.Q<TextField>("backgroundField");
            if (_backgroundField != null)
            {
                _backgroundField.value = MermaidPreviewerPrefs.Background;
                _backgroundField.RegisterValueChangedCallback(OnBackgroundChanged);
            }

            _clearButton = root.Q<Button>("clearButton");
            if (_clearButton != null)
            {
                _clearButton.clicked += ClearPreview;
            }

            refreshButton.clicked += RefreshRender;
            fitButton.clicked += ResetView;
            if (openButton != null)
            {
                openButton.clicked += OpenFileDialog;
            }

            RegisterDragAndDrop(root);
            RegisterViewportInteractions();

            UpdatePathLabel();
            UpdateStatus();
            ResetView();


            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_renderQueued && EditorApplication.timeSinceStartup >= _nextRenderTime)
            {
                _renderQueued = false;
                RenderCurrent();
            }
        }

        private void RegisterDragAndDrop(VisualElement root)
        {
            root.RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            root.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = HasSupportedDrag(DragAndDrop.objectReferences, DragAndDrop.paths)
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();

            if (!TryHandleDrag(DragAndDrop.objectReferences, DragAndDrop.paths))
            {
                SetStatus("Error");
            }
        }

        private bool HasSupportedDrag(UnityEngine.Object[] objects, string[] paths)
        {
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj is TextAsset textAsset)
                    {
                        if (IsSupportedTextAsset(textAsset))
                        {
                            return true;
                        }
                    }
                    else if (obj is DefaultAsset defaultAsset)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(defaultAsset);
                        if (IsMarkdownPath(assetPath))
                        {
                            return true;
                        }
                    }
                }
            }

            if (paths != null)
            {
                foreach (var path in paths)
                {
                    if (IsSupportedPath(path))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryHandleDrag(UnityEngine.Object[] objects, string[] paths)
        {
            if (TryGetTextFromObjects(objects, out var text, out var path, out var error))
            {
                return ProcessText(text, path);
            }

            if (TryGetTextFromPaths(paths, out text, out path, out error))
            {
                return ProcessText(text, path);
            }

            ShowToast(error ?? "Unsupported object. Drag a .md or .txt file.", true);
            return false;
        }

        private bool TryGetTextFromObjects(UnityEngine.Object[] objects, out string text, out string path, out string error)
        {
            text = null;
            path = null;
            error = null;

            if (objects == null || objects.Length == 0)
            {
                error = "No drag data.";
                return false;
            }

            foreach (var obj in objects)
            {
                if (obj is TextAsset textAsset)
                {
                    var assetPath = AssetDatabase.GetAssetPath(textAsset);
                    if (!IsSupportedTextAsset(textAsset))
                    {
                        error = "Unsupported TextAsset type.";
                        return false;
                    }

                    text = textAsset.text;
                    path = string.IsNullOrEmpty(assetPath) ? null : Path.GetFullPath(assetPath);
                    if (string.IsNullOrEmpty(text))
                    {
                        error = "TextAsset is empty.";
                        return false;
                    }

                    return true;
                }

                if (obj is DefaultAsset defaultAsset)
                {
                    var assetPath = AssetDatabase.GetAssetPath(defaultAsset);
                    if (!IsMarkdownPath(assetPath))
                    {
                        continue;
                    }

                    try
                    {
                        var fullPath = Path.GetFullPath(assetPath);
                        text = File.ReadAllText(fullPath);
                        path = fullPath;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        error = $"Failed to read file: {ex.Message}";
                        return false;
                    }
                }
            }

            error = "Unsupported object. Drag a .md or .txt file.";
            return false;
        }

        private bool TryGetTextFromPaths(string[] paths, out string text, out string path, out string error)
        {
            text = null;
            path = null;
            error = null;

            if (paths == null || paths.Length == 0)
            {
                error = "No file paths in drag.";
                return false;
            }

            foreach (var candidate in paths)
            {
                if (!IsSupportedPath(candidate))
                {
                    continue;
                }

                try
                {
                    text = File.ReadAllText(candidate);
                    path = candidate;
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"Failed to read file: {ex.Message}";
                    return false;
                }
            }

            error = "Unsupported file type. Drag a .md or .txt file.";
            return false;
        }

        private bool ProcessText(string text, string sourcePath)
        {
            if (!MermaidParser.TryExtractFirst(text, out var mermaid))
            {
                ShowToast("No mermaid block found in file.", true);
                SetStatus("Error");
                return false;
            }

            _currentSource = mermaid;
            _currentPath = sourcePath;
            UpdatePathLabel();

            return RenderCurrent();
        }

        private bool RenderCurrent()
        {
            if (string.IsNullOrWhiteSpace(_currentSource))
            {
                ShowToast("No mermaid source loaded.", false);
                return false;
            }

            SetStatus("Rendering...");
            _renderRequestId++;
            var requestId = _renderRequestId;
            var source = _currentSource;

            EditorApplication.delayCall += () => RunRender(requestId, source);
            return true;
        }

        private void RefreshRender()
        {
            if (string.IsNullOrWhiteSpace(_currentSource))
            {
                ShowToast("Nothing to refresh.", false);
                return;
            }

            RenderCurrent();
        }

        private void OnRenderScaleChanged(ChangeEvent<float> evt)
        {
            var newScale = evt.newValue;
            MermaidPreviewerPrefs.RenderScale = newScale;
            UpdateStatus();

            QueueRender();
        }

        private void OnThemeChanged(ChangeEvent<string> evt)
        {
            var theme = evt.newValue;
            MermaidPreviewerPrefs.Theme = theme;
            QueueRender();
        }

        private void OnBackgroundChanged(ChangeEvent<string> evt)
        {
            var background = evt.newValue;
            MermaidPreviewerPrefs.Background = background;
            QueueRender();
        }

        private void QueueRender()
        {
            if (string.IsNullOrWhiteSpace(_currentSource))
            {
                return;
            }

            // Debouncing
            _renderQueued = true;
            _nextRenderTime = EditorApplication.timeSinceStartup + 0.2;
        }

        private void OpenFileDialog()
        {
            var path = EditorUtility.OpenFilePanel("Open Markdown", Application.dataPath, "md,txt");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!IsSupportedPath(path))
            {
                ShowToast("Unsupported file type.", true);
                return;
            }

            try
            {
                var text = File.ReadAllText(path);
                ProcessText(text, path);
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to read file: {ex.Message}", true);
            }
        }

        private void RunRender(int requestId, string source)
        {
            if (requestId != _renderRequestId)
            {
                return;
            }

            var runner = new MmdcRunner();
            var renderScale = MermaidPreviewerPrefs.RenderScale;
            var theme = MermaidPreviewerPrefs.Theme;
            var background = MermaidPreviewerPrefs.Background;
            if (!runner.TryRender(source, renderScale, theme, background, out var texture, out var errorMessage))
            {
                ShowToast(errorMessage, false);
                SetStatus("Error");
                return;
            }

            if (requestId != _renderRequestId)
            {
                return;
            }

            ApplyTexture(texture);
            UpdateStatus();
        }

        private void ClearPreview()
        {
            if (_currentTexture != null)
            {
                DestroyImmediate(_currentTexture);
                _currentTexture = null;
            }

            if (_image != null)
            {
                _image.image = null;
                _image.style.width = StyleKeyword.Auto;
                _image.style.height = StyleKeyword.Auto;
            }

            SetStatus("Cleared");
        }

        private void ApplyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (_currentTexture != null)
            {
                DestroyImmediate(_currentTexture);
            }

            _currentTexture = texture;
            _image.image = _currentTexture;
            _image.style.width = _currentTexture.width;
            _image.style.height = _currentTexture.height;
        }

        private void UpdateStatus(string customStatus = null)
        {
            if (_statusLabel == null)
            {
                return;
            }

            var scale = MermaidPreviewerPrefs.RenderScale;
            var status = customStatus ?? "Ready";
            _statusLabel.text = $"{status} | Scale: {scale:F1}";
        }

        private void SetStatus(string status)
        {
            UpdateStatus(status);
        }

        private void UpdatePathLabel()
        {
            if (_pathLabel == null)
            {
                return;
            }

            _pathLabel.text = string.IsNullOrEmpty(_currentPath) ? "No file loaded" : _currentPath;
        }

        private void ShowToast(string message, bool logWarning)
        {
            _toast.Show(message);
            if (logWarning && !string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning(message);
            }
        }

        private void RegisterViewportInteractions()
        {
            if (_viewport == null || _content == null)
            {
                return;
            }

            _viewport.RegisterCallback<WheelEvent>(OnWheelZoom, TrickleDown.TrickleDown);
            _viewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _viewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _viewport.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        private void OnWheelZoom(WheelEvent evt)
        {
            if (_content == null)
            {
                return;
            }

            var delta = -evt.delta.y;
            var zoomFactor = 1f + (delta > 0 ? 0.1f : -0.1f);
            var newScale = Mathf.Clamp(_scale * zoomFactor, MinScale, MaxScale);
            if (Mathf.Approximately(newScale, _scale))
            {
                return;
            }

            var mousePos = evt.localMousePosition;
            var contentPos = (mousePos - _translation) / _scale;
            _scale = newScale;
            _translation = mousePos - contentPos * _scale;

            ApplyTransform();
            evt.StopPropagation();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.MiddleMouse) return;

            _isPanning = true;
            _lastPointerPosition = (Vector2)evt.position;   // ✅ 显式
            _viewport.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isPanning) return;

            var currentPos = (Vector2)evt.position;         // ✅ 显式
            var delta = currentPos - _lastPointerPosition;  // ✅ Vector2 - Vector2
            _lastPointerPosition = currentPos;
            _translation += delta;
            ApplyTransform();
            evt.StopPropagation();
        }


        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isPanning)
            {
                return;
            }

            _isPanning = false;
            if (_viewport.HasPointerCapture(evt.pointerId))
            {
                _viewport.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (!_isPanning)
            {
                return;
            }

            _isPanning = false;
            if (_viewport.HasPointerCapture(evt.pointerId))
            {
                _viewport.ReleasePointer(evt.pointerId);
            }
        }

        private void ResetView()
        {
            _scale = 1f;
            _translation = Vector2.zero;
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            if (_content == null)
            {
                return;
            }

            _content.style.translate = new Translate(_translation.x, _translation.y, 0);
            _content.style.scale = new Scale(new Vector3(_scale, _scale, 1f));
        }

        private static bool IsSupportedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".md" || ext == ".txt";
        }

        private static bool IsMarkdownPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return string.Equals(Path.GetExtension(path), ".md", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedTextAsset(TextAsset textAsset)
        {
            var assetPath = AssetDatabase.GetAssetPath(textAsset);
            if (string.IsNullOrEmpty(assetPath))
            {
                return true;
            }

            return IsSupportedPath(assetPath);
        }
    }
}
