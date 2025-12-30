using System;
using UnityEngine;

namespace ThatGameJam.UI.Models
{
    [Serializable]
    public struct PlayerPanelData
    {
        public Texture2D Portrait;
        public string AreaName;
        public int DeathsInArea;
        public int TotalDeaths;
        public float LightValue;
    }
}
