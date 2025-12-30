using UnityEngine.UIElements;

namespace ThatGameJam.UI
{
    public abstract class UIPanel
    {
        private readonly VisualTreeAsset _asset;

        protected UIPanel(VisualTreeAsset asset)
        {
            _asset = asset;
        }

        public VisualElement Root { get; private set; }

        public virtual bool PausesGame => true;

        public virtual bool IsModal => false;

        public void Build()
        {
            Root = _asset != null ? _asset.CloneTree() : new VisualElement();
            Root.AddToClassList("ui-panel");
            Root.style.flexGrow = 1f;
            OnBuild();
        }

        protected abstract void OnBuild();

        public virtual void OnPushed() { }

        public virtual void OnPopped() { }

        public virtual bool HandleEscape() => false;
    }
}
