using PixelCrushers.DialogueSystem;
using ThatGameJam.Features.InteractableFeature.Controllers;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: HideInteractionAndSprite(gameObjectName)
    /// 
    /// Disables the Interactable component and the SpriteRenderer component on a specific GameObject.
    /// Used for "removing" an NPC or object visually and mechanically without destroying the GameObject.
    /// 
    /// Arguments:
    /// - gameObjectName: The name of the object to modify.
    /// </summary>
    public class SequencerCommandHideInteractionAndSprite : SequencerCommand
    {
        public void Awake()
        {
            string objectName = GetParameter(0);

            if (string.IsNullOrEmpty(objectName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandHideInteractionAndSprite: No object name provided.");
                Stop();
                return;
            }

            GameObject target = FindGameObjectIncludingInactive(objectName);

            if (target != null)
            {
                // 1. Disable Interactable
                var interactable = target.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.enabled = false;
                }

                // 2. Disable SpriteRenderer
                var renderer = target.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }

                if (DialogueDebug.logInfo) Debug.Log($"SequencerCommandHideInteractionAndSprite: Hid components on '{objectName}'.");
            }
            else
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandHideInteractionAndSprite: Could not find object named '{objectName}'.");
            }

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
