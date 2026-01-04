using System;
using QFramework;
using ThatGameJam.Features.AreaSystem.Models;
using ThatGameJam.Features.LightVitality.Models;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.Features.PlayerStatus.Controllers
{
    public class PlayerStatusUIProvider : MonoBehaviour, IController, IPlayerStatusProvider
    {
        public event Action<PlayerPanelData> OnChanged;

        private PlayerPanelData _data;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            UIServiceRegistry.SetPlayerStatus(this);

            var areaModel = this.GetModel<IAreaModel>();
            areaModel.CurrentAreaId.RegisterWithInitValue(OnAreaChanged)
                .UnRegisterWhenDisabled(gameObject);

            var lightModel = this.GetModel<ILightVitalityModel>();
            lightModel.CurrentLight.RegisterWithInitValue(OnLightChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnDisable()
        {
            if (UIServiceRegistry.PlayerStatus == this)
            {
                UIServiceRegistry.SetPlayerStatus(null);
            }
        }

        public PlayerPanelData GetData() => _data;

        private void OnAreaChanged(string areaId)
        {
            var nextArea = areaId ?? string.Empty;
            if (_data.AreaName == nextArea)
            {
                return;
            }

            _data.AreaName = nextArea;
            NotifyChanged();
        }

        private void OnLightChanged(float value)
        {
            if (Mathf.Approximately(_data.LightValue, value))
            {
                return;
            }

            _data.LightValue = value;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnChanged?.Invoke(_data);
        }
    }
}
