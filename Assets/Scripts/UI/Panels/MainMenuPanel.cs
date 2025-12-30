using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThatGameJam.UI.Panels
{
    public class MainMenuPanel : UIPanel
    {
        private readonly UIRouter _router;
        private Button _startButton;
        private Button _continueButton;
        private Button _settingsButton;
        private Button _quitButton;

        public MainMenuPanel(VisualTreeAsset asset, UIRouter router) : base(asset)
        {
            _router = router;
        }

        protected override void OnBuild()
        {
            _startButton = Root.Q<Button>("StartButton");
            _continueButton = Root.Q<Button>("ContinueButton");
            _settingsButton = Root.Q<Button>("SettingsButton");
            _quitButton = Root.Q<Button>("QuitButton");

            if (_startButton != null)
            {
                _startButton.clicked += OnStartClicked;
            }

            if (_continueButton != null)
            {
                _continueButton.clicked += OnContinueClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked += OnSettingsClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked += OnQuitClicked;
            }
        }

        public override void OnPushed()
        {
            _startButton?.Focus();
        }

        private void OnStartClicked()
        {
            var service = UIServiceRegistry.GameFlow;
            if (service != null)
            {
                service.StartNewGame();
            }
            else
            {
                Debug.LogWarning("TODO(INTEGRATION): IGameFlowService not registered for StartNewGame.");
            }

            _router?.CloseAll();
        }

        private void OnContinueClicked()
        {
            var saveService = UIServiceRegistry.SaveService;
            if (saveService != null)
            {
                saveService.LoadSlot(0);
            }
            else
            {
                Debug.LogWarning("TODO(INTEGRATION): ISaveService not registered for LoadSlot.");
            }

            _router?.CloseAll();
        }

        private void OnSettingsClicked()
        {
            _router?.OpenSettings(SettingsOpenedFrom.MainMenu);
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
