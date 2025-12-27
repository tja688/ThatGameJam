using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Models
{
    public struct LampInfo
    {
        public int LampId;
        public Vector3 WorldPos;
        public string AreaId;
        public int SpawnOrderInArea;
        public bool VisualEnabled;
        public bool GameplayEnabled;
        public string PresetId;
    }
}
