using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class GroundSpritePlacerWindow : EditorWindow
{
    private const string DefaultParentName = "GroundSpriteStamps";

    private Sprite _sprite;
    private Transform _parent;
    private LayerMask _floorMask;
    private float _raycastDistance = 200f;
    private bool _detectionEnabled = true;
    private float _raycastPlaneZ = 0f;
    private KeyCode _placeHotkey = KeyCode.P;
    private Vector3 _placementOffset = Vector3.zero;
    private Vector3 _placementScale = Vector3.one;
    private int _targetLayer;
    private string[] _sortingLayerNames;
    private string _sortingLayerName = "Default";
    private int _sortingOrder;
    private bool _autoSelectNew = true;
    private Vector3 _bulkOffsetDelta = Vector3.zero;

    [MenuItem("Tools/Ground Sprite Placer")]
    public static void ShowWindow()
    {
        var window = GetWindow<GroundSpritePlacerWindow>("Ground Sprite Placer");
        window.minSize = new Vector2(360, 420);
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        RefreshSortingLayers();
        if (_floorMask.value == 0)
        {
            int floorLayer = LayerMask.NameToLayer("floor");
            if (floorLayer < 0) floorLayer = LayerMask.NameToLayer("Floor");
            _floorMask = floorLayer >= 0 ? 1 << floorLayer : ~0;
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("场景射线预览 + 快速布置贴图", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("在 Scene 视图移动鼠标查看射线命中点，按快捷键即可放置。", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("放置设置", EditorStyles.boldLabel);
        _sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", _sprite, typeof(Sprite), false);
        _parent = (Transform)EditorGUILayout.ObjectField("Parent", _parent, typeof(Transform), true);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("使用/创建默认父物体"))
            {
                _parent = GetOrCreateParent();
            }
            if (GUILayout.Button("使用当前选择"))
            {
                if (Selection.activeTransform != null)
                    _parent = Selection.activeTransform;
            }
        }

        _floorMask = LayerMaskField("Floor Mask", _floorMask);
        _raycastDistance = EditorGUILayout.FloatField("Raycast Distance", _raycastDistance);
        _raycastPlaneZ = EditorGUILayout.FloatField("Raycast Plane Z", _raycastPlaneZ);
        _placeHotkey = (KeyCode)EditorGUILayout.EnumPopup("Place Hotkey", _placeHotkey);
        _detectionEnabled = EditorGUILayout.Toggle("Enable Detection", _detectionEnabled);
        _autoSelectNew = EditorGUILayout.Toggle("Auto Select New", _autoSelectNew);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
        _placementOffset = EditorGUILayout.Vector3Field("Offset", _placementOffset);
        _placementScale = EditorGUILayout.Vector3Field("Scale", _placementScale);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Renderer", EditorStyles.boldLabel);
        DrawSortingLayerPopup();
        _sortingOrder = EditorGUILayout.IntField("Order In Layer", _sortingOrder);
        _targetLayer = EditorGUILayout.LayerField("GameObject Layer", _targetLayer);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("批量调整", EditorStyles.boldLabel);
        _bulkOffsetDelta = EditorGUILayout.Vector3Field("Offset Delta", _bulkOffsetDelta);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用偏移到父物体下全部贴图"))
            {
                ApplyBulkOffset();
                _bulkOffsetDelta = Vector3.zero;
            }
            if (GUILayout.Button("应用缩放/层级/排序到全部"))
            {
                ApplyBulkSettings();
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_detectionEnabled) return;

        Event e = Event.current;
        if (e == null) return;

        if (!TryGetMouseWorldPoint(e.mousePosition, out Vector3 mouseWorld))
            return;

        Vector2 origin = new Vector2(mouseWorld.x, mouseWorld.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, _raycastDistance, _floorMask);
        bool hitSomething = hit.collider != null;

        DrawRayPreview(mouseWorld, hitSomething, hit);

        if (e.type == EventType.KeyDown && e.keyCode == _placeHotkey)
        {
            if (EditorGUIUtility.editingTextField || e.alt || e.control || e.command) return;

            if (hitSomething)
            {
                Vector3 hitPoint = new Vector3(hit.point.x, hit.point.y, _raycastPlaneZ);
                PlaceSprite(hitPoint);
                e.Use();
            }
        }

        sceneView.Repaint();
    }

    private void DrawRayPreview(Vector3 origin, bool hitSomething, RaycastHit2D hit)
    {
        Color lineColor = hitSomething ? new Color(0.2f, 1f, 0.2f, 0.9f) : new Color(1f, 0.25f, 0.25f, 0.9f);
        Handles.color = lineColor;

        Vector3 start = new Vector3(origin.x, origin.y, _raycastPlaneZ);
        Vector3 end = hitSomething
            ? new Vector3(hit.point.x, hit.point.y, _raycastPlaneZ)
            : start + Vector3.down * _raycastDistance;

        Handles.DrawLine(start, end);

        if (!hitSomething) return;

        Vector3 hitPoint = new Vector3(hit.point.x, hit.point.y, _raycastPlaneZ);
        float size = HandleUtility.GetHandleSize(hitPoint) * 0.12f;
        Vector3 hitNormal = new Vector3(hit.normal.x, hit.normal.y, 0f);
        Handles.DrawWireDisc(hitPoint, Vector3.forward, size);
        Handles.DrawLine(hitPoint, hitPoint + hitNormal * size * 1.2f);

        if (_sprite == null) return;

        Matrix4x4 oldMatrix = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(hitPoint + _placementOffset, Quaternion.identity, _placementScale);
        Handles.DrawWireCube(_sprite.bounds.center, _sprite.bounds.size);
        Handles.matrix = oldMatrix;
    }

    private void PlaceSprite(Vector3 hitPoint)
    {
        if (_sprite == null)
        {
            ShowNotification(new GUIContent("请先指定 Sprite"));
            return;
        }

        Transform parent = _parent != null ? _parent : GetOrCreateParent();

        var go = new GameObject(_sprite.name);
        Undo.RegisterCreatedObjectUndo(go, "Place Ground Sprite");
        go.transform.SetParent(parent, true);
        go.transform.position = hitPoint + _placementOffset;
        go.transform.localScale = _placementScale;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = _sprite;
        renderer.sortingLayerName = _sortingLayerName;
        renderer.sortingOrder = _sortingOrder;

        go.layer = _targetLayer;

        if (_autoSelectNew)
            Selection.activeGameObject = go;
    }

    private Transform GetOrCreateParent()
    {
        var existing = GameObject.Find(DefaultParentName);
        if (existing != null) return existing.transform;

        var go = new GameObject(DefaultParentName);
        Undo.RegisterCreatedObjectUndo(go, "Create Ground Sprite Parent");
        return go.transform;
    }

    private void ApplyBulkOffset()
    {
        if (_parent == null)
        {
            ShowNotification(new GUIContent("请先指定 Parent"));
            return;
        }

        if (_bulkOffsetDelta == Vector3.zero) return;

        var transforms = _parent.GetComponentsInChildren<Transform>(true);
        Undo.RecordObjects(transforms, "Apply Ground Sprite Offset");
        foreach (var t in transforms)
        {
            if (t == _parent) continue;
            t.position += _bulkOffsetDelta;
        }
    }

    private void ApplyBulkSettings()
    {
        if (_parent == null)
        {
            ShowNotification(new GUIContent("请先指定 Parent"));
            return;
        }

        var renderers = _parent.GetComponentsInChildren<SpriteRenderer>(true);
        var transforms = renderers.Select(r => r.transform).ToArray();
        var gameObjects = renderers.Select(r => r.gameObject).ToArray();

        if (renderers.Length == 0) return;

        Undo.RecordObjects(renderers, "Apply Ground Sprite Settings");
        Undo.RecordObjects(transforms, "Apply Ground Sprite Settings");
        Undo.RecordObjects(gameObjects, "Apply Ground Sprite Settings");

        foreach (var renderer in renderers)
        {
            renderer.sortingLayerName = _sortingLayerName;
            renderer.sortingOrder = _sortingOrder;
            renderer.transform.localScale = _placementScale;
            renderer.gameObject.layer = _targetLayer;
        }
    }

    private void RefreshSortingLayers()
    {
        _sortingLayerNames = SortingLayer.layers.Select(l => l.name).ToArray();
        if (_sortingLayerNames.Length == 0)
        {
            _sortingLayerNames = new[] { "Default" };
        }

        if (string.IsNullOrEmpty(_sortingLayerName) || !_sortingLayerNames.Contains(_sortingLayerName))
            _sortingLayerName = _sortingLayerNames[0];
    }

    private void DrawSortingLayerPopup()
    {
        if (_sortingLayerNames == null || _sortingLayerNames.Length == 0)
            RefreshSortingLayers();

        using (new EditorGUILayout.HorizontalScope())
        {
            int index = System.Array.IndexOf(_sortingLayerNames, _sortingLayerName);
            if (index < 0) index = 0;
            int newIndex = EditorGUILayout.Popup("Sorting Layer", index, _sortingLayerNames);
            _sortingLayerName = _sortingLayerNames[newIndex];

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
                RefreshSortingLayers();
        }
    }

    private static LayerMask LayerMaskField(string label, LayerMask selected)
    {
        var layers = InternalEditorUtility.layers;
        int[] layerNumbers = new int[layers.Length];
        int mask = 0;

        for (int i = 0; i < layers.Length; i++)
        {
            layerNumbers[i] = LayerMask.NameToLayer(layers[i]);
            if (((1 << layerNumbers[i]) & selected.value) != 0)
                mask |= 1 << i;
        }

        mask = EditorGUILayout.MaskField(label, mask, layers);

        int newMask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            if ((mask & (1 << i)) != 0)
                newMask |= 1 << layerNumbers[i];
        }

        selected.value = newMask;
        return selected;
    }

    private bool TryGetMouseWorldPoint(Vector2 guiPosition, out Vector3 worldPoint)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, _raycastPlaneZ));
        if (plane.Raycast(ray, out float enter) && enter >= 0f)
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = default;
        return false;
    }
}
