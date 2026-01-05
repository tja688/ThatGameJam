using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine.UIElements;

namespace ThatGameJam.UI.Panels
{
    public class InstructionsPanel : UIPanel
    {
        private readonly UIRouter _router;

        private Label _instructionsLabel;
        private IOperationInstructionsProvider _provider;

        public InstructionsPanel(VisualTreeAsset asset, UIRouter router) : base(asset)
        {
            _router = router;
        }

        protected override void OnBuild()
        {
            _instructionsLabel = Root.Q<Label>("InstructionsLabel");

            var returnButton = Root.Q<Button>("ReturnButton");
            if (returnButton != null)
            {
                returnButton.clicked += () => _router.CloseTop();
            }

            ApplyInstructions(null);
        }

        public override void OnPushed()
        {
            _provider = UIServiceRegistry.OperationInstructions;
            if (_provider != null)
            {
                _provider.OnChanged += OnInstructionsChanged;
                ApplyInstructions(_provider.GetInstructions());
            }
            else
            {
                ApplyInstructions(null);
            }
        }

        public override void OnPopped()
        {
            if (_provider != null)
            {
                _provider.OnChanged -= OnInstructionsChanged;
                _provider = null;
            }
        }

        private void OnInstructionsChanged(string text)
        {
            ApplyInstructions(text);
        }

        private void ApplyInstructions(string text)
        {
            if (_instructionsLabel == null)
            {
                return;
            }

            _instructionsLabel.text = string.IsNullOrWhiteSpace(text) ? "No instructions." : text;
        }
    }
}
