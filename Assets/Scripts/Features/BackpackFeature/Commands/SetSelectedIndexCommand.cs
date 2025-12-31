using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Commands
{
    public class SetSelectedIndexCommand : AbstractCommand
    {
        private readonly int _index;

        public SetSelectedIndexCommand(int index)
        {
            _index = index;
        }

        protected override void OnExecute()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return;
            }

            var clamped = ClampIndex(model, _index);
            if (model.SelectedIndexValue == clamped)
            {
                return;
            }

            model.SelectedIndexValue = clamped;

            var definition = default(ItemDefinition);
            var quantity = 0;
            if (clamped >= 0 && clamped < model.Items.Count)
            {
                var entry = model.Items[clamped];
                definition = entry.Definition;
                quantity = entry.Quantity;
            }

            this.SendEvent(new BackpackSelectionChangedEvent
            {
                SelectedIndex = clamped,
                Definition = definition,
                Quantity = quantity
            });
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
