using System.Collections.Generic;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine.UIElements;

namespace ThatGameJam.UI.Panels
{
    public class PlayerPanel : UIPanel
    {
        private readonly UIRouter _router;
        private readonly UIPanelAssets _assets;

        private VisualElement _portrait;
        private Label _lightLabel;
        private Label _areaLabel;
        private Label _deathsInAreaLabel;
        private Label _totalDeathsLabel;

        private ScrollView _questList;
        private Label _questTitle;
        private Label _questDescription;
        private Label _questRequirements;
        private Label _questStatus;

        private VisualElement _inventoryGrid;
        private readonly List<VisualElement> _inventorySlots = new List<VisualElement>();

        private IPlayerStatusProvider _playerStatus;
        private IQuestLogProvider _questLog;
        private IInventoryProvider _inventory;

        private VisualElement _selectedQuestElement;

        public PlayerPanel(VisualTreeAsset asset, UIPanelAssets assets, UIRouter router) : base(asset)
        {
            _router = router;
            _assets = assets;
        }

        protected override void OnBuild()
        {
            _portrait = Root.Q<VisualElement>("PortraitImage");
            _lightLabel = Root.Q<Label>("LightValueLabel");
            _areaLabel = Root.Q<Label>("AreaLabel");
            _deathsInAreaLabel = Root.Q<Label>("DeathsInAreaLabel");
            _totalDeathsLabel = Root.Q<Label>("TotalDeathsLabel");

            _questList = Root.Q<ScrollView>("QuestList");
            _questTitle = Root.Q<Label>("QuestTitle");
            _questDescription = Root.Q<Label>("QuestDescription");
            _questRequirements = Root.Q<Label>("QuestRequirements");
            _questStatus = Root.Q<Label>("QuestStatus");

            _inventoryGrid = Root.Q<VisualElement>("InventoryGrid");

            var closeButton = Root.Q<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.clicked += () => _router.CloseTop();
            }

            BuildInventorySlots();
        }

        public override void OnPushed()
        {
            _playerStatus = UIServiceRegistry.PlayerStatus;
            _questLog = UIServiceRegistry.QuestLog;
            _inventory = UIServiceRegistry.Inventory;

            if (_playerStatus != null)
            {
                _playerStatus.OnChanged += OnPlayerStatusChanged;
                ApplyPlayerStatus(_playerStatus.GetData());
            }

            if (_questLog != null)
            {
                _questLog.OnQuestChanged += RefreshQuestList;
            }

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += RefreshInventory;
            }

            RefreshQuestList();
            RefreshInventory();
        }

        public override void OnPopped()
        {
            if (_playerStatus != null)
            {
                _playerStatus.OnChanged -= OnPlayerStatusChanged;
            }

            if (_questLog != null)
            {
                _questLog.OnQuestChanged -= RefreshQuestList;
            }

            if (_inventory != null)
            {
                _inventory.OnInventoryChanged -= RefreshInventory;
            }
        }

        private void OnPlayerStatusChanged(PlayerPanelData data)
        {
            ApplyPlayerStatus(data);
        }

        private void ApplyPlayerStatus(PlayerPanelData data)
        {
            if (_portrait != null)
            {
                _portrait.style.backgroundImage = data.Portrait != null
                    ? new StyleBackground(data.Portrait)
                    : StyleKeyword.None;
            }

            if (_lightLabel != null)
            {
                _lightLabel.text = $"Light: {data.LightValue:0.0}";
            }

            if (_areaLabel != null)
            {
                _areaLabel.text = string.IsNullOrEmpty(data.AreaName) ? "Area: -" : $"Area: {data.AreaName}";
            }

            if (_deathsInAreaLabel != null)
            {
                _deathsInAreaLabel.text = $"Deaths (Area): {data.DeathsInArea}";
            }

            if (_totalDeathsLabel != null)
            {
                _totalDeathsLabel.text = $"Deaths (Total): {data.TotalDeaths}";
            }
        }

        private void RefreshQuestList()
        {
            if (_questList == null)
            {
                return;
            }

            _questList.Clear();
            _selectedQuestElement = null;

            var quests = _questLog?.GetQuests();
            if (quests == null || quests.Count == 0)
            {
                var emptyLabel = new Label("No quests.");
                emptyLabel.AddToClassList("quest-empty");
                _questList.Add(emptyLabel);
                ApplyQuestDetails(null);
                return;
            }

            VisualElement firstItem = null;

            foreach (var quest in quests)
            {
                var itemRoot = _assets.QuestListItem != null ? _assets.QuestListItem.CloneTree() : new VisualElement();
                var button = itemRoot.Q<Button>("QuestButton") ?? itemRoot.Q<Button>();
                var titleLabel = itemRoot.Q<Label>("QuestTitle");
                var statusLabel = itemRoot.Q<Label>("QuestStatus");

                if (titleLabel != null)
                {
                    titleLabel.text = quest.Title;
                }

                if (statusLabel != null)
                {
                    statusLabel.text = quest.IsCompleted ? "Done" : "Active";
                }

                if (button != null)
                {
                    button.clicked += () => SelectQuest(quest, itemRoot);
                }

                _questList.Add(itemRoot);

                if (firstItem == null)
                {
                    firstItem = itemRoot;
                }
            }

            if (firstItem != null)
            {
                SelectQuest(quests[0], firstItem);
            }
        }

        private void SelectQuest(QuestData quest, VisualElement element)
        {
            if (_selectedQuestElement != null)
            {
                _selectedQuestElement.RemoveFromClassList("selected");
            }

            _selectedQuestElement = element;
            _selectedQuestElement?.AddToClassList("selected");

            ApplyQuestDetails(quest);
        }

        private void ApplyQuestDetails(QuestData quest)
        {
            if (_questTitle != null)
            {
                _questTitle.text = quest != null ? quest.Title : "Select a quest";
            }

            if (_questDescription != null)
            {
                _questDescription.text = quest?.Description ?? string.Empty;
            }

            if (_questRequirements != null)
            {
                _questRequirements.text = quest?.Requirements ?? string.Empty;
            }

            if (_questStatus != null)
            {
                _questStatus.text = quest != null
                    ? (quest.IsCompleted ? "Status: Completed" : "Status: In Progress")
                    : string.Empty;
            }
        }

        private void BuildInventorySlots()
        {
            if (_inventoryGrid == null)
            {
                return;
            }

            _inventoryGrid.Clear();
            _inventorySlots.Clear();

            for (var i = 0; i < 6; i++)
            {
                var slot = _assets.InventorySlot != null ? _assets.InventorySlot.CloneTree() : new VisualElement();
                slot.AddToClassList("inventory-slot");
                _inventoryGrid.Add(slot);
                _inventorySlots.Add(slot);
            }
        }

        private void RefreshInventory()
        {
            if (_inventorySlots.Count == 0)
            {
                return;
            }

            var slots = _inventory?.GetSlots(6);
            for (var i = 0; i < _inventorySlots.Count; i++)
            {
                var slotRoot = _inventorySlots[i];
                var icon = slotRoot.Q<VisualElement>("Icon");
                var countLabel = slotRoot.Q<Label>("CountLabel");

                ItemStack item = null;
                if (slots != null && i < slots.Count)
                {
                    item = slots[i];
                }

                if (icon != null)
                {
                    if (item != null && item.Icon != null)
                    {
                        icon.style.backgroundImage = new StyleBackground(item.Icon);
                    }
                    else
                    {
                        icon.style.backgroundImage = StyleKeyword.None;
                    }
                }

                if (countLabel != null)
                {
                    countLabel.text = item != null && item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;
                }

                if (item == null)
                {
                    slotRoot.AddToClassList("empty");
                }
                else
                {
                    slotRoot.RemoveFromClassList("empty");
                }
            }
        }
    }
}
