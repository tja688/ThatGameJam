using System;
using System.Collections.Generic;
using ThatGameJam.UI.Models;

namespace ThatGameJam.UI.Services.Interfaces
{
    public interface IQuestLogProvider
    {
        IReadOnlyList<QuestData> GetQuests();
        event Action OnQuestChanged;
    }
}
