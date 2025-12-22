using QFramework;
using ThatGameJam.Features.Darkness.Models;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.Features.RunFailReset.Models;
using ThatGameJam.Features.SafeZone.Models;
using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam.Features.HUD.Controllers
{
    public class HUDController : MonoBehaviour, IController
    {
        [Header("Light")]
        [SerializeField] private Text lightText;
        [SerializeField] private Image lightFill;

        [Header("States")]
        [SerializeField] private Text darknessText;
        [SerializeField] private Text safeZoneText;

        [Header("Lamps")]
        [SerializeField] private Text lampText;

        [Header("Fail")]
        [SerializeField] private Text failText;

        private float _currentLight;
        private float _maxLight;
        private int _lampCount;
        private int _lampMax;
        private int _safeZoneCount;
        private bool _isSafe;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            var lightModel = this.GetModel<ILightVitalityModel>();
            lightModel.CurrentLight.RegisterWithInitValue(OnCurrentLightChanged)
                .UnRegisterWhenDisabled(gameObject);
            lightModel.MaxLight.RegisterWithInitValue(OnMaxLightChanged)
                .UnRegisterWhenDisabled(gameObject);

            var darknessModel = this.GetModel<IDarknessModel>();
            darknessModel.IsInDarkness.RegisterWithInitValue(OnDarknessChanged)
                .UnRegisterWhenDisabled(gameObject);

            var safeZoneModel = this.GetModel<ISafeZoneModel>();
            safeZoneModel.IsSafe.RegisterWithInitValue(OnSafeChanged)
                .UnRegisterWhenDisabled(gameObject);
            safeZoneModel.SafeZoneCount.RegisterWithInitValue(OnSafeCountChanged)
                .UnRegisterWhenDisabled(gameObject);

            var lampModel = this.GetModel<IKeroseneLampModel>();
            lampModel.LampCount.RegisterWithInitValue(OnLampCountChanged)
                .UnRegisterWhenDisabled(gameObject);
            lampModel.LampMax.RegisterWithInitValue(OnLampMaxChanged)
                .UnRegisterWhenDisabled(gameObject);

            var runModel = this.GetModel<IRunFailResetModel>();
            runModel.IsFailed.RegisterWithInitValue(OnRunFailedChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnCurrentLightChanged(float value)
        {
            _currentLight = value;
            RefreshLightUI();
        }

        private void OnMaxLightChanged(float value)
        {
            _maxLight = value;
            RefreshLightUI();
        }

        private void RefreshLightUI()
        {
            if (lightText != null)
            {
                lightText.text = $"Light: {_currentLight:0.#}/{_maxLight:0.#}";
            }

            if (lightFill != null)
            {
                var percent = _maxLight > 0f ? Mathf.Clamp01(_currentLight / _maxLight) : 0f;
                lightFill.fillAmount = percent;
            }
        }

        private void OnDarknessChanged(bool isInDarkness)
        {
            if (darknessText != null)
            {
                darknessText.text = isInDarkness ? "Darkness: IN" : "Darkness: OUT";
            }
        }

        private void OnSafeChanged(bool isSafe)
        {
            _isSafe = isSafe;
            RefreshSafeUI();
        }

        private void OnSafeCountChanged(int count)
        {
            _safeZoneCount = count;
            RefreshSafeUI();
        }

        private void RefreshSafeUI()
        {
            if (safeZoneText == null)
            {
                return;
            }

            var state = _isSafe ? "Safe: YES" : "Safe: NO";
            var suffix = _safeZoneCount > 0 ? $" ({_safeZoneCount})" : string.Empty;
            safeZoneText.text = state + suffix;
        }

        private void OnLampCountChanged(int count)
        {
            _lampCount = count;
            RefreshLampUI();
        }

        private void OnLampMaxChanged(int max)
        {
            _lampMax = max;
            RefreshLampUI();
        }

        private void RefreshLampUI()
        {
            if (lampText != null)
            {
                lampText.text = $"Lamps: {_lampCount}/{_lampMax}";
            }
        }

        private void OnRunFailedChanged(bool isFailed)
        {
            if (failText != null)
            {
                failText.gameObject.SetActive(isFailed);
                failText.text = isFailed ? "RUN FAILED" : string.Empty;
            }
        }
    }
}
