using PixelCrushers.DialogueSystem;
using ThatGameJam.Independents.Audio;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: PlayEndingBgm(gameObjectName, endingIndex)
    ///
    /// - gameObjectName: The object name that has BackgroundMusicManager.
    /// - endingIndex: 1 or 2.
    /// </summary>
    public class SequencerCommandPlayEndingBgm : SequencerCommand
    {
        public void Awake()
        {
            string objectName = GetParameter(0);
            string endingParam = GetParameter(1);

            if (string.IsNullOrEmpty(objectName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandPlayEndingBgm: No object name provided.");
                Stop();
                return;
            }

            if (!int.TryParse(endingParam, out int endingIndex) || (endingIndex != 1 && endingIndex != 2))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandPlayEndingBgm: Invalid ending index '{endingParam}'. Use 1 or 2.");
                Stop();
                return;
            }

            GameObject target = FindGameObjectIncludingInactive(objectName);
            if (target == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandPlayEndingBgm: Could not find object named '{objectName}'.");
                Stop();
                return;
            }

            var manager = target.GetComponent<BackgroundMusicManager>();
            if (manager == null)
            {
                manager = target.GetComponentInChildren<BackgroundMusicManager>(true);
            }

            if (manager == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandPlayEndingBgm: No BackgroundMusicManager found on '{objectName}'.");
                Stop();
                return;
            }

            manager.PlayEndingMusic(endingIndex);
            Stop();
        }

        private static GameObject FindGameObjectIncludingInactive(string name)
        {
            GameObject activeObj = GameObject.Find(name);
            if (activeObj != null) return activeObj;

            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                GameObject result = FindRecursive(root.transform, name);
                if (result != null) return result;
            }

            return null;
        }

        private static GameObject FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject result = FindRecursive(parent.GetChild(i), name);
                if (result != null) return result;
            }

            return null;
        }
    }
}
