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

        public static void PlayClip(AudioClip clip)
        {
            PlayClip(clip, false);
        }

        public static void PlayClip(AudioClip clip, bool loop)
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
        }

        public static void StopAllClips()
        {
            EnsureMethods();
            _stopMethod?.Invoke(null, null);
        }

        private static void EnsureMethods()
        {
            if (_playMethod != null && _stopMethod != null)
            {
                return;
            }

            Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
            {
                return;
            }

            _playMethod = audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public)
                           ?? audioUtil.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public);
            _stopMethod = audioUtil.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)
                          ?? audioUtil.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public);
        }
    }
}
