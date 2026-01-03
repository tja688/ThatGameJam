using System.Collections.Generic;
using System.Linq;
using ThatGameJam.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThatGameJam.UI
{
    public class UIRouter : MonoBehaviour
    {
        private readonly Stack<UIPanel> _stack = new Stack<UIPanel>();
        private UIPanelAssets _assets;
        private UIPauseService _pauseService;
        private VisualElement _root;
        private bool _initialized;

        public void Initialize(UIDocument document, UIPanelAssets assets, StyleSheet[] styleSheets, UIPauseService pauseService)
        {
            if (_initialized)
            {
                return;
            }

            _assets = assets;
            _pauseService = pauseService;

            if (document == null)
            {
                Debug.LogError("UIRouter requires a UIDocument.");
                return;
            }

            _root = document.rootVisualElement;
            _root.Clear();
            _root.style.flexGrow = 1f;
            _root.AddToClassList("ui-root");

            if (styleSheets != null)
            {
                foreach (var sheet in styleSheets)
                {
                    if (sheet != null && !_root.styleSheets.Contains(sheet))
                    {
                        _root.styleSheets.Add(sheet);
                    }
                }
            }

            _root.focusable = true;
            _root.tabIndex = 0;
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            UpdateRootVisibility();
            _initialized = true;
        }

        public void OpenMainMenu()
        {
            ClearStack();
            PushPanel(new MainMenuPanel(_assets.MainMenu, this));
        }

        public void OpenSettings(SettingsOpenedFrom openedFrom)
        {
            PushPanel(new SettingsPanel(_assets.SettingsMenu, _assets, this, openedFrom));
        }

        public void OpenRebindModal(Models.DeviceType device)
        {
            PushPanel(new RebindModalPanel(_assets.RebindModal, this, device));
        }

        public void OpenPlayerPanel()
        {
            PushPanel(new PlayerPanel(_assets.PlayerPanel, _assets, this));
        }

        public void CloseTop()
        {
            if (_stack.Count == 0)
            {
                return;
            }

            var panel = _stack.Pop();
            panel.OnPopped();
            if (panel.Root != null)
            {
                _root.Remove(panel.Root);
            }

            RefreshPauseState();
        }

        public void CloseAll()
        {
            ClearStack();
        }

        public void ReturnToMainMenuFromSettings()
        {
            var service = Services.UIServiceRegistry.GameFlow;
            if (service != null)
            {
                service.ReturnToMainMenu();
            }
            else
            {
                Services.MainMenuSceneLoader.ReturnToMainMenu();
            }
        }

        private void PushPanel(UIPanel panel)
        {
            if (!_initialized)
            {
                Debug.LogWarning("UIRouter not initialized; call Initialize first.");
                return;
            }

            if (panel == null)
            {
                return;
            }

            panel.Build();
            _stack.Push(panel);
            _root.Add(panel.Root);
            panel.OnPushed();
            panel.Root.Focus();

            RefreshPauseState();
        }

        private void ClearStack()
        {
            while (_stack.Count > 0)
            {
                var panel = _stack.Pop();
                panel.OnPopped();
                if (panel.Root != null)
                {
                    _root.Remove(panel.Root);
                }
            }

            RefreshPauseState();
        }

        private void RefreshPauseState()
        {
            var shouldPause = _stack.Any(panel => panel.PausesGame);
            if (_pauseService != null)
            {
                _pauseService.ApplyPauseState(shouldPause);
            }

            UpdateRootVisibility();
        }

        private void UpdateRootVisibility()
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = _stack.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            if (_stack.Count == 0)
            {
                return;
            }

            var top = _stack.Peek();
            if (top.HandleEscape())
            {
                evt.StopPropagation();
                return;
            }

            CloseTop();
            evt.StopPropagation();
        }
    }
}
