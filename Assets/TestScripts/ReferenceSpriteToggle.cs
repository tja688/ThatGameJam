using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThatGameJam.Test
{
    [ExecuteAlways]
    public class ReferenceSpriteToggle : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private SpriteRenderer targetRenderer;

        [Header("Keys")]
        [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
        [SerializeField] private KeyCode enableToggleKey = KeyCode.Space;

        [Header("Sorting A (Default)")]
        [SerializeField] private string sortingLayerA = "Default";
        [SerializeField] private int sortingOrderA = -100;

        [Header("Sorting B (UI)")]
        [SerializeField] private string sortingLayerB = "UI";
        [SerializeField] private int sortingOrderB = 100;

        [Header("Input")]
        [Min(0f)]
        [SerializeField] private float inputCooldown = 0.2f;

        private double _nextAllowedTime;
#if UNITY_EDITOR
        private bool _editorToggleHeld;
        private bool _editorSpaceHeld;
#endif

        private void Reset()
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (Application.isPlaying || targetRenderer == null)
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type != EventType.KeyDown && currentEvent.type != EventType.KeyUp)
            {
                return;
            }

            UpdateKeyHeldState(currentEvent);

            if (IsCooldownActive() || currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (currentEvent.keyCode == enableToggleKey && _editorToggleHeld)
            {
                ToggleRendererEnabled();
                StartCooldown();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == toggleKey)
            {
                if (_editorSpaceHeld)
                {
                    ToggleRendererEnabled();
                }
                else
                {
                    ToggleSorting();
                }

                StartCooldown();
                currentEvent.Use();
            }
        }

        private void UpdateKeyHeldState(Event currentEvent)
        {
            if (currentEvent.keyCode == toggleKey)
            {
                _editorToggleHeld = currentEvent.type == EventType.KeyDown;
            }
            else if (currentEvent.keyCode == enableToggleKey)
            {
                _editorSpaceHeld = currentEvent.type == EventType.KeyDown;
            }
        }
#endif

        private void Update()
        {
            if (targetRenderer == null || !Application.isPlaying)
            {
                return;
            }

            if (IsCooldownActive())
            {
                return;
            }

            if (Input.GetKey(toggleKey) && Input.GetKeyDown(enableToggleKey))
            {
                ToggleRendererEnabled();
                StartCooldown();
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                ToggleSorting();
                StartCooldown();
            }
        }

        private double GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorApplication.timeSinceStartup;
            }
#endif
            return Time.unscaledTime;
        }

        private bool IsCooldownActive()
        {
            return GetCurrentTime() < _nextAllowedTime;
        }

        private void StartCooldown()
        {
            _nextAllowedTime = GetCurrentTime() + inputCooldown;
        }

        private void ToggleRendererEnabled()
        {
            RecordUndo("Toggle Reference Sprite");
            targetRenderer.enabled = !targetRenderer.enabled;
            MarkDirty();
        }

        private void ToggleSorting()
        {
            if (IsStateA())
            {
                ApplySorting(sortingLayerB, sortingOrderB);
            }
            else
            {
                ApplySorting(sortingLayerA, sortingOrderA);
            }
        }

        private bool IsStateA()
        {
            return targetRenderer.sortingLayerName == sortingLayerA
                && targetRenderer.sortingOrder == sortingOrderA;
        }

        private void ApplySorting(string layerName, int order)
        {
            RecordUndo("Toggle Reference Sprite Sorting");
            if (!string.IsNullOrEmpty(layerName))
            {
                targetRenderer.sortingLayerName = layerName;
            }

            targetRenderer.sortingOrder = order;
            MarkDirty();
        }

        private void RecordUndo(string actionName)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(targetRenderer, actionName);
            }
#endif
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(targetRenderer);
                SceneView.RepaintAll();
            }
#endif
        }
    }
}
