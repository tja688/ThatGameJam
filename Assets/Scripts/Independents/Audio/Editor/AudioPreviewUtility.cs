using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ThatGameJam.Independents.Audio.Editor
{
    public static class AudioPreviewUtility
    {
        private static MethodInfo _playMethod;
        private static MethodInfo _stopMethod;
        private static MethodInfo _setVolumeMethod;
        private static MethodInfo _setPitchMethod;
        private static bool _initialized;

        public static void PlayClip(AudioClip clip)
        {
            PlayClip(clip, false, 1f, 1f);
        }

        public static void PlayClip(AudioClip clip, bool loop)
        {
            PlayClip(clip, loop, 1f, 1f);
        }

        public static void PlayClip(AudioClip clip, bool loop, float volume, float pitch)
        {
            if (clip == null)
            {
                return;
            }

            EnsureMethods();
            if (_playMethod == null)
            {
                return;
            }

            ParameterInfo[] parameters = _playMethod.GetParameters();
            if (parameters.Length == 1)
            {
                _playMethod.Invoke(null, new object[] { clip });
            }
            else if (parameters.Length == 2)
            {
                _playMethod.Invoke(null, new object[] { clip, 0 });
            }
            else if (parameters.Length >= 3)
            {
                _playMethod.Invoke(null, new object[] { clip, 0, loop });
            }

            ApplyPreviewVolume(volume);
            ApplyPreviewPitch(pitch);
        }

        public static void StopAllClips()
        {
            EnsureMethods();
            _stopMethod?.Invoke(null, null);
        }

        private static void EnsureMethods()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            _playMethod = audioUtil.GetMethod("PlayPreviewClip", flags)
                           ?? audioUtil.GetMethod("PlayClip", flags);
            _stopMethod = audioUtil.GetMethod("StopAllPreviewClips", flags)
                          ?? audioUtil.GetMethod("StopAllClips", flags);
            _setVolumeMethod = audioUtil.GetMethod("SetPreviewVolume", flags);
            _setPitchMethod = audioUtil.GetMethod("SetPreviewPitch", flags);
        }

        private static void ApplyPreviewVolume(float volume)
        {
            EnsureMethods();
            if (_setVolumeMethod == null)
            {
                return;
            }

            _setVolumeMethod.Invoke(null, new object[] { Mathf.Clamp01(volume) });
        }

        private static void ApplyPreviewPitch(float pitch)
        {
            EnsureMethods();
            if (_setPitchMethod == null)
            {
                return;
            }

            _setPitchMethod.Invoke(null, new object[] { Mathf.Clamp(pitch, 0.1f, 3f) });
        }
    }
}
