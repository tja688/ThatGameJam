using UnityEngine;

namespace ThatGameJam.Independents.Audio
{
    public class AudioHandle
    {
        public object NativeHandle;
        public AudioBus Bus;
        public bool IsMusic;
    }

    public struct AudioPlaybackParams
    {
        public AudioBus Bus;
        public float Volume;
        public float Pitch;
        public float ClipStartTime;
        public float ClipEndTime;
        public AudioSpatialMode SpatialMode;
        public float SpatialBlend;
        public float MinDistance;
        public float MaxDistance;
        public Transform Parent;
        public Vector3 Position;
        public bool HasPosition;
    }

    public interface IAudioBackend
    {
        AudioHandle PlayOneShot(AudioClip clip, AudioPlaybackParams parameters);
        AudioHandle PlayLoop(AudioClip clip, AudioPlaybackParams parameters);
        void Stop(AudioHandle handle, float fadeOut);
        void SetBusVolume(AudioBus bus, float volume);
        void UpdateHandleVolume(AudioHandle handle, float volume);
    }
}
