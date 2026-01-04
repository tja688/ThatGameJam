using PixelCrushers.DialogueSystem;
using ThatGameJam.Independents;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: DialogueImage(gameObjectName[, imageKey])
    ///
    /// Shows one of three sprites on a DialogueImageDisplay or clears it if imageKey is empty.
    /// imageKey accepts 1-3 (digits).
    /// </summary>
    public class SequencerCommandDialogueImage : SequencerCommand
    {
        public void Awake()
        {
            string objectName = GetParameter(0);
            string imageKey = GetParameter(1);

            if (string.IsNullOrEmpty(objectName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandDialogueImage: No object name provided.");
                Stop();
                return;
            }

            GameObject target = FindGameObjectIncludingInactive(objectName);

            if (target == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandDialogueImage: Could not find object named '{objectName}'.");
                Stop();
                return;
            }

            DialogueImageDisplay display = target.GetComponent<DialogueImageDisplay>();
            if (display == null)
            {
                display = target.GetComponentInChildren<DialogueImageDisplay>(true);
            }

            if (display == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandDialogueImage: No DialogueImageDisplay found on '{objectName}'.");
                Stop();
                return;
            }

            display.ApplyKey(imageKey);
            Stop();
        }

        private GameObject FindGameObjectIncludingInactive(string name)
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

        private GameObject FindRecursive(Transform parent, string name)
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
