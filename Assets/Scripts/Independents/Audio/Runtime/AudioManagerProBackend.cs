using System.Collections;
using UnityEngine;
using AMPInternal;

namespace ThatGameJam.Independents.Audio
{
    public class AudioManagerProBackend : MonoBehaviour, IAudioBackend
    {
        [SerializeField] private SFXManager sfxManager;
        [SerializeField] private MusicManager musicManager;

        private void Awake()
        {
            if (sfxManager == null) { sfxManager = SFXManager.Main ?? FindObjectOfType<SFXManager>(); }
            if (musicManager == null) { musicManager = MusicManager.Main ?? FindObjectOfType<MusicManager>(); }
        }

        public AudioHandle PlayOneShot(AudioClip clip, AudioPlaybackParams parameters)
        {
            return PlayInternal(clip, parameters, false);
        }

        public AudioHandle PlayLoop(AudioClip clip, AudioPlaybackParams parameters)
        {
            return PlayInternal(clip, parameters, true);
        }

        public void Stop(AudioHandle handle, float fadeOut)
        {
            if (handle == null || handle.NativeHandle == null)
            {
                return;
            }

            AudioObject audio = handle.NativeHandle as AudioObject;
            if (audio == null)
            {
                return;
            }

            if (handle.IsMusic)
            {
                if (musicManager == null)
                {
                    return;
                }

                if (fadeOut > 0f)
                {
                    musicManager.FadeAudio(audio, fadeOut, 0f);
                }
                else
                {
                    musicManager.StopAudio(audio);
                }
            }
            else
            {
                if (sfxManager == null)
                {
                    return;
                }

                if (fadeOut > 0f)
                {
                    StartCoroutine(FadeOutSfx(audio, fadeOut));
                }
                else
                {
                    sfxManager.StopAudio(audio);
                }
            }
        }

        public void SetBusVolume(AudioBus bus, float volume)
        {
            if (bus == AudioBus.Music)
            {
                if (musicManager != null)
                {
                    musicManager.SetVolume(volume);
                }
            }
            else if (bus == AudioBus.SFX)
            {
                if (sfxManager != null)
                {
                    sfxManager.SetVolume(volume);
                }
            }
        }

        public void UpdateHandleVolume(AudioHandle handle, float volume)
        {
            if (handle == null || handle.NativeHandle == null)
            {
                return;
            }

            AudioObject audio = handle.NativeHandle as AudioObject;
            if (audio == null)
            {
                return;
            }

            audio.SourceVolume = volume;
        }

        private AudioHandle PlayInternal(AudioClip clip, AudioPlaybackParams parameters, bool loop)
        {
            if (clip == null)
            {
                return null;
            }

            bool isMusic = parameters.Bus == AudioBus.Music;
            AudioObject audio = null;
            Transform parent = parameters.Parent;

            if (isMusic)
            {
                if (musicManager == null)
                {
                    return null;
                }

                audio = musicManager.StartAudio(clip, loop, false);
                if (parent == null) { parent = musicManager.Parent; }
            }
            else
            {
                if (sfxManager == null)
                {
                    return null;
                }

                audio = sfxManager.StartAudio(clip, loop, false);
                if (parent == null) { parent = sfxManager.Parent; }
            }

            ApplyParams(audio, parameters, parent);

            float startTime = Mathf.Clamp(parameters.ClipStartTime, 0f, clip.length);
            if (audio.Source != null && startTime > 0f && startTime < clip.length)
            {
                audio.Source.time = startTime;
            }

            float endTime = parameters.ClipEndTime > 0f
                ? Mathf.Clamp(parameters.ClipEndTime, 0f, clip.length)
                : -1f;
            bool useCustomEnd = !loop && endTime > 0f && endTime > startTime;
            audio.Play();

            if (!loop)
            {
                if (isMusic && musicManager != null)
                {
                    float remaining = clip.length;
                    if (audio.Source != null)
                    {
                        remaining = Mathf.Max(0f, clip.length - audio.Source.time);
                    }

                    if (useCustomEnd)
                    {
                        musicManager.SequenceEnd(audio, GetPlaybackDuration(endTime - startTime, audio.Source));
                    }
                    else
                    {
                        musicManager.SequenceEnd(audio, remaining);
                    }
                }
                else if (sfxManager != null)
                {
                    sfxManager.SequenceEnd(audio);
                    if (useCustomEnd)
                    {
                        StartCoroutine(StopAfterDuration(audio, GetPlaybackDuration(endTime - startTime, audio.Source), isMusic));
                    }
                }
            }

            return new AudioHandle
            {
                NativeHandle = audio,
                Bus = parameters.Bus,
                IsMusic = isMusic
            };
        }

        private void ApplyParams(AudioObject audio, AudioPlaybackParams parameters, Transform parent)
        {
            if (audio == null || audio.Source == null)
            {
                return;
            }

            audio.SourceVolume = parameters.Volume;
            audio.Source.pitch = parameters.Pitch;

            audio.Source.spatialBlend = parameters.SpatialMode == AudioSpatialMode.ThreeD ? parameters.SpatialBlend : 0f;
            audio.Source.minDistance = parameters.MinDistance;
            audio.Source.maxDistance = parameters.MaxDistance;

            Transform audioTransform = audio.Source.transform;
            audioTransform.parent = parent;
            if (parameters.HasPosition)
            {
                audioTransform.position = parameters.Position;
            }
        }

        private IEnumerator FadeOutSfx(AudioObject audio, float duration)
        {
            if (audio == null)
            {
                yield break;
            }

            float startVolume = audio.volume;
            float time = 0f;
            while (time < duration)
            {
                if (audio.Source == null)
                {
                    yield break;
                }

                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                audio.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (sfxManager != null)
            {
                sfxManager.StopAudio(audio);
            }
        }

        private IEnumerator StopAfterDuration(AudioObject audio, float duration, bool isMusic)
        {
            if (audio == null || duration <= 0f)
            {
                yield break;
            }

            yield return new WaitForSeconds(duration);
            if (audio == null || audio.Source == null || !audio.Source.isPlaying)
            {
                yield break;
            }

            if (isMusic)
            {
                if (musicManager != null)
                {
                    musicManager.StopAudio(audio);
                }
            }
            else
            {
                if (sfxManager != null)
                {
                    sfxManager.StopAudio(audio);
                }
            }
        }

        private float GetPlaybackDuration(float duration, AudioSource source)
        {
            if (duration <= 0f)
            {
                return 0f;
            }

            float pitch = source != null ? Mathf.Abs(source.pitch) : 1f;
            if (pitch < 0.01f)
            {
                pitch = 1f;
            }

            return duration / pitch;
        }
    }
}
