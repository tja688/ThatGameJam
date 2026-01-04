using System;
using System.Globalization;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: CanvasGroupFade(targetAlpha, duration, gameObjectName)
    /// Alternate: CanvasGroupFade(gameObjectName, targetAlpha, duration)
    /// </summary>
    public class SequencerCommandCanvasGroupFade : SequencerCommand
    {
        private const string DefaultCompletionMessage = "CanvasGroupFadeDone";

        private CanvasGroup _canvasGroup;
        private float _startAlpha;
        private float _targetAlpha;
        private float _duration;
        private float _startTime;
        private float _endTime;
        private bool _completed;

        public void Awake()
        {
            string param0 = GetParameter(0);
            string param1 = GetParameter(1);
            string param2 = GetParameter(2);

            string objectName;
            if (TryParseFloat(param0, out float targetAlpha))
            {
                _targetAlpha = Mathf.Clamp01(targetAlpha);
                _duration = Mathf.Max(0f, GetParameterAsFloat(1, 0f));
                objectName = param2;
            }
            else
            {
                objectName = param0;
                _targetAlpha = Mathf.Clamp01(GetParameterAsFloat(1, 1f));
                _duration = Mathf.Max(0f, GetParameterAsFloat(2, 0f));
            }

            if (string.IsNullOrEmpty(objectName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandCanvasGroupFade: No object name provided.");
                Stop();
                return;
            }

            GameObject target = ResolveTarget(objectName);
            if (target == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandCanvasGroupFade: Could not find object named '{objectName}'.");
                Stop();
                return;
            }

            _canvasGroup = target.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = target.GetComponentInChildren<CanvasGroup>(true);
            }

            if (_canvasGroup == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandCanvasGroupFade: No CanvasGroup found on '{target.name}'.");
                Stop();
                return;
            }

            _startAlpha = _canvasGroup.alpha;

            if (_duration <= 0f)
            {
                _canvasGroup.alpha = _targetAlpha;
                Complete();
                return;
            }

            _startTime = DialogueTime.time;
            _endTime = _startTime + _duration;
        }

        public void Update()
        {
            if (_completed || _canvasGroup == null)
            {
                return;
            }

            if (DialogueTime.time < _endTime && _duration > 0f)
            {
                float t = Mathf.Clamp01((DialogueTime.time - _startTime) / _duration);
                _canvasGroup.alpha = Mathf.Lerp(_startAlpha, _targetAlpha, t);
                return;
            }

            _canvasGroup.alpha = _targetAlpha;
            Complete();
        }

        private void Complete()
        {
            if (_completed) return;
            _completed = true;

            if (string.IsNullOrEmpty(endMessage))
            {
                Sequencer.Message(DefaultCompletionMessage);
            }

            Stop();
        }

        private GameObject ResolveTarget(string objectName)
        {
            if (string.Equals(objectName, "speaker", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(objectName, "listener", StringComparison.OrdinalIgnoreCase))
            {
                Transform subject = GetSubject(objectName);
                if (subject != null) return subject.gameObject;
            }

            return FindGameObjectIncludingInactive(objectName);
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private GameObject FindGameObjectIncludingInactive(string name)
        {
            GameObject activeObj = GameObject.Find(name);
            if (activeObj != null) return activeObj;

            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                GameObject result = FindRecursive(root.transform, name);
                if (result != null) return result;
            }

            return null;
        }

        private GameObject FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject result = FindRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }

            return null;
        }
    }
}
