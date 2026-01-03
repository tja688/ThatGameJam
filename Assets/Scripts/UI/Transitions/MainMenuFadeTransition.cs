using System.Collections;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace ThatGameJam.UI.Transitions
{
    public interface ICanvasAlphaController
    {
        float Alpha { get; set; }
    }

    public class MainMenuFadeTransition : MonoBehaviour
    {
        [Header("Countdown")]
        [SerializeField] private int countdownSeconds = 3;
        [SerializeField] private UnityEvent<int> onCountdownTick;

        [Header("Fade")]
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float startAlpha = 0f;
        [SerializeField] private float blackAlpha = 1f;

        [Header("Alpha Target")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private MonoBehaviour alphaController;

        [Header("Main Menu")]
        [SerializeField] private MonoBehaviour gameFlowOverride;

        private ICanvasAlphaController _alphaController;
        private IGameFlowService _gameFlowOverride;
        private Coroutine _routine;

        private void Awake()
        {
            ResolveTargets();
            SetAlpha(startAlpha);
        }

        private void OnEnable()
        {
            ResolveTargets();

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(RunSequence());
        }

        private void OnDisable()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private void ResolveTargets()
        {
            _alphaController = alphaController as ICanvasAlphaController;
            _gameFlowOverride = gameFlowOverride as IGameFlowService;

            if (alphaController != null && _alphaController == null)
            {
                Debug.LogWarning("MainMenuFadeTransition alphaController does not implement ICanvasAlphaController.");
            }

            if (gameFlowOverride != null && _gameFlowOverride == null)
            {
                Debug.LogWarning("MainMenuFadeTransition gameFlowOverride does not implement IGameFlowService.");
            }
        }

        private IEnumerator RunSequence()
        {
            SetAlpha(startAlpha);

            if (countdownSeconds > 0)
            {
                for (int remaining = countdownSeconds; remaining > 0; remaining--)
                {
                    if (onCountdownTick != null)
                    {
                        onCountdownTick.Invoke(remaining);
                    }

                    yield return new WaitForSecondsRealtime(1f);
                }
            }

            if (onCountdownTick != null)
            {
                onCountdownTick.Invoke(0);
            }

            yield return FadeTo(blackAlpha, fadeOutDuration);
            SwitchToMainMenu();
            yield return FadeTo(startAlpha, fadeInDuration);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float from = GetAlpha();
            if (duration <= 0f)
            {
                SetAlpha(target);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(from, target, t));
                yield return null;
            }

            SetAlpha(target);
        }

        private void SwitchToMainMenu()
        {
            var service = _gameFlowOverride ?? UIServiceRegistry.GameFlow;
            if (service != null)
            {
                service.ReturnToMainMenu();
                return;
            }

            MainMenuSceneLoader.ReturnToMainMenu();
        }

        private float GetAlpha()
        {
            if (_alphaController != null)
            {
                return _alphaController.Alpha;
            }

            if (canvasGroup != null)
            {
                return canvasGroup.alpha;
            }

            return 0f;
        }

        private void SetAlpha(float value)
        {
            if (_alphaController != null)
            {
                _alphaController.Alpha = value;
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = value;
            }
        }
    }
}
