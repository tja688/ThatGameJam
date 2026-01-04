using System;
using System.Collections;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// UI信封演出：从下到上匀速移动，播放期间激活，结束后失活，并发送Sequencer消息。
    /// </summary>
    [DisallowMultipleComponent]
    public class EnvelopeReadShow : MonoBehaviour
    {
        [Header("Envelope Target")]
        [Tooltip("需要移动的UI对象。")]
        [SerializeField] private RectTransform envelopeRect;

        [Tooltip("用于激活/失活的根对象。通常是信封的GameObject。")]
        [SerializeField] private GameObject envelopeRoot;

        [Header("Motion")]
        [Tooltip("入场起点（anchoredPosition）。")]
        [SerializeField] private Vector2 startAnchoredPosition = new Vector2(0f, -600f);

        [Tooltip("出场终点（anchoredPosition）。")]
        [SerializeField] private Vector2 endAnchoredPosition = new Vector2(0f, 600f);

        [Tooltip("移动配速（UI单位/秒）。")]
        [SerializeField] private float moveSpeed = 600f;

        [Tooltip("对话期间推荐使用不受时间缩放影响的时间。")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Sequencer")]
        [Tooltip("非空时，演出完成后发送该Sequencer消息。")]
        [SerializeField] private string completionMessage = "EnvelopeReadShowDone";

        public event Action Completed;

        public bool IsPlaying => _isPlaying;

        private Coroutine _playRoutine;
        private bool _isPlaying;
        private string _pendingMessageOverride;

        private void Awake()
        {
            ResolveReferences();
            DeactivateEnvelopeIfSafe();
        }

        private void OnDisable()
        {
            StopRoutine();
            SetEnvelopeActive(false);
        }

        public void Play()
        {
            TryPlay(null);
        }

        public void Play(string completionMessageOverride)
        {
            TryPlay(completionMessageOverride);
        }

        public bool TryPlay(string completionMessageOverride)
        {
            ResolveReferences();

            if (envelopeRect == null)
            {
                Debug.LogWarning("EnvelopeReadShow: Envelope RectTransform not assigned.", this);
                return false;
            }

            _pendingMessageOverride = completionMessageOverride;
            StartRoutine();
            return true;
        }

        private void StartRoutine()
        {
            StopRoutine();

            if (envelopeRoot == null && envelopeRect != null)
            {
                envelopeRoot = envelopeRect.gameObject;
            }

            if (envelopeRoot == gameObject)
            {
                Debug.LogWarning("EnvelopeReadShow: Envelope Root is the same object as this component. Use a child object to allow deactivation.", this);
            }

            SetEnvelopeActive(true);
            envelopeRect.anchoredPosition = startAnchoredPosition;

            _playRoutine = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            _isPlaying = true;

            float speed = Mathf.Max(0.01f, moveSpeed);
            Vector2 target = endAnchoredPosition;

            while (Vector2.Distance(envelopeRect.anchoredPosition, target) > 0.01f)
            {
                float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                envelopeRect.anchoredPosition = Vector2.MoveTowards(envelopeRect.anchoredPosition, target, speed * delta);
                yield return null;
            }

            envelopeRect.anchoredPosition = target;
            _isPlaying = false;
            _playRoutine = null;

            SetEnvelopeActive(false);
            Completed?.Invoke();

            string message = _pendingMessageOverride ?? completionMessage;
            _pendingMessageOverride = null;

            if (!string.IsNullOrEmpty(message))
            {
                Sequencer.Message(message);
            }
        }

        private void StopRoutine()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            _isPlaying = false;
        }

        private void ResolveReferences()
        {
            if (envelopeRect == null && envelopeRoot != null)
            {
                envelopeRect = envelopeRoot.GetComponent<RectTransform>();
            }

            if (envelopeRect == null)
            {
                envelopeRect = GetComponent<RectTransform>();
            }
        }

        private void SetEnvelopeActive(bool active)
        {
            if (envelopeRoot == null) return;
            if (envelopeRoot == gameObject && !active) return;

            envelopeRoot.SetActive(active);
        }

        private void DeactivateEnvelopeIfSafe()
        {
            if (envelopeRoot == null) return;
            if (envelopeRoot == gameObject) return;

            envelopeRoot.SetActive(false);
        }

        private void OnValidate()
        {
            if (moveSpeed < 0.01f) moveSpeed = 0.01f;
        }
    }
}
