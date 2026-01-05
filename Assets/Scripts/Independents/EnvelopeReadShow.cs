using System;
using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// UI envelope show: moves from bottom to top at constant speed, toggles active state, and sends a sequencer message.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnvelopeReadShow : MonoBehaviour
    {
        private const string LOG_TAG = "[EnvelopeReadShow-UI] ";
        
        [Header("Envelope Target")]
        [Tooltip("The UI object to move.")]
        [SerializeField] private RectTransform envelopeRect;

        [Tooltip("Root object to activate/deactivate. Usually the envelope GameObject.")]
        [SerializeField] private GameObject envelopeRoot;

        [Header("Motion")]
        [Tooltip("Start position (anchoredPosition).")]
        [SerializeField] private Vector2 startAnchoredPosition = new Vector2(0f, -600f);

        [Tooltip("End position (anchoredPosition).")]
        [SerializeField] private Vector2 endAnchoredPosition = new Vector2(0f, 600f);

        [Tooltip("Move speed (UI units per second).")]
        [SerializeField] private float moveSpeed = 600f;

        [Tooltip("Use unscaled time for dialogue sequences.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Sequencer")]
        [Tooltip("If not empty, send this sequencer message when the show finishes.")]
        [SerializeField] private string completionMessage = "EnvelopeReadShowDone";

        public event Action Completed;

        public bool IsPlaying => _isPlaying;

        private Coroutine _playRoutine;
        private bool _isPlaying;
        private string _pendingMessageOverride;
        private int _updateCount;

        private void Awake()
        {
            Debug.Log($"{LOG_TAG}Awake() called on '{gameObject.name}'");
            ResolveReferences();
            DeactivateEnvelopeIfSafe();
            Debug.Log($"{LOG_TAG}Awake complete. envelopeRect: {(envelopeRect != null ? envelopeRect.name : "NULL")}, envelopeRoot: {(envelopeRoot != null ? envelopeRoot.name : "NULL")}");
        }

        private void OnEnable()
        {
            Debug.Log($"{LOG_TAG}OnEnable() called on '{gameObject.name}'");
        }

        private void OnDisable()
        {
            Debug.Log($"{LOG_TAG}OnDisable() called on '{gameObject.name}'. Stopping routine if any.");
            StopRoutine();
            SetEnvelopeActive(false);
        }

        public void Play()
        {
            Debug.Log($"{LOG_TAG}Play() called (no override)");
            TryPlay(null);
        }

        public void Play(string completionMessageOverride)
        {
            Debug.Log($"{LOG_TAG}Play('{completionMessageOverride}') called");
            TryPlay(completionMessageOverride);
        }

        public bool TryPlay(string completionMessageOverride)
        {
            Debug.Log($"{LOG_TAG}========== TryPlay CALLED ==========");
            Debug.Log($"{LOG_TAG}Time: {Time.time}, Frame: {Time.frameCount}");
            Debug.Log($"{LOG_TAG}completionMessageOverride: '{completionMessageOverride}'");
            Debug.Log($"{LOG_TAG}GameObject: '{gameObject.name}', enabled: {enabled}, isActiveAndEnabled: {isActiveAndEnabled}");
            
            ResolveReferences();

            Debug.Log($"{LOG_TAG}After ResolveReferences:");
            Debug.Log($"{LOG_TAG}  envelopeRect: {(envelopeRect != null ? envelopeRect.name : "NULL")}");
            Debug.Log($"{LOG_TAG}  envelopeRoot: {(envelopeRoot != null ? envelopeRoot.name : "NULL")}");

            if (envelopeRect == null)
            {
                Debug.LogError($"{LOG_TAG}ERROR: Envelope RectTransform not assigned! Returning false.");
                return false;
            }

            _pendingMessageOverride = completionMessageOverride;
            Debug.Log($"{LOG_TAG}Calling StartRoutine()...");
            StartRoutine();
            Debug.Log($"{LOG_TAG}TryPlay returning true");
            return true;
        }

        private void StartRoutine()
        {
            Debug.Log($"{LOG_TAG}StartRoutine() called");
            StopRoutine();

            if (envelopeRoot == null && envelopeRect != null)
            {
                envelopeRoot = envelopeRect.gameObject;
                Debug.Log($"{LOG_TAG}Set envelopeRoot to envelopeRect.gameObject: '{envelopeRoot.name}'");
            }

            if (envelopeRoot == gameObject)
            {
                Debug.LogWarning($"{LOG_TAG}WARNING: Envelope Root is the same object as this component. Use a child object to allow deactivation.");
            }

            Debug.Log($"{LOG_TAG}Setting envelope active and resetting position...");
            Debug.Log($"{LOG_TAG}  startAnchoredPosition: {startAnchoredPosition}");
            Debug.Log($"{LOG_TAG}  endAnchoredPosition: {endAnchoredPosition}");
            Debug.Log($"{LOG_TAG}  moveSpeed: {moveSpeed}");
            Debug.Log($"{LOG_TAG}  useUnscaledTime: {useUnscaledTime}");
            
            SetEnvelopeActive(true);
            
            Debug.Log($"{LOG_TAG}envelopeRoot.activeSelf after SetEnvelopeActive(true): {(envelopeRoot != null ? envelopeRoot.activeSelf.ToString() : "N/A")}");
            Debug.Log($"{LOG_TAG}envelopeRoot.activeInHierarchy: {(envelopeRoot != null ? envelopeRoot.activeInHierarchy.ToString() : "N/A")}");
            
            envelopeRect.anchoredPosition = startAnchoredPosition;
            Debug.Log($"{LOG_TAG}Set anchoredPosition to: {envelopeRect.anchoredPosition}");

            Debug.Log($"{LOG_TAG}Starting PlayRoutine coroutine...");
            _updateCount = 0;
            _playRoutine = StartCoroutine(PlayRoutine());
            Debug.Log($"{LOG_TAG}_playRoutine is null: {_playRoutine == null}");
        }

        private IEnumerator PlayRoutine()
        {
            Debug.Log($"{LOG_TAG}========== PlayRoutine COROUTINE STARTED ==========");
            Debug.Log($"{LOG_TAG}Time: {Time.time}, Frame: {Time.frameCount}");
            
            _isPlaying = true;

            float speed = Mathf.Max(0.01f, moveSpeed);
            Vector2 target = endAnchoredPosition;
            float totalDistance = Vector2.Distance(startAnchoredPosition, target);
            float estimatedTime = totalDistance / speed;

            Debug.Log($"{LOG_TAG}Coroutine params:");
            Debug.Log($"{LOG_TAG}  speed: {speed}");
            Debug.Log($"{LOG_TAG}  target: {target}");
            Debug.Log($"{LOG_TAG}  totalDistance: {totalDistance}");
            Debug.Log($"{LOG_TAG}  estimatedTime: {estimatedTime}s");
            Debug.Log($"{LOG_TAG}  Time.timeScale: {Time.timeScale}");

            float startTime = useUnscaledTime ? Time.unscaledTime : Time.time;
            Vector2 startPos = envelopeRect.anchoredPosition;

            while (Vector2.Distance(envelopeRect.anchoredPosition, target) > 0.01f)
            {
                float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                Vector2 oldPos = envelopeRect.anchoredPosition;
                envelopeRect.anchoredPosition = Vector2.MoveTowards(envelopeRect.anchoredPosition, target, speed * delta);
                
                _updateCount++;
                
                // Log every 60 frames to avoid spam
                if (_updateCount % 60 == 1)
                {
                    float elapsed = (useUnscaledTime ? Time.unscaledTime : Time.time) - startTime;
                    float progress = 1f - (Vector2.Distance(envelopeRect.anchoredPosition, target) / totalDistance);
                    Debug.Log($"{LOG_TAG}[Frame {_updateCount}] Progress: {progress:P1}, Pos: {envelopeRect.anchoredPosition}, Delta: {delta:F4}, Elapsed: {elapsed:F2}s");
                }
                
                yield return null;
            }

            float totalElapsed = (useUnscaledTime ? Time.unscaledTime : Time.time) - startTime;
            Debug.Log($"{LOG_TAG}========== PlayRoutine LOOP FINISHED ==========");
            Debug.Log($"{LOG_TAG}Total updates: {_updateCount}, Total time: {totalElapsed:F2}s");

            envelopeRect.anchoredPosition = target;
            Debug.Log($"{LOG_TAG}Final position set to: {envelopeRect.anchoredPosition}");
            
            _isPlaying = false;
            _playRoutine = null;

            Debug.Log($"{LOG_TAG}Setting envelope inactive...");
            SetEnvelopeActive(false);
            
            Debug.Log($"{LOG_TAG}Invoking Completed event...");
            int subscriberCount = Completed?.GetInvocationList()?.Length ?? 0;
            Debug.Log($"{LOG_TAG}Completed event has {subscriberCount} subscriber(s)");
            Completed?.Invoke();
            Debug.Log($"{LOG_TAG}Completed event invoked");

            string message = _pendingMessageOverride ?? completionMessage;
            _pendingMessageOverride = null;

            Debug.Log($"{LOG_TAG}Sequencer message to send: '{message}'");
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"{LOG_TAG}Calling Sequencer.Message('{message}')...");
                Sequencer.Message(message);
                Debug.Log($"{LOG_TAG}Sequencer.Message called successfully");
            }
            else
            {
                Debug.Log($"{LOG_TAG}No sequencer message to send (empty string)");
            }

            Debug.Log($"{LOG_TAG}========== PlayRoutine COROUTINE COMPLETE ==========");
        }

        private void StopRoutine()
        {
            if (_playRoutine != null)
            {
                Debug.Log($"{LOG_TAG}StopRoutine: Stopping existing coroutine after {_updateCount} updates");
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _isPlaying = false;
        }

        private void ResolveReferences()
        {
            bool changed = false;
            
            if (envelopeRect == null && envelopeRoot != null)
            {
                envelopeRect = envelopeRoot.GetComponent<RectTransform>();
                if (envelopeRect != null) changed = true;
            }

            if (envelopeRect == null)
            {
                envelopeRect = GetComponent<RectTransform>();
                if (envelopeRect != null) changed = true;
            }

            if (envelopeRoot == null && envelopeRect != null)
            {
                envelopeRoot = envelopeRect.gameObject;
                changed = true;
            }
            
            if (changed)
            {
                Debug.Log($"{LOG_TAG}ResolveReferences: References were resolved dynamically");
            }
        }

        private void SetEnvelopeActive(bool active)
        {
            Debug.Log($"{LOG_TAG}SetEnvelopeActive({active}) called");
            
            if (envelopeRoot == null)
            {
                Debug.Log($"{LOG_TAG}SetEnvelopeActive: envelopeRoot is null, skipping");
                return;
            }
            
            if (envelopeRoot == gameObject && !active)
            {
                Debug.Log($"{LOG_TAG}SetEnvelopeActive: Cannot deactivate self (envelopeRoot == this.gameObject), skipping");
                return;
            }

            Debug.Log($"{LOG_TAG}SetEnvelopeActive: Setting '{envelopeRoot.name}' active = {active}");
            envelopeRoot.SetActive(active);
        }

        private void DeactivateEnvelopeIfSafe()
        {
            if (envelopeRoot == null)
            {
                Debug.Log($"{LOG_TAG}DeactivateEnvelopeIfSafe: envelopeRoot is null");
                return;
            }
            if (envelopeRoot == gameObject)
            {
                Debug.Log($"{LOG_TAG}DeactivateEnvelopeIfSafe: Cannot deactivate self");
                return;
            }

            Debug.Log($"{LOG_TAG}DeactivateEnvelopeIfSafe: Deactivating '{envelopeRoot.name}'");
            envelopeRoot.SetActive(false);
        }

        private void OnValidate()
        {
            if (moveSpeed < 0.01f) moveSpeed = 0.01f;
        }
    }
}
