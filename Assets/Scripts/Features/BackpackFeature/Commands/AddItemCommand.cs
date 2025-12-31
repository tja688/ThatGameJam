using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;

namespace ThatGameJam.Features.BackpackFeature.Commands
{
    public class AddItemCommand : AbstractCommand<int>
    {
        private readonly ItemDefinition _definition;
        private readonly IBackpackItemInstance _instance;
        private readonly int _quantity;

        public AddItemCommand(ItemDefinition definition, IBackpackItemInstance instance, int quantity)
        {
            _definition = definition;
            _instance = instance;
            _quantity = quantity;
        }

        protected override int OnExecute()
        {
            if (_definition == null || _quantity <= 0)
            {
                LogKit.W("AddItemCommand called with invalid definition or quantity.");
                return -1;
            }

            var model = (BackpackModel)this.GetModel<IBackpackModel>();
            if (model == null)
            {
                return -1;
            }

            var addedIndex = -1;
            if (_instance != null)
            {
                var entry = new BackpackModel.BackpackItemEntry
                {
                    Definition = _definition,
                    Quantity = 1,
                    Instance = _instance
                };
                model.Items.Add(entry);
                addedIndex = model.Items.Count - 1;
                _instance.OnAddedToBackpack();
            }
            else if (_definition.Stackable)
            {
                var remaining = _quantity;
                for (var i = 0; i < model.Items.Count && remaining > 0; i++)
                {
                    var entry = model.Items[i];
                    if (entry.Definition != _definition || entry.Instance != null)
                    {
                        continue;
                    }

                    var maxStack = _definition.MaxStack;
                    if (maxStack > 0 && entry.Quantity >= maxStack)
                    {
                        continue;
                    }

                    var addCount = remaining;
                    if (maxStack > 0)
                    {
                        addCount = System.Math.Min(addCount, maxStack - entry.Quantity);
                    }

                    entry.Quantity += addCount;
                    remaining -= addCount;
                    addedIndex = i;
                }

                while (remaining > 0)
                {
                    var maxStack = _definition.MaxStack;
                    var addCount = maxStack > 0 ? System.Math.Min(remaining, maxStack) : remaining;

                    model.Items.Add(new BackpackModel.BackpackItemEntry
                    {
                        Definition = _definition,
                        Quantity = addCount,
                        Instance = null
                    });
                    remaining -= addCount;
                    addedIndex = model.Items.Count - 1;
                }
            }
            else
            {
                model.Items.Add(new BackpackModel.BackpackItemEntry
                {
                    Definition = _definition,
                    Quantity = _quantity,
                    Instance = null
                });
                addedIndex = model.Items.Count - 1;
            }

            this.SendEvent(new BackpackChangedEvent { Count = model.Items.Count });

            return addedIndex;
        }
    }
}
