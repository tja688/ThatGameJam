using System;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.UI.Mock
{
    public class MockAudioSettingsService : IAudioSettingsService
    {
        public event Action<float> OnBgmChanged;
        public event Action<float> OnSfxChanged;
        public event Action<float> OnUiChanged;

        private float _bgm = 0.5f;
        private float _sfx = 0.6f;
        private float _ui = 0.4f;

        public void SetBgmVolume(float v01)
        {
            _bgm = Mathf.Clamp01(v01);
            OnBgmChanged?.Invoke(_bgm);
        }

        public void SetSfxVolume(float v01)
        {
            _sfx = Mathf.Clamp01(v01);
            OnSfxChanged?.Invoke(_sfx);
        }

        public void SetUiVolume(float v01)
        {
            _ui = Mathf.Clamp01(v01);
            OnUiChanged?.Invoke(_ui);
        }

        public float GetBgmVolume() => _bgm;
        public float GetSfxVolume() => _sfx;
        public float GetUiVolume() => _ui;
    }
}
