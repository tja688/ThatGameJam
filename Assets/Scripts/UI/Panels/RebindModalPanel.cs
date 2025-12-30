using System.Collections.Generic;
using System.Threading.Tasks;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services;
using UnityEngine;
using UnityEngine.UIElements;
using DeviceType = ThatGameJam.UI.Models.DeviceType;

namespace ThatGameJam.UI.Panels
{
    public class RebindModalPanel : UIPanel
    {
        private readonly UIRouter _router;
        private readonly DeviceType _device;

        private ScrollView _listView;
        private Label _deviceLabel;
        private Label _promptLabel;
        private Button _closeButton;

        private bool _isRebinding;

        public RebindModalPanel(VisualTreeAsset asset, UIRouter router, DeviceType device) : base(asset)
        {
            _router = router;
            _device = device;
        }

        public override bool IsModal => true;

        protected override void OnBuild()
        {
            _listView = Root.Q<ScrollView>("RebindList");
            _deviceLabel = Root.Q<Label>("DeviceLabel");
            _promptLabel = Root.Q<Label>("PromptLabel");
            _closeButton = Root.Q<Button>("CloseButton");

            if (_deviceLabel != null)
            {
                _deviceLabel.text = $"Device: {_device}";
            }

            if (_promptLabel != null)
            {
                _promptLabel.style.display = DisplayStyle.None;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += () => _router.CloseTop();
            }

            RefreshEntries();
        }

        public override void OnPopped()
        {
            if (_isRebinding)
            {
                UIServiceRegistry.InputRebind?.CancelRebind();
                _isRebinding = false;
            }
        }

        public override bool HandleEscape()
        {
            if (_isRebinding)
            {
                CancelRebind();
                return true;
            }

            return false;
        }

        private void RefreshEntries()
        {
            if (_listView == null)
            {
                return;
            }

            _listView.Clear();
            var service = UIServiceRegistry.InputRebind;
            if (service == null)
            {
                _listView.Add(new Label("No rebind service available."));
                return;
            }

            foreach (var entry in service.GetEntries(_device))
            {
                _listView.Add(BuildEntryRow(entry));
            }
        }

        private VisualElement BuildEntryRow(RebindEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("rebind-row");

            var nameLabel = new Label(entry.DisplayName);
            nameLabel.AddToClassList("rebind-row__name");

            var bindingLabel = new Label(entry.BindingDisplay);
            bindingLabel.AddToClassList("rebind-row__binding");

            var rebindButton = new Button(async () => await StartRebind(entry))
            {
                text = "Rebind"
            };
            rebindButton.AddToClassList("menu-button");

            var resetButton = new Button(() => ResetBinding(entry))
            {
                text = "Reset"
            };
            resetButton.AddToClassList("ghost-button");
            resetButton.SetEnabled(entry.CanReset);

            row.Add(nameLabel);
            row.Add(bindingLabel);
            row.Add(rebindButton);
            row.Add(resetButton);

            return row;
        }

        private async Task StartRebind(RebindEntry entry)
        {
            var service = UIServiceRegistry.InputRebind;
            if (service == null)
            {
                Debug.LogWarning("TODO(INTEGRATION): IInputRebindService not registered for rebind.");
                return;
            }

            if (_isRebinding)
            {
                return;
            }

            _isRebinding = true;
            SetPromptVisible(true);
            _listView?.SetEnabled(false);

            await service.StartRebind(entry.ActionId, entry.BindingIndex);

            _isRebinding = false;
            SetPromptVisible(false);
            _listView?.SetEnabled(true);
            RefreshEntries();
        }

        private void ResetBinding(RebindEntry entry)
        {
            var service = UIServiceRegistry.InputRebind;
            if (service == null)
            {
                Debug.LogWarning("TODO(INTEGRATION): IInputRebindService not registered for reset.");
                return;
            }

            service.ResetBinding(entry.ActionId, entry.BindingIndex);
            RefreshEntries();
        }

        private void CancelRebind()
        {
            UIServiceRegistry.InputRebind?.CancelRebind();
            _isRebinding = false;
            SetPromptVisible(false);
            _listView?.SetEnabled(true);
        }

        private void SetPromptVisible(bool visible)
        {
            if (_promptLabel != null)
            {
                _promptLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
