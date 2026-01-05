using PixelCrushers.DialogueSystem;
using ThatGameJam.Independents.Audio;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThatGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class UIRoot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIRouter router;
        [SerializeField] private UIPauseService pauseService;

        [Header("Styles")]
        [SerializeField] private StyleSheet themeStyle;
        [SerializeField] private StyleSheet componentsStyle;
        [SerializeField] private StyleSheet menusStyle;

        [Header("UXML Assets")]
        [SerializeField] private UIPanelAssets panelAssets;

        [Header("Startup")]
        [SerializeField] private bool showMainMenuOnStart = true;

        [Header("Main Menu State")]
        [SerializeField] private bool stopDialogueOnMainMenu = true;
        [SerializeField] private bool muteGameplaySfxOnMainMenu = true;
        [SerializeField] private bool muteAmbientOnMainMenu = true;

        private IAudioSettingsService _audioSettings;
        private bool _mainMenuVisible;
        private float _desiredSfxVolume = 1f;
        private float _desiredAmbientVolume = 1f;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (router == null)
            {
                router = GetComponent<UIRouter>();
            }

            if (pauseService == null)
            {
                pauseService = GetComponent<UIPauseService>();
            }

            if (panelAssets == null)
            {
                panelAssets = new UIPanelAssets();
                Debug.LogWarning("UIRoot panel assets are not assigned. UI will render empty until set.");
            }

            if (router != null)
            {
                router.Initialize(uiDocument, panelAssets, new[] { themeStyle, componentsStyle, menusStyle }, pauseService);
            }
        }

        private void OnEnable()
        {
            BindMainMenuEvents();
            BindAudioSettings();
        }

        private void OnDisable()
        {
            UnbindMainMenuEvents();
            UnbindAudioSettings();
        }

        private void Start()
        {
            if (showMainMenuOnStart && router != null)
            {
                router.OpenMainMenu();
            }
        }

        public void OpenPlayerPanel()
        {
            router?.OpenPlayerPanel();
        }

        public void OpenInstructionsPanel()
        {
            router?.OpenInstructionsPanel();
        }

        public void OpenSettingsFromGame()
        {
            router?.OpenSettings(SettingsOpenedFrom.InGame);
        }

        private void BindMainMenuEvents()
        {
            if (router == null)
            {
                return;
            }

            router.MainMenuVisibilityChanged -= HandleMainMenuVisibilityChanged;
            router.MainMenuVisibilityChanged += HandleMainMenuVisibilityChanged;
            HandleMainMenuVisibilityChanged(router.IsMainMenuOnTop);
        }

        private void UnbindMainMenuEvents()
        {
            if (router == null)
            {
                return;
            }

            router.MainMenuVisibilityChanged -= HandleMainMenuVisibilityChanged;
        }

        private void BindAudioSettings()
        {
            _audioSettings = UIServiceRegistry.AudioSettings;
            if (_audioSettings == null)
            {
                return;
            }

            _audioSettings.OnSfxChanged -= HandleSfxChanged;
            _audioSettings.OnSfxChanged += HandleSfxChanged;
        }

        private void UnbindAudioSettings()
        {
            if (_audioSettings == null)
            {
                return;
            }

            _audioSettings.OnSfxChanged -= HandleSfxChanged;
            _audioSettings = null;
        }

        private void HandleMainMenuVisibilityChanged(bool isVisible)
        {
            _mainMenuVisible = isVisible;
            if (_audioSettings == null)
            {
                BindAudioSettings();
            }

            if (stopDialogueOnMainMenu && isVisible)
            {
                DialogueManager.StopAllConversations();
            }

            if (!muteGameplaySfxOnMainMenu)
            {
                return;
            }

            if (isVisible)
            {
                CacheDesiredVolumes();
                SetBusVolume(AudioBus.SFX, 0f);
                if (muteAmbientOnMainMenu)
                {
                    SetBusVolume(AudioBus.Ambient, 0f);
                }
            }
            else
            {
                RestoreDesiredVolumes();
            }
        }

        private void HandleSfxChanged(float value)
        {
            _desiredSfxVolume = value;
            if (_mainMenuVisible && muteGameplaySfxOnMainMenu)
            {
                SetBusVolume(AudioBus.SFX, 0f);
            }
        }

        private void CacheDesiredVolumes()
        {
            var audioService = AudioService.Instance;
            if (audioService == null)
            {
                return;
            }

            _desiredSfxVolume = audioService.GetBusVolume(AudioBus.SFX);
            _desiredAmbientVolume = audioService.GetBusVolume(AudioBus.Ambient);
        }

        private void RestoreDesiredVolumes()
        {
            SetBusVolume(AudioBus.SFX, _desiredSfxVolume);
            if (muteAmbientOnMainMenu)
            {
                SetBusVolume(AudioBus.Ambient, _desiredAmbientVolume);
            }
        }

        private static void SetBusVolume(AudioBus bus, float value)
        {
            var audioService = AudioService.Instance;
            if (audioService == null)
            {
                return;
            }

            audioService.SetBusVolume(bus, value);
        }
    }
}
