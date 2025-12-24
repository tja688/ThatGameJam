
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
        private const string ActiveTabClass = "tab-content--active";
        private const string ActiveTabButtonClass = "tab-button--active";
        private const string EmptyStateMessage = "No scan results found. Please run Scan in Config tab.";
        private const string NavIdL0 = "nav:l0";

        private Button _configTabButton;
        private Button _mermaidTabButton;
        private VisualElement _configTab;
        private VisualElement _mermaidTab;

        private TextField _scanRootField;
        private Button _scanRootButton;
        private TextField _outputFolderField;
        private Button _outputFolderButton;
        private TextField _excludeField;
        private DropdownField _featureModeDropdown;
        private TextField _featureTokenField;
        private TextField _namespacePrefixField;
        private Button _scanButton;
        private Label _scanStatusLabel;

        private Label _breadcrumbLabel;
        private Label _emptyStateLabel;
        private TreeView _navTree;

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

        private ArchitectureScannerSettings _settings;
        private ArchitectureScanIndex _scanIndex;
        private string _outputFolderFull;
        private readonly Dictionary<string, MermaidNavItem> _navItemsById = new Dictionary<string, MermaidNavItem>();
        private readonly Dictionary<string, int> _navTreeIdByNavId = new Dictionary<string, int>();

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

            _settings = ArchitectureScannerSettingsStore.LoadOrCreate();

            _configTabButton = root.Q<Button>("configTabButton");
            _mermaidTabButton = root.Q<Button>("mermaidTabButton");
            _configTab = root.Q<VisualElement>("configTab");
            _mermaidTab = root.Q<VisualElement>("mermaidTab");

            _configTabButton.clicked += () => ShowTab(TabKind.Config);
            _mermaidTabButton.clicked += () => ShowTab(TabKind.Mermaid);

            _scanRootField = root.Q<TextField>("scanRootField");
            _scanRootButton = root.Q<Button>("scanRootButton");
            _outputFolderField = root.Q<TextField>("outputFolderField");
            _outputFolderButton = root.Q<Button>("outputFolderButton");
            _excludeField = root.Q<TextField>("excludeField");
            _featureModeDropdown = root.Q<DropdownField>("featureModeDropdown");
            _featureTokenField = root.Q<TextField>("featureTokenField");
            _namespacePrefixField = root.Q<TextField>("namespacePrefixField");
            _scanButton = root.Q<Button>("scanButton");
            _scanStatusLabel = root.Q<Label>("scanStatusLabel");

            _breadcrumbLabel = root.Q<Label>("breadcrumbLabel");
            _emptyStateLabel = root.Q<Label>("emptyStateLabel");
            _navTree = root.Q<TreeView>("navTree");

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

            if (refreshButton != null)
            {
                refreshButton.clicked += RefreshRender;
            }

            if (fitButton != null)
            {
                fitButton.clicked += ResetView;
            }

            if (openButton != null)
            {
                openButton.clicked += OpenFileDialog;
            }

            ConfigureSettingsUI();
            ConfigureNavigationTree();

            RegisterDragAndDrop(_mermaidTab);
            RegisterViewportInteractions();

            UpdatePathLabel();
            UpdateStatus();
            ResetView();

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            ShowTab(TabKind.Config);
            ReloadScanIndex();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void ConfigureSettingsUI()
        {
            if (_settings == null)
            {
                return;
            }

            if (_scanRootField != null)
            {
                _scanRootField.SetValueWithoutNotify(_settings.scanRootPath);
                _scanRootField.RegisterValueChangedCallback(evt =>
                {
                    _settings.scanRootPath = evt.newValue?.Trim() ?? string.Empty;
                    SaveSettings();
                });
            }

            if (_scanRootButton != null)
            {
                _scanRootButton.clicked += () => SelectFolder(_scanRootField, value => _settings.scanRootPath = value);
            }

            if (_outputFolderField != null)
            {
                _outputFolderField.SetValueWithoutNotify(_settings.outputFolderPath);
                _outputFolderField.RegisterValueChangedCallback(evt =>
                {
                    _settings.outputFolderPath = evt.newValue?.Trim() ?? string.Empty;
                    SaveSettings();
                });
            }

            if (_outputFolderButton != null)
            {
                _outputFolderButton.clicked += () => SelectFolder(_outputFolderField, value => _settings.outputFolderPath = value);
            }

            if (_excludeField != null)
            {
                _excludeField.SetValueWithoutNotify(JoinPatterns(_settings.excludePatterns));
                _excludeField.RegisterValueChangedCallback(evt =>
                {
                    _settings.excludePatterns = ParsePatterns(evt.newValue);
                    SaveSettings();
                });
            }

            if (_featureModeDropdown != null)
            {
                _featureModeDropdown.choices = new List<string> { "Path", "Namespace" };
                _featureModeDropdown.value = _settings.featureMode.ToString();
                _featureModeDropdown.RegisterValueChangedCallback(evt =>
                {
                    _settings.featureMode = evt.newValue == "Namespace"
                        ? ArchitectureFeatureMode.Namespace
                        : ArchitectureFeatureMode.Path;
                    UpdateFeatureModeUI();
                    SaveSettings();
                });
            }

            if (_featureTokenField != null)
            {
                _featureTokenField.SetValueWithoutNotify(_settings.featurePathToken);
                _featureTokenField.RegisterValueChangedCallback(evt =>
                {
                    _settings.featurePathToken = evt.newValue?.Trim() ?? string.Empty;
                    SaveSettings();
                });
            }

            if (_namespacePrefixField != null)
            {
                _namespacePrefixField.SetValueWithoutNotify(_settings.namespacePrefix);
                _namespacePrefixField.RegisterValueChangedCallback(evt =>
                {
                    _settings.namespacePrefix = evt.newValue?.Trim() ?? string.Empty;
                    SaveSettings();
                });
            }

            if (_scanButton != null)
            {
                _scanButton.clicked += StartScan;
            }

            if (_scanStatusLabel != null)
            {
                _scanStatusLabel.text = "Ready";
            }

            UpdateFeatureModeUI();
        }

        private void ConfigureNavigationTree()
        {
            if (_navTree == null)
            {
                return;
            }

            _navTree.makeItem = () => new Label();
            _navTree.bindItem = (element, index) =>
            {
                var item = _navTree.GetItemDataForIndex<MermaidNavItem>(index);
                if (element is Label label)
                {
                    label.text = item.DisplayName;
                }
            };
            _navTree.selectionType = SelectionType.Single;
            _navTree.selectionChanged += OnTreeSelectionChanged;
        }

        private void UpdateFeatureModeUI()
        {
            var isPathMode = _settings != null && _settings.featureMode == ArchitectureFeatureMode.Path;
            _featureTokenField?.SetEnabled(isPathMode);
            _namespacePrefixField?.SetEnabled(!isPathMode);
        }

        private void SelectFolder(TextField field, Action<string> assign)
        {
            var initial = field != null ? field.value : Application.dataPath;
            var selected = EditorUtility.OpenFolderPanel("Select Folder", initial, string.Empty);
            if (string.IsNullOrEmpty(selected))
            {
                return;
            }

            var normalized = NormalizeFolderPath(selected);
            if (field != null)
            {
                field.value = normalized;
            }

            assign?.Invoke(normalized);
            SaveSettings();
        }

        private static string NormalizeFolderPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var full = Path.GetFullPath(path);
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relative = full.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return relative.Replace('\\', '/');
            }

            return full;
        }

        private void StartScan()
        {
            SetScanStatus("Scanning...");

            if (!ArchitectureScanner.ScanAndWrite(_settings, out var index, out var error))
            {
                SetScanStatus($"Error: {error}");
                ShowToast(error, true);
                return;
            }

            _scanIndex = index;
            _outputFolderFull = ArchitectureScanner.ResolveToFullPath(_settings.outputFolderPath);
            BuildNavigationTree(_scanIndex, _outputFolderFull);
            SetScanStatus("Scan completed.");
            ShowTab(TabKind.Mermaid);
            _settings.lastSelectedNavId = NavIdL0;
            SaveSettings();
            SelectDefaultNav();
        }

        private void ReloadScanIndex()
        {
            if (_settings == null)
            {
                ShowEmptyState();
                return;
            }

            if (!ArchitectureScanner.TryLoadIndex(_settings.outputFolderPath, out _scanIndex))
            {
                ShowEmptyState();
                return;
            }

            _outputFolderFull = ArchitectureScanner.ResolveToFullPath(_settings.outputFolderPath);
            BuildNavigationTree(_scanIndex, _outputFolderFull);
            SelectDefaultNav();
        }
        private void BuildNavigationTree(ArchitectureScanIndex index, string outputFolderFull)
        {
            if (_navTree == null)
            {
                return;
            }

            _navItemsById.Clear();
            _navTreeIdByNavId.Clear();

            var rootItems = new List<TreeViewItemData<MermaidNavItem>>();
            var nextId = 1;

            var l0Item = new MermaidNavItem(NavIdL0, "L0 - Features Overview", ResolveMermaidPath(outputFolderFull, index.l0MermaidPath), null, "L0 / Features Overview");
            rootItems.Add(new TreeViewItemData<MermaidNavItem>(nextId, l0Item));
            RegisterNavItem(l0Item, nextId);
            nextId++;

            var featuresRootChildren = new List<TreeViewItemData<MermaidNavItem>>();
            var features = new List<ArchitectureFeatureIndex>(index.features ?? new List<ArchitectureFeatureIndex>());
            features.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            foreach (var feature in features)
            {
                var featureChildren = new List<TreeViewItemData<MermaidNavItem>>();

                var l1Item = new MermaidNavItem($"feature:{feature.id}:l1", $"L1 - {feature.name} Internal", ResolveMermaidPath(outputFolderFull, feature.l1MermaidPath), null, $"L1 / {feature.name}");
                featureChildren.Add(new TreeViewItemData<MermaidNavItem>(nextId, l1Item));
                RegisterNavItem(l1Item, nextId);
                nextId++;

                var l2Item = new MermaidNavItem($"feature:{feature.id}:l2", $"L2 - {feature.name} External Calls", ResolveMermaidPath(outputFolderFull, feature.l2MermaidPath), null, $"L2 / {feature.name}");
                featureChildren.Add(new TreeViewItemData<MermaidNavItem>(nextId, l2Item));
                RegisterNavItem(l2Item, nextId);
                nextId++;

                var componentChildren = new List<TreeViewItemData<MermaidNavItem>>();
                var components = new List<ArchitectureComponentIndex>(feature.components ?? new List<ArchitectureComponentIndex>());
                components.Sort((a, b) =>
                {
                    var categoryCompare = string.Compare(a.category, b.category, StringComparison.OrdinalIgnoreCase);
                    return categoryCompare != 0
                        ? categoryCompare
                        : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                });

                foreach (var component in components)
                {
                    var label = string.IsNullOrEmpty(component.category)
                        ? component.name
                        : $"{component.category}: {component.name}";
                    var navItem = new MermaidNavItem(
                        $"feature:{feature.id}:component:{component.name}",
                        label,
                        ResolveMermaidPath(outputFolderFull, component.l2MermaidPath),
                        ResolveMermaidPath(outputFolderFull, feature.l2MermaidPath),
                        $"L2 / {feature.name} / {component.name}");

                    componentChildren.Add(new TreeViewItemData<MermaidNavItem>(nextId, navItem));
                    RegisterNavItem(navItem, nextId);
                    nextId++;
                }

                if (componentChildren.Count > 0)
                {
                    var componentsRoot = new MermaidNavItem($"feature:{feature.id}:components", "Components", null, null, $"Components / {feature.name}", false);
                    featureChildren.Add(new TreeViewItemData<MermaidNavItem>(nextId, componentsRoot, componentChildren));
                    nextId++;
                }

                var featureRoot = new MermaidNavItem($"feature:{feature.id}:root", feature.name, null, null, feature.name, false);
                featuresRootChildren.Add(new TreeViewItemData<MermaidNavItem>(nextId, featureRoot, featureChildren));
                nextId++;
            }

            var featuresRootItem = new MermaidNavItem("features", "Features", null, null, "Features", false);
            rootItems.Add(new TreeViewItemData<MermaidNavItem>(nextId, featuresRootItem, featuresRootChildren));

            _navTree.SetRootItems(rootItems);
            _navTree.Rebuild();
            HideEmptyState();
        }

        private void RegisterNavItem(MermaidNavItem item, int treeId)
        {
            _navItemsById[item.NavId] = item;
            _navTreeIdByNavId[item.NavId] = treeId;
        }

        private void OnTreeSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is MermaidNavItem navItem && navItem.IsSelectable)
                {
                    ApplyNavSelection(navItem);
                    return;
                }
            }
        }

        private void ApplyNavSelection(MermaidNavItem navItem)
        {
            var path = navItem.MermaidPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                path = !string.IsNullOrEmpty(navItem.FallbackMermaidPath) && File.Exists(navItem.FallbackMermaidPath)
                    ? navItem.FallbackMermaidPath
                    : null;
            }

            if (string.IsNullOrEmpty(path))
            {
                ShowToast("Mermaid file not found.", true);
                return;
            }

            LoadMermaidFromPath(path);
            UpdateBreadcrumb(navItem.Breadcrumb);
            _settings.lastSelectedNavId = navItem.NavId;
            SaveSettings();
        }

        private void SelectDefaultNav()
        {
            var navId = !string.IsNullOrEmpty(_settings?.lastSelectedNavId) ? _settings.lastSelectedNavId : NavIdL0;
            if (!SelectNavById(navId))
            {
                SelectNavById(NavIdL0);
            }
        }

        private bool SelectNavById(string navId)
        {
            if (string.IsNullOrEmpty(navId) || _navTree == null)
            {
                return false;
            }

            if (!_navTreeIdByNavId.TryGetValue(navId, out var treeId))
            {
                return false;
            }

            _navTree.SetSelection(new[] { treeId });
            return true;
        }

        private void UpdateBreadcrumb(string breadcrumb)
        {
            if (_breadcrumbLabel != null)
            {
                _breadcrumbLabel.text = breadcrumb ?? string.Empty;
            }
        }

        private static string ResolveMermaidPath(string outputFolderFull, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(outputFolderFull))
            {
                return null;
            }

            return Path.GetFullPath(Path.Combine(outputFolderFull, relativePath));
        }

        private void ShowEmptyState()
        {
            if (_emptyStateLabel != null)
            {
                _emptyStateLabel.text = EmptyStateMessage;
                _emptyStateLabel.style.display = DisplayStyle.Flex;
            }

            if (_navTree != null)
            {
                _navTree.SetRootItems(new List<TreeViewItemData<MermaidNavItem>>());
                _navTree.Rebuild();
            }
        }

        private void HideEmptyState()
        {
            if (_emptyStateLabel != null)
            {
                _emptyStateLabel.style.display = DisplayStyle.None;
            }
        }

        private void ShowTab(TabKind tab)
        {
            SetTabActive(_configTab, _configTabButton, tab == TabKind.Config);
            SetTabActive(_mermaidTab, _mermaidTabButton, tab == TabKind.Mermaid);

            if (tab == TabKind.Mermaid)
            {
                ReloadScanIndex();
            }
        }

        private static void SetTabActive(VisualElement tab, Button button, bool isActive)
        {
            if (tab != null)
            {
                if (isActive)
                {
                    tab.AddToClassList(ActiveTabClass);
                }
                else
                {
                    tab.RemoveFromClassList(ActiveTabClass);
                }
            }

            if (button != null)
            {
                if (isActive)
                {
                    button.AddToClassList(ActiveTabButtonClass);
                }
                else
                {
                    button.RemoveFromClassList(ActiveTabButtonClass);
                }
            }
        }

        private void SaveSettings()
        {
            ArchitectureScannerSettingsStore.Save(_settings);
        }

        private void SetScanStatus(string status)
        {
            if (_scanStatusLabel != null)
            {
                _scanStatusLabel.text = status;
            }
        }

        private static List<string> ParsePatterns(string raw)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return results;
            }

            var tokens = raw.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    results.Add(trimmed);
                }
            }

            return results;
        }

        private static string JoinPatterns(List<string> patterns)
        {
            if (patterns == null || patterns.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", patterns);
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
            if (root == null)
            {
                return;
            }

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
                        if (IsSupportedPath(assetPath))
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

            ShowToast(error ?? "Unsupported object. Drag a .md, .mmd, or .txt file.", true);
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
                    if (!IsSupportedPath(assetPath))
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

            error = "Unsupported object. Drag a .md, .mmd, or .txt file.";
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

            error = "Unsupported file type. Drag a .md, .mmd, or .txt file.";
            return false;
        }

        private bool ProcessText(string text, string sourcePath)
        {
            if (string.IsNullOrEmpty(text))
            {
                ShowToast("No mermaid content loaded.", true);
                SetStatus("Error");
                return false;
            }

            var mermaid = text;
            if (!IsMermaidRawPath(sourcePath))
            {
                if (!MermaidParser.TryExtractFirst(text, out mermaid))
                {
                    ShowToast("No mermaid block found in file.", true);
                    SetStatus("Error");
                    return false;
                }
            }

            return LoadMermaidSource(mermaid, sourcePath);
        }

        private bool LoadMermaidFromPath(string path)
        {
            try
            {
                var text = File.ReadAllText(path);
                return ProcessText(text, path);
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to read file: {ex.Message}", true);
                return false;
            }
        }

        private bool LoadMermaidSource(string mermaid, string sourcePath)
        {
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

            _renderQueued = true;
            _nextRenderTime = EditorApplication.timeSinceStartup + 0.2;
        }

        private void OpenFileDialog()
        {
            var path = EditorUtility.OpenFilePanel("Open Mermaid", Application.dataPath, "md,txt,mmd");
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
            if (texture == null || _image == null)
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
            if (evt.button != (int)MouseButton.MiddleMouse)
            {
                return;
            }

            _isPanning = true;
            _lastPointerPosition = (Vector2)evt.position;
            _viewport.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isPanning)
            {
                return;
            }

            var currentPos = (Vector2)evt.position;
            var delta = currentPos - _lastPointerPosition;
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
            return ext == ".md" || ext == ".txt" || ext == ".mmd";
        }

        private static bool IsMermaidRawPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mmd";
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

        private enum TabKind
        {
            Config,
            Mermaid
        }

        private sealed class MermaidNavItem
        {
            public MermaidNavItem(string navId, string displayName, string mermaidPath, string fallbackPath, string breadcrumb, bool isSelectable = true)
            {
                NavId = navId;
                DisplayName = displayName;
                MermaidPath = mermaidPath;
                FallbackMermaidPath = fallbackPath;
                Breadcrumb = breadcrumb;
                IsSelectable = isSelectable;
            }

            public string NavId { get; }
            public string DisplayName { get; }
            public string MermaidPath { get; }
            public string FallbackMermaidPath { get; }
            public string Breadcrumb { get; }
            public bool IsSelectable { get; }
        }
    }
}
