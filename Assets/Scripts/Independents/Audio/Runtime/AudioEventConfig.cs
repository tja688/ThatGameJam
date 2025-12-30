using System.Collections.Generic;
using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    [System.Serializable]
    public class AudioEventConfig
    {
        public string EventId;
        public string DisplayName;
        public AudioCategory Category = AudioCategory.SFX;
        public AudioBus Bus = AudioBus.SFX;
        public List<AudioClip> Clips = new List<AudioClip>();
        public AudioPlayMode PlayMode = AudioPlayMode.RandomOne;
        public bool Loop;
        public bool UseVolumeRange;
        public float Volume = 1f;
        public Vector2 VolumeRange = new Vector2(0.9f, 1f);
        public bool UsePitchRange;
        public float Pitch = 1f;
        public Vector2 PitchRange = new Vector2(0.95f, 1.05f);
        public float Cooldown;
        public float RandomIntervalMin;
        public float RandomIntervalMax;
        public AudioSpatialMode SpatialMode = AudioSpatialMode.TwoD;
        public float SpatialBlend;
        public float MinDistance = 1f;
        public float MaxDistance = 500f;
        public AudioStopPolicy StopPolicy = AudioStopPolicy.StopByOwner;
        public float FadeOutDuration;
    }
}
