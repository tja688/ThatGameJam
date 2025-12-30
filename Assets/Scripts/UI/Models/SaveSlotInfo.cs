using System;

namespace ThatGameJam.UI.Models
{
    [Serializable]
    public struct SaveSlotInfo
    {
        public DateTime SavedAt;
        public string AreaName;
        public int DeathsInArea;
        public int TotalDeaths;
        public float LightValue;
        public string Summary;
    }
}
