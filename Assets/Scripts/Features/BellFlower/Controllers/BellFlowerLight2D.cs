using QFramework;
using ThatGameJam.Features.BellFlower.Events;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ThatGameJam.Features.BellFlower.Controllers
{
    public class BellFlowerLight2D : MonoBehaviour, IController
    {
        [SerializeField] private string doorId;
        [SerializeField] private string flowerId;
        [SerializeField] private Light2D targetLight;
        [SerializeField] private float activeIntensity = 1f;
        [SerializeField] private float fadeDuration = 0.35f;

        private float _fadeElapsed;
        private float _fadeFrom;
        private float _fadeTo;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponentInChildren<Light2D>();
            }
        }

        private void OnEnable()
        {
            if (targetLight != null)
            {
                targetLight.intensity = 0f;
            }

            ResetFade(0f);

            this.RegisterEvent<FlowerActivatedEvent>(OnFlowerActivated)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            if (fadeDuration <= 0f)
            {
                targetLight.intensity = _fadeTo;
                return;
            }

            if (Mathf.Approximately(targetLight.intensity, _fadeTo))
            {
                return;
            }

            _fadeElapsed = Mathf.Min(_fadeElapsed + Time.deltaTime, fadeDuration);
            var t = Mathf.Clamp01(_fadeElapsed / fadeDuration);
            targetLight.intensity = Mathf.Lerp(_fadeFrom, _fadeTo, t);
        }

        private void OnFlowerActivated(FlowerActivatedEvent e)
        {
            if (!IsEventForThisFlower(e))
            {
                return;
            }

            var targetIntensity = e.IsActive ? activeIntensity : 0f;
            StartFade(targetIntensity);
        }

        private bool IsEventForThisFlower(FlowerActivatedEvent e)
        {
            if (string.IsNullOrEmpty(doorId) && string.IsNullOrEmpty(flowerId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(doorId) && e.DoorId != doorId)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(flowerId) && e.FlowerId != flowerId)
            {
                return false;
            }

            return true;
        }

        private void StartFade(float targetIntensity)
        {
            if (targetLight == null)
            {
                return;
            }

            _fadeFrom = targetLight.intensity;
            _fadeTo = Mathf.Max(0f, targetIntensity);
            _fadeElapsed = 0f;

            if (fadeDuration <= 0f)
            {
                targetLight.intensity = _fadeTo;
            }
        }

        private void ResetFade(float targetIntensity)
        {
            _fadeFrom = targetIntensity;
            _fadeTo = targetIntensity;
            _fadeElapsed = fadeDuration;
        }
    }
}
