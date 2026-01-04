using PixelCrushers.DialogueSystem;
using ThatGameJam.Independents;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: EnvelopeReadShow(gameObjectName[, completionMessage])
    ///
    /// Triggers the EnvelopeReadShow on a target object and waits for completion.
    /// </summary>
    public class SequencerCommandEnvelopeReadShow : SequencerCommand
    {
        private EnvelopeReadShow _show;

        public void Awake()
        {
            string objectName = GetParameter(0);
            string completionMessage = GetParameter(1);

            if (string.IsNullOrEmpty(objectName))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandEnvelopeReadShow: No object name provided.");
                Stop();
                return;
            }

            GameObject target = FindGameObjectIncludingInactive(objectName);

            if (target == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandEnvelopeReadShow: Could not find object named '{objectName}'.");
                Stop();
                return;
            }

            _show = target.GetComponent<EnvelopeReadShow>();
            if (_show == null)
            {
                _show = target.GetComponentInChildren<EnvelopeReadShow>(true);
            }

            if (_show == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandEnvelopeReadShow: No EnvelopeReadShow found on '{objectName}'.");
                Stop();
                return;
            }

            _show.Completed += HandleCompleted;

            bool started = _show.TryPlay(string.IsNullOrEmpty(completionMessage) ? null : completionMessage);
            if (!started)
            {
                _show.Completed -= HandleCompleted;
                Stop();
            }
        }

        private void HandleCompleted()
        {
            if (_show != null)
            {
                _show.Completed -= HandleCompleted;
            }

            Stop();
        }

        private void OnDisable()
        {
            if (_show != null)
            {
                _show.Completed -= HandleCompleted;
            }
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
