using UnityEngine.UIElements;

namespace MermaidPreviewer
{
    internal sealed class ToastController
    {
        private readonly VisualElement _root;
        private readonly Label _label;
        private IVisualElementScheduledItem _hideItem;
        private IVisualElementScheduledItem _fadeItem;

        private const int FadeDelayMs = 2000;
        private const int HideDelayMs = 2300;

        public ToastController(VisualElement root, Label label)
        {
            _root = root;
            _label = label;

            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _root.style.opacity = 0f;
            }
        }

        public void Show(string message)
        {
            if (_root == null || _label == null)
            {
                return;
            }

            _label.text = string.IsNullOrWhiteSpace(message) ? "Error" : message;

            _fadeItem?.Pause();
            _hideItem?.Pause();

            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;

            _fadeItem = _root.schedule.Execute(() =>
            {
                _root.style.opacity = 0f;
            }).StartingIn(FadeDelayMs);

            _hideItem = _root.schedule.Execute(() =>
            {
                _root.style.display = DisplayStyle.None;
            }).StartingIn(HideDelayMs);
        }
    }
}
