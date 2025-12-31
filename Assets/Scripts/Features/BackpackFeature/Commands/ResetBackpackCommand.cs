using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Commands
{
    public class ResetBackpackCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return;
            }

            for (var i = 0; i < model.Items.Count; i++)
            {
                var entry = model.Items[i];
                entry.Instance?.OnRemovedFromBackpack();
            }

            model.Clear();

            this.SendEvent(new BackpackChangedEvent { Count = model.Items.Count });
            this.SendEvent(new BackpackSelectionChangedEvent
            {
                SelectedIndex = -1,
                Definition = null,
                Quantity = 0
            });
            this.SendEvent(new HeldItemChangedEvent
            {
                HeldIndex = -1,
                Definition = null,
                Quantity = 0
            });
        }
    }
}
