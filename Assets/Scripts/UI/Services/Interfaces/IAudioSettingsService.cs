using System;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface IAudioSettingsService
    {
        event Action<float> OnBgmChanged;
        event Action<float> OnSfxChanged;
        event Action<float> OnUiChanged;

        void SetBgmVolume(float v01);
        void SetSfxVolume(float v01);
        void SetUiVolume(float v01);

        float GetBgmVolume();
        float GetSfxVolume();
        float GetUiVolume();
    }
}
