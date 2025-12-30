using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    [CreateAssetMenu(menuName = "ThatGameJam/Audio/Audio Debug Settings", fileName = "AudioDebugSettings")]
    public class AudioDebugSettings : ScriptableObject
    {
        public bool LogMissingEvents;
        public bool LogMissingClips;
        public bool LogPlay;
        public bool LogStop;
        public bool LogCooldown;
    }
}
