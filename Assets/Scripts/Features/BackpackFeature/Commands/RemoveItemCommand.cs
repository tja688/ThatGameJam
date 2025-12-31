using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Commands
{
    public class RemoveItemCommand : AbstractCommand
    {
        private readonly string _itemId;
        private readonly int _quantity;

        public RemoveItemCommand(string itemId, int quantity)
        {
            _itemId = itemId;
            _quantity = quantity;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_itemId) || _quantity <= 0)
            {
                return;
            }

            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return;
            }

            var previousSelected = model.SelectedIndexValue;
            var previousHeld = model.HeldIndexValue;

            var remaining = _quantity;
            for (var i = model.Items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var entry = model.Items[i];
                if (entry.Definition == null || entry.Definition.Id != _itemId)
                {
                    continue;
                }

                if (entry.Instance != null)
                {
                    entry.Instance.OnRemovedFromBackpack();
                    model.Items.RemoveAt(i);
                    remaining -= 1;
                    continue;
                }

                if (entry.Quantity > remaining)
                {
                    entry.Quantity -= remaining;
                    remaining = 0;
                    break;
                }

                remaining -= entry.Quantity;
                model.Items.RemoveAt(i);
            }

            NormalizeIndices(model);
            this.SendEvent(new BackpackChangedEvent { Count = model.Items.Count });

            if (model.SelectedIndexValue != previousSelected)
            {
                var selectedEntry = model.SelectedIndexValue >= 0 ? model.Items[model.SelectedIndexValue] : null;
                this.SendEvent(new BackpackSelectionChangedEvent
                {
                    SelectedIndex = model.SelectedIndexValue,
                    Definition = selectedEntry != null ? selectedEntry.Definition : null,
                    Quantity = selectedEntry != null ? selectedEntry.Quantity : 0
                });
            }

            if (model.HeldIndexValue != previousHeld)
            {
                var heldEntry = model.HeldIndexValue >= 0 ? model.Items[model.HeldIndexValue] : null;
                this.SendEvent(new HeldItemChangedEvent
                {
                    HeldIndex = model.HeldIndexValue,
                    Definition = heldEntry != null ? heldEntry.Definition : null,
                    Quantity = heldEntry != null ? heldEntry.Quantity : 0
                });
            }
        }

        private void NormalizeIndices(BackpackModel model)
        {
            if (model.SelectedIndexValue >= model.Items.Count)
            {
                model.SelectedIndexValue = model.Items.Count - 1;
            }

            if (model.HeldIndexValue >= model.Items.Count)
            {
                model.HeldIndexValue = -1;
            }
        }
    }
}
