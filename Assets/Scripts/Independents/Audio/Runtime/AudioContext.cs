using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    public struct AudioContext
    {
        public Transform Owner;
        public Vector3 Position;
        public bool HasPosition;
        public bool IsUI;
        public float VolumeScale;
        public float PitchScale;
        public bool HasVolumeOverride;
        public float VolumeOverride;
        public bool HasPitchOverride;
        public float PitchOverride;
        public AudioBus? BusOverride;

        public static AudioContext Default => new AudioContext
        {
            VolumeScale = 1f,
            PitchScale = 1f
        };
    }
}
