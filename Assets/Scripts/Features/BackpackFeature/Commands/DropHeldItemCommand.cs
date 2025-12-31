using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;
using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Commands
{
    public class DropHeldItemCommand : AbstractCommand
    {
        private readonly Vector3 _worldPosition;
        private readonly bool _dropIfInstanceOnly;

        public DropHeldItemCommand(Vector3 worldPosition, bool dropIfInstanceOnly = false)
        {
            _worldPosition = worldPosition;
            _dropIfInstanceOnly = dropIfInstanceOnly;
        }

        protected override void OnExecute()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return;
            }

            var heldIndex = model.HeldIndexValue;
            if (heldIndex < 0 || heldIndex >= model.Items.Count)
            {
                return;
            }

            var entry = model.Items[heldIndex];
            if (_dropIfInstanceOnly && entry.Instance == null)
            {
                return;
            }

            if (entry.Instance != null)
            {
                entry.Instance.OnDropped(_worldPosition);
                entry.Instance.OnRemovedFromBackpack();
                model.Items.RemoveAt(heldIndex);
            }
            else
            {
                if (entry.Definition != null && entry.Definition.DropPrefab != null)
                {
                    Object.Instantiate(entry.Definition.DropPrefab, _worldPosition, Quaternion.identity);
                }

                entry.Quantity = Mathf.Max(0, entry.Quantity - 1);
                if (entry.Quantity <= 0)
                {
                    model.Items.RemoveAt(heldIndex);
                }
            }

            model.HeldIndexValue = -1;

            if (model.SelectedIndexValue >= model.Items.Count)
            {
                model.SelectedIndexValue = model.Items.Count - 1;
                var selectedEntry = model.SelectedIndexValue >= 0 ? model.Items[model.SelectedIndexValue] : null;
                this.SendEvent(new BackpackSelectionChangedEvent
                {
                    SelectedIndex = model.SelectedIndexValue,
                    Definition = selectedEntry != null ? selectedEntry.Definition : null,
                    Quantity = selectedEntry != null ? selectedEntry.Quantity : 0
                });
            }

            this.SendEvent(new BackpackChangedEvent { Count = model.Items.Count });
            this.SendEvent(new HeldItemChangedEvent
            {
                HeldIndex = -1,
                Definition = null,
                Quantity = 0
            });
        }
    }
}
