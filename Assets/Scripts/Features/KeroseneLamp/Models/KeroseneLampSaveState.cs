using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Models
{
    [Serializable]
    public class KeroseneLampSaveState
    {
        public int nextLampId;
        public int heldLampId = -1;
        public List<KeroseneLampSaveEntry> lamps = new List<KeroseneLampSaveEntry>();
    }

    [Serializable]
    public class KeroseneLampSaveEntry
    {
        public int lampId;
        public Vector3 worldPos;
        public string areaId;
        public int spawnOrderInArea;
        public bool visualEnabled;
        public bool gameplayEnabled;
        public bool ignoreAreaLimit;
        public bool countInLampCount;
        public string presetId;
        public bool isHeld;
        public bool isPreplaced;
    }
}
