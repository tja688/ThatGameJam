using System;
using ThatGameJam.UI.Services.Interfaces;

namespace ThatGameJam.Independents.Audio
{
    public class AudioSettingsService : IAudioSettingsService
    {
        public event Action<float> OnBgmChanged;
        public event Action<float> OnSfxChanged;
        public event Action<float> OnUiChanged;

        private readonly AudioService _audioService;

        public AudioSettingsService(AudioService audioService)
        {
            _audioService = audioService;
        }

        public void SetBgmVolume(float v01)
        {
            float value = _audioService.SetBusVolume(AudioBus.Music, v01);
            OnBgmChanged?.Invoke(value);
        }

        public void SetSfxVolume(float v01)
        {
            float value = _audioService.SetBusVolume(AudioBus.SFX, v01);
            OnSfxChanged?.Invoke(value);
        }

        public void SetUiVolume(float v01)
        {
            float value = _audioService.SetBusVolume(AudioBus.UI, v01);
            OnUiChanged?.Invoke(value);
        }

        public float GetBgmVolume() => _audioService.GetBusVolume(AudioBus.Music);
        public float GetSfxVolume() => _audioService.GetBusVolume(AudioBus.SFX);
        public float GetUiVolume() => _audioService.GetBusVolume(AudioBus.UI);
    }
}
