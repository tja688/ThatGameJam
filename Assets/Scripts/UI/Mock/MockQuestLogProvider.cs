using System;
using System.Collections.Generic;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services.Interfaces;

namespace ThatGameJam.UI.Mock
{
    public class MockQuestLogProvider : IQuestLogProvider
    {
        public event Action OnQuestChanged;

        private readonly List<QuestData> _quests = new List<QuestData>();

        public IReadOnlyList<QuestData> GetQuests() => _quests;

        public void SetQuests(List<QuestData> quests)
        {
            _quests.Clear();
            if (quests != null)
            {
                _quests.AddRange(quests);
            }

            OnQuestChanged?.Invoke();
        }
    }
}
