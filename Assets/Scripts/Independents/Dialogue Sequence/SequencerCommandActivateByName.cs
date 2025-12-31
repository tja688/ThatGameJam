using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: ActivateByName(gameObjectName)
    /// 
    /// Activates a GameObject in the scene by its name. 
    /// This version can find inactive objects by searching through all root objects.
    /// 
    /// Arguments:
    /// - gameObjectName: The exact name of the object to activate.
    /// </summary>
    public class SequencerCommandActivateByName : SequencerCommand
    {
        public void Awake()
        {
            string objectName = GetParameter(0);

            if (string.IsNullOrEmpty(objectName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandActivateByName: No object name provided.");
                Stop();
                return;
            }

            GameObject target = FindGameObjectIncludingInactive(objectName);

            if (target != null)
            {
                target.SetActive(true);
                if (DialogueDebug.logInfo) Debug.Log($"SequencerCommandActivateByName: Activated '{objectName}'.");
            }
            else
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandActivateByName: Could not find object named '{objectName}'.");
            }

            Stop();
        }

        /// <summary>
        /// Finds a GameObject by name, including inactive ones.
        /// GameObject.Find only finds active objects, so we need to search through root objects.
        /// </summary>
        private GameObject FindGameObjectIncludingInactive(string name)
        {
            // Standard find (fastest for active objects)
            GameObject activeObj = GameObject.Find(name);
            if (activeObj != null) return activeObj;

            // Deep search for inactive objects
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
