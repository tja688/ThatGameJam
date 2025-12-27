using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Events
{
    public struct RequestSpawnLampEvent
    {
        public Vector3 WorldPos;
        public GameObject PrefabOverride;
        public string PresetId;
    }
}
