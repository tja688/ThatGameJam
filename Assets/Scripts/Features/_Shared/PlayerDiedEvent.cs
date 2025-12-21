using UnityEngine;

namespace ThatGameJam.Features.Shared
{
    public struct PlayerDiedEvent
    {
        public EDeathReason Reason;
        public Vector3 WorldPos;
    }
}
