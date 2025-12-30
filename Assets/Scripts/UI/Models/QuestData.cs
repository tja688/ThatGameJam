using System;

namespace ThatGameJam.UI.Models
{
    [Serializable]
    public class QuestData
    {
        public string Id;
        public string Title;
        public string Description;
        public string Requirements;
        public bool IsCompleted;
    }
}
