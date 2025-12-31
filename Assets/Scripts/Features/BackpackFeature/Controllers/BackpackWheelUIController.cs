using System.Collections;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.BackpackFeature.Queries;
using UnityEngine;
using UnityEngine.UI;

namespace ThatGameJam.Features.BackpackFeature.Controllers
{
    public class BackpackWheelUIController : MonoBehaviour, IController
    {
        [System.Serializable]
        private class WheelSlot
        {
            public CanvasGroup group;
            public RawImage icon;
            public Text label;
            public Text count;
        }

        [Header("Slots")]
        [SerializeField] private WheelSlot previousSlot;
        [SerializeField] private WheelSlot currentSlot;
        [SerializeField] private WheelSlot nextSlot;

        [Header("Style")]
        [SerializeField] private float sideAlpha = 0.4f;
        [SerializeField] private float fadeDuration = 0.15f;

        private int _lastSelectedIndex = -1;
        private Coroutine _fadeRoutine;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<BackpackSelectionChangedEvent>(e => RefreshWheel(e.SelectedIndex))
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<BackpackChangedEvent>(_ => RefreshFromModel())
                .UnRegisterWhenDisabled(gameObject);

            RefreshFromModel();
        }

        private void RefreshFromModel()
        {
            var selectedIndex = this.SendQuery(new GetSelectedIndexQuery());
            RefreshWheel(selectedIndex);
        }

        private void RefreshWheel(int selectedIndex)
        {
            var items = this.SendQuery(new GetBackpackItemsQuery());
            if (items == null || items.Count == 0)
            {
                SetSlot(previousSlot, null, 0);
                SetSlot(currentSlot, null, 0);
                SetSlot(nextSlot, null, 0);
                _lastSelectedIndex = -1;
                return;
            }

            var current = selectedIndex >= 0 && selectedIndex < items.Count ? selectedIndex : 0;
            var prev = (current - 1 + items.Count) % items.Count;
            var next = (current + 1) % items.Count;

            SetSlot(previousSlot, items[prev].Definition, items[prev].Quantity);
            SetSlot(currentSlot, items[current].Definition, items[current].Quantity);
            SetSlot(nextSlot, items[next].Definition, items[next].Quantity);

            SetAlpha(currentSlot, 1f);
            SetAlpha(previousSlot, sideAlpha);
            SetAlpha(nextSlot, sideAlpha);

            AnimateEdgeFade(items.Count, _lastSelectedIndex, current);
            _lastSelectedIndex = current;
        }

        private void AnimateEdgeFade(int itemCount, int previousIndex, int currentIndex)
        {
            if (itemCount <= 1 || previousIndex < 0)
            {
                return;
            }

            var direction = 0;
            if ((previousIndex + 1) % itemCount == currentIndex)
            {
                direction = 1;
            }
            else if ((previousIndex - 1 + itemCount) % itemCount == currentIndex)
            {
                direction = -1;
            }

            if (direction == 0)
            {
                return;
            }

            var incoming = direction > 0 ? nextSlot : previousSlot;
            if (incoming == null || incoming.group == null)
            {
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(FadeSlot(incoming, sideAlpha));
        }

        private IEnumerator FadeSlot(WheelSlot slot, float targetAlpha)
        {
            var group = slot.group;
            group.alpha = 0f;
            var elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(0f, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            group.alpha = targetAlpha;
        }

        private void SetSlot(WheelSlot slot, ItemDefinition definition, int quantity)
        {
            if (slot == null)
            {
                return;
            }

            if (slot.icon != null)
            {
                slot.icon.texture = definition != null ? definition.Icon : null;
                slot.icon.enabled = definition != null && definition.Icon != null;
            }

            if (slot.label != null)
            {
                slot.label.text = definition != null ? definition.DisplayName : string.Empty;
            }

            if (slot.count != null)
            {
                slot.count.text = definition != null && quantity > 1 ? quantity.ToString() : string.Empty;
            }
        }

        private static void SetAlpha(WheelSlot slot, float alpha)
        {
            if (slot != null && slot.group != null)
            {
                slot.group.alpha = alpha;
            }
        }
    }
}
