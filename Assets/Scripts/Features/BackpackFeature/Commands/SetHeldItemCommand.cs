using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;
using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Commands
{
    public class SetHeldItemCommand : AbstractCommand
    {
        private readonly int _index;
        private readonly Transform _holdPoint;
        private readonly bool _applyInstance;
        private readonly bool _forceApply;

        public SetHeldItemCommand(int index, Transform holdPoint, bool applyInstance = true, bool forceApply = false)
        {
            _index = index;
            _holdPoint = holdPoint;
            _applyInstance = applyInstance;
            _forceApply = forceApply;
        }

        protected override void OnExecute()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return;
            }

            var clamped = ClampIndex(model, _index);
            if (!_forceApply && model.HeldIndexValue == clamped)
            {
                return;
            }

            if (_applyInstance)
            {
                ApplyInstanceChange(model, clamped);
            }

            model.HeldIndexValue = clamped;

            var definition = default(ItemDefinition);
            var quantity = 0;
            if (clamped >= 0 && clamped < model.Items.Count)
            {
                var entry = model.Items[clamped];
                definition = entry.Definition;
                quantity = entry.Quantity;
            }

            this.SendEvent(new HeldItemChangedEvent
            {
                HeldIndex = clamped,
                Definition = definition,
                Quantity = quantity
            });
        }

        private void ApplyInstanceChange(BackpackModel model, int clamped)
        {
            var previousIndex = model.HeldIndexValue;
            if (previousIndex >= 0 && previousIndex < model.Items.Count)
            {
                var previousEntry = model.Items[previousIndex];
                previousEntry.Instance?.OnAddedToBackpack();
            }

            if (clamped >= 0 && clamped < model.Items.Count)
            {
                var entry = model.Items[clamped];
                if (entry.Instance != null)
                {
                    entry.Instance.OnSetHeld(_holdPoint);
                }
            }
        }

        private static int ClampIndex(BackpackModel model, int index)
        {
            if (model.Items.Count == 0)
            {
                return -1;
            }

            if (index < 0)
            {
                return -1;
            }

            return index >= model.Items.Count ? model.Items.Count - 1 : index;
        }
    }
}
