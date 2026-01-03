using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ThatGameJam.Features.FallingRockFromTrashCan.Editor
{
    public class AreaCameraShakeSetupWindow : EditorWindow
    {
        private const string RecommendedNoisePath = "Packages/com.unity.cinemachine/Presets/Noise/6D Shake.asset";
        private const float RecommendedAmplitudeGain = 1f;
        private const float RecommendedFrequencyGain = 1f;

        private CinemachineCamera _targetCamera;
        private NoiseSettings _noiseProfile;
        private float _amplitudeGain = RecommendedAmplitudeGain;
        private float _frequencyGain = RecommendedFrequencyGain;

        [MenuItem("Tools/Camera/Area Shake Setup")]
        public static void Open()
        {
            GetWindow<AreaCameraShakeSetupWindow>("Area Shake Setup");
        }

        private void OnEnable()
        {
            if (_noiseProfile == null)
            {
                _noiseProfile = AssetDatabase.LoadAssetAtPath<NoiseSettings>(RecommendedNoisePath);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Target Camera", EditorStyles.boldLabel);
            _targetCamera = (CinemachineCamera)EditorGUILayout.ObjectField(
                "Cinemachine Camera", _targetCamera, typeof(CinemachineCamera), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selection"))
                {
                    TryAssignFromSelection();
                }

                if (GUILayout.Button("Find Player Camera"))
                {
                    _targetCamera = FindPlayerCamera();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Noise Settings", EditorStyles.boldLabel);
            _noiseProfile = (NoiseSettings)EditorGUILayout.ObjectField(
                "Noise Profile", _noiseProfile, typeof(NoiseSettings), false);
            _amplitudeGain = EditorGUILayout.FloatField("Amplitude Gain", _amplitudeGain);
            _frequencyGain = EditorGUILayout.FloatField("Frequency Gain", _frequencyGain);

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Noise To Camera"))
            {
                ApplyNoiseSetup();
            }

            if (_noiseProfile == null)
            {
                EditorGUILayout.HelpBox("Noise Profile is empty. Assign a NoiseSettings asset or use the Cinemachine preset.",
                    MessageType.Warning);
            }
        }

        private void TryAssignFromSelection()
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            _targetCamera = Selection.activeGameObject.GetComponent<CinemachineCamera>();
        }

        private static CinemachineCamera FindPlayerCamera()
        {
            var cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            if (cameras == null || cameras.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].name.IndexOf("Player", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return cameras[i];
                }
            }

            return cameras[0];
        }

        private void ApplyNoiseSetup()
        {
            if (_targetCamera == null)
            {
                EditorUtility.DisplayDialog("Area Shake Setup",
                    "Please assign a CinemachineCamera in the scene first.", "OK");
                return;
            }

            var noise = _targetCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null)
            {
                noise = Undo.AddComponent<CinemachineBasicMultiChannelPerlin>(_targetCamera.gameObject);
            }

            Undo.RecordObject(noise, "Setup Cinemachine Noise");
            if (_noiseProfile != null)
            {
                noise.NoiseProfile = _noiseProfile;
            }

            noise.AmplitudeGain = Mathf.Max(0f, _amplitudeGain);
            noise.FrequencyGain = Mathf.Max(0f, _frequencyGain);
            noise.ReSeed();
            EditorUtility.SetDirty(noise);
        }
    }
}
