using ThatGameJam.UI.Mock;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThatGameJam.UI.Panels
{
    public class SettingsPanel : UIPanel
    {
        private readonly UIRouter _router;
        private readonly UIPanelAssets _assets;
        private readonly SettingsOpenedFrom _openedFrom;

        private Button _audioTabButton;
        private Button _saveTabButton;
        private Button _gameTabButton;

        private VisualElement _contentContainer;
        private VisualElement _audioPage;
        private VisualElement _savePage;
        private VisualElement _gamePage;

        private Button _closeButton;
        private Button _returnToMainMenuButton;
        private Button _devInjectButton;

        private Slider _bgmSlider;
        private Label _bgmValueLabel;
        private Slider _sfxSlider;
        private Label _sfxValueLabel;
        private Slider _uiSlider;
        private Label _uiValueLabel;

        private Button _saveButton;
        private Button _loadButton;
        private Label _saveInfoLabel;

        private Button _keyboardBindingsButton;
        private Button _gamepadBindingsButton;
        private Button _mouseBindingsButton;
        private Button _returnToGameButton;
        private Button _exitGameButton;

        private IAudioSettingsService _audioService;

        public SettingsPanel(VisualTreeAsset asset, UIPanelAssets assets, UIRouter router, SettingsOpenedFrom openedFrom)
            : base(asset)
        {
            _assets = assets;
            _router = router;
            _openedFrom = openedFrom;
        }

        protected override void OnBuild()
        {
            _contentContainer = Root.Q<VisualElement>("ContentContainer");
            _closeButton = Root.Q<Button>("CloseButton");
            _returnToMainMenuButton = Root.Q<Button>("ReturnToMainMenuButton");
            _devInjectButton = Root.Q<Button>("DevInjectButton");

            _audioTabButton = GetSidebarButton("AudioTab");
            _saveTabButton = GetSidebarButton("SaveTab");
            _gameTabButton = GetSidebarButton("GameTab");

            if (_audioTabButton != null)
            {
                _audioTabButton.text = "Audio";
                _audioTabButton.clicked += () => ShowPage(SettingsTab.Audio);
            }

            if (_saveTabButton != null)
            {
                _saveTabButton.text = "Save";
                _saveTabButton.clicked += () => ShowPage(SettingsTab.Save);
            }

            if (_gameTabButton != null)
            {
                _gameTabButton.text = "Game";
                _gameTabButton.clicked += () => ShowPage(SettingsTab.Game);
            }

            if (_closeButton != null)
            {
                _closeButton.text = _openedFrom == SettingsOpenedFrom.MainMenu ? "Back" : "Return";
                _closeButton.clicked += () => _router.CloseTop();
            }

            if (_returnToMainMenuButton != null)
            {
                _returnToMainMenuButton.clicked += () => _router.ReturnToMainMenuFromSettings();
            }

            BindDevInjectButton();

            BuildPages();
            ShowPage(SettingsTab.Audio);
        }

        public override void OnPushed()
        {
            _audioService = UIServiceRegistry.AudioSettings;
            if (_audioService != null)
            {
                _audioService.OnBgmChanged += OnBgmChanged;
                _audioService.OnSfxChanged += OnSfxChanged;
                _audioService.OnUiChanged += OnUiChanged;

                RefreshAudioFromService();
            }

            RefreshSaveInfo();
        }

        public override void OnPopped()
        {
            if (_audioService != null)
            {
                _audioService.OnBgmChanged -= OnBgmChanged;
                _audioService.OnSfxChanged -= OnSfxChanged;
                _audioService.OnUiChanged -= OnUiChanged;
                _audioService = null;
            }
        }

        private void BuildPages()
        {
            if (_contentContainer == null)
            {
                return;
            }

            _contentContainer.Clear();

            _audioPage = _assets.SettingsAudio != null ? _assets.SettingsAudio.CloneTree() : new VisualElement();
            _savePage = _assets.SettingsSave != null ? _assets.SettingsSave.CloneTree() : new VisualElement();
            _gamePage = _assets.SettingsGame != null ? _assets.SettingsGame.CloneTree() : new VisualElement();

            _contentContainer.Add(_audioPage);
            _contentContainer.Add(_savePage);
            _contentContainer.Add(_gamePage);

            BindAudioPage();
            BindSavePage();
            BindGamePage();
        }

        private void BindAudioPage()
        {
            var bgmRow = _audioPage.Q<VisualElement>("BgmRow");
            var sfxRow = _audioPage.Q<VisualElement>("SfxRow");
            var uiRow = _audioPage.Q<VisualElement>("UiRow");

            ConfigureSliderRow(bgmRow, "BGM", value => _audioService?.SetBgmVolume(value), out _bgmSlider, out _bgmValueLabel);
            ConfigureSliderRow(sfxRow, "SFX", value => _audioService?.SetSfxVolume(value), out _sfxSlider, out _sfxValueLabel);
            ConfigureSliderRow(uiRow, "UI SFX", value => _audioService?.SetUiVolume(value), out _uiSlider, out _uiValueLabel);
        }

        private void BindSavePage()
        {
            _saveButton = _savePage.Q<Button>("SaveButton");
            _loadButton = _savePage.Q<Button>("LoadButton");
            _saveInfoLabel = _savePage.Q<Label>("SaveInfoLabel");

            if (_saveButton != null)
            {
                _saveButton.clicked += () =>
                {
                    var saveService = UIServiceRegistry.SaveService;
                    if (saveService != null)
                    {
                        saveService.SaveSlot(0);
                        RefreshSaveInfo();
                    }
                    else
                    {
                        Debug.LogWarning("TODO(INTEGRATION): ISaveService not registered for SaveSlot.");
                    }
                };
            }

            if (_loadButton != null)
            {
                _loadButton.clicked += () =>
                {
                    var saveService = UIServiceRegistry.SaveService;
                    if (saveService != null)
                    {
                        saveService.LoadSlot(0);
                        RefreshSaveInfo();
                    }
                    else
                    {
                        Debug.LogWarning("TODO(INTEGRATION): ISaveService not registered for LoadSlot.");
                    }
                };
            }
        }

        private void BindGamePage()
        {
            _keyboardBindingsButton = _gamePage.Q<Button>("KeyboardBindingsButton");
            _gamepadBindingsButton = _gamePage.Q<Button>("GamepadBindingsButton");
            _mouseBindingsButton = _gamePage.Q<Button>("MouseBindingsButton");
            _returnToGameButton = _gamePage.Q<Button>("ReturnToGameButton");
            _exitGameButton = _gamePage.Q<Button>("ExitGameButton");

            if (_keyboardBindingsButton != null)
            {
                _keyboardBindingsButton.clicked += () => _router.OpenRebindModal(Models.DeviceType.Keyboard);
            }

            if (_gamepadBindingsButton != null)
            {
                _gamepadBindingsButton.clicked += () => _router.OpenRebindModal(Models.DeviceType.Gamepad);
            }

            if (_mouseBindingsButton != null)
            {
                _mouseBindingsButton.clicked += () => _router.OpenRebindModal(Models.DeviceType.Mouse);
            }

            if (_returnToGameButton != null)
            {
                _returnToGameButton.clicked += () => _router.CloseAll();
            }

            if (_exitGameButton != null)
            {
                _exitGameButton.clicked += QuitGame;
            }
        }

        private void ShowPage(SettingsTab tab)
        {
            SetTabSelected(_audioTabButton, tab == SettingsTab.Audio);
            SetTabSelected(_saveTabButton, tab == SettingsTab.Save);
            SetTabSelected(_gameTabButton, tab == SettingsTab.Game);

            SetPageVisible(_audioPage, tab == SettingsTab.Audio);
            SetPageVisible(_savePage, tab == SettingsTab.Save);
            SetPageVisible(_gamePage, tab == SettingsTab.Game);
        }

        private void BindDevInjectButton()
        {
            if (_devInjectButton == null)
            {
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _devInjectButton.clicked += () =>
            {
                // TODO(INTEGRATION): Hide or remove DEV button for production builds.
                UIMockBootstrap.Instance?.InjectNow();
            };
#else
            _devInjectButton.style.display = DisplayStyle.None;
#endif
        }

        private void ConfigureSliderRow(
            VisualElement row,
            string label,
            System.Action<float> onValueChanged,
            out Slider slider,
            out Label valueLabel)
        {
            slider = row?.Q<Slider>("Slider");
            valueLabel = row?.Q<Label>("ValueLabel");
            var titleLabel = row?.Q<Label>("Title");

            if (titleLabel != null)
            {
                titleLabel.text = label;
            }

            if (slider != null)
            {
                slider.lowValue = 0f;
                slider.highValue = 1f;
                slider.showInputField = false;
                var labelRef = valueLabel;
                slider.RegisterValueChangedCallback(evt =>
                {
                    UpdateValueLabel(labelRef, evt.newValue);
                    onValueChanged?.Invoke(evt.newValue);
                });
            }
        }

        private void RefreshAudioFromService()
        {
            if (_audioService == null)
            {
                return;
            }

            UpdateSlider(_bgmSlider, _bgmValueLabel, _audioService.GetBgmVolume());
            UpdateSlider(_sfxSlider, _sfxValueLabel, _audioService.GetSfxVolume());
            UpdateSlider(_uiSlider, _uiValueLabel, _audioService.GetUiVolume());
        }

        private void OnBgmChanged(float value)
        {
            UpdateSlider(_bgmSlider, _bgmValueLabel, value);
        }

        private void OnSfxChanged(float value)
        {
            UpdateSlider(_sfxSlider, _sfxValueLabel, value);
        }

        private void OnUiChanged(float value)
        {
            UpdateSlider(_uiSlider, _uiValueLabel, value);
        }

        private void UpdateSlider(Slider slider, Label label, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }

            UpdateValueLabel(label, value);
        }

        private void UpdateValueLabel(Label label, float value)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value * 100f).ToString();
            }
        }

        private void RefreshSaveInfo()
        {
            var saveService = UIServiceRegistry.SaveService;
            if (_saveInfoLabel == null)
            {
                return;
            }

            if (saveService != null && saveService.TryGetSlotInfo(0, out var info))
            {
                var summary = string.IsNullOrEmpty(info.Summary) ? "" : $"\n{info.Summary}";
                _saveInfoLabel.text =
                    $"Saved: {info.SavedAt:g}\n" +
                    $"Area: {info.AreaName}\n" +
                    $"Deaths (Area): {info.DeathsInArea}\n" +
                    $"Deaths (Total): {info.TotalDeaths}\n" +
                    $"Light: {info.LightValue:0.0}{summary}";
            }
            else
            {
                _saveInfoLabel.text = "No save data.";
            }
        }

        private Button GetSidebarButton(string containerName)
        {
            var container = Root.Q<TemplateContainer>(containerName);
            if (container == null)
            {
                return Root.Q<Button>(containerName);
            }

            return container.Q<Button>("SidebarButton") ?? container.Q<Button>();
        }

        private void SetPageVisible(VisualElement page, bool visible)
        {
            if (page != null)
            {
                page.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void SetTabSelected(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            if (selected)
            {
                button.AddToClassList("selected");
            }
            else
            {
                button.RemoveFromClassList("selected");
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private enum SettingsTab
        {
            Audio,
            Save,
            Game
        }
    }
}
