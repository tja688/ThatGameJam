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
        private const string LOG_TAG = "[EnvelopeReadShow-CMD] ";
        private EnvelopeReadShow _show;
        private string _objectName;

        public void Awake()
        {
            Debug.Log($"{LOG_TAG}========== COMMAND AWAKE CALLED ==========");
            Debug.Log($"{LOG_TAG}Time: {Time.time}, Frame: {Time.frameCount}");
            
            _objectName = GetParameter(0);
            string completionMessage = GetParameter(1);

            Debug.Log($"{LOG_TAG}Parameters received - objectName: '{_objectName}', completionMessage: '{completionMessage}'");

            if (string.IsNullOrEmpty(_objectName))
            {
                Debug.LogError($"{LOG_TAG}ERROR: No object name provided! Stopping command.");
                Stop();
                return;
            }

            Debug.Log($"{LOG_TAG}Searching for GameObject: '{_objectName}'...");
            GameObject target = FindGameObjectIncludingInactive(_objectName);

            if (target == null)
            {
                Debug.LogError($"{LOG_TAG}ERROR: Could not find object named '{_objectName}'! Stopping command.");
                Debug.Log($"{LOG_TAG}Listing all root objects in active scene:");
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    Debug.Log($"{LOG_TAG}  Root: {root.name} (active: {root.activeSelf})");
                }
                Stop();
                return;
            }

            Debug.Log($"{LOG_TAG}Found target GameObject: '{target.name}' (active: {target.activeSelf}, activeInHierarchy: {target.activeInHierarchy})");
            Debug.Log($"{LOG_TAG}Target path: {GetGameObjectPath(target)}");

            _show = target.GetComponent<EnvelopeReadShow>();
            Debug.Log($"{LOG_TAG}GetComponent<EnvelopeReadShow> on target: {(_show != null ? "FOUND" : "NOT FOUND")}");
            
            if (_show == null)
            {
                Debug.Log($"{LOG_TAG}Trying GetComponentInChildren<EnvelopeReadShow>(true)...");
                _show = target.GetComponentInChildren<EnvelopeReadShow>(true);
                Debug.Log($"{LOG_TAG}GetComponentInChildren result: {(_show != null ? "FOUND" : "NOT FOUND")}");
            }

            if (_show == null)
            {
                Debug.LogError($"{LOG_TAG}ERROR: No EnvelopeReadShow component found on '{_objectName}' or its children! Stopping command.");
                Stop();
                return;
            }

            Debug.Log($"{LOG_TAG}EnvelopeReadShow component found on: '{_show.gameObject.name}'");
            Debug.Log($"{LOG_TAG}EnvelopeReadShow enabled: {_show.enabled}, isActiveAndEnabled: {_show.isActiveAndEnabled}");
            Debug.Log($"{LOG_TAG}EnvelopeReadShow IsPlaying before TryPlay: {_show.IsPlaying}");

            Debug.Log($"{LOG_TAG}Subscribing to Completed event...");
            _show.Completed += HandleCompleted;

            Debug.Log($"{LOG_TAG}Calling TryPlay with message: '{(string.IsNullOrEmpty(completionMessage) ? "(null)" : completionMessage)}'");
            bool started = _show.TryPlay(string.IsNullOrEmpty(completionMessage) ? null : completionMessage);
            
            Debug.Log($"{LOG_TAG}TryPlay returned: {started}");
            Debug.Log($"{LOG_TAG}EnvelopeReadShow IsPlaying after TryPlay: {_show.IsPlaying}");

            if (!started)
            {
                Debug.LogWarning($"{LOG_TAG}WARNING: TryPlay returned false! Unsubscribing and stopping command.");
                _show.Completed -= HandleCompleted;
                Stop();
            }
            else
            {
                Debug.Log($"{LOG_TAG}TryPlay succeeded! Waiting for Completed callback...");
            }
        }

        private void HandleCompleted()
        {
            Debug.Log($"{LOG_TAG}========== HANDLE COMPLETED CALLED ==========");
            Debug.Log($"{LOG_TAG}Time: {Time.time}, Frame: {Time.frameCount}");
            
            if (_show != null)
            {
                Debug.Log($"{LOG_TAG}Unsubscribing from Completed event");
                _show.Completed -= HandleCompleted;
            }

            Debug.Log($"{LOG_TAG}Calling Stop() to finish sequencer command");
            Stop();
            Debug.Log($"{LOG_TAG}Stop() called - command should now be complete");
        }

        private void OnDisable()
        {
            Debug.Log($"{LOG_TAG}OnDisable called for object: '{_objectName}'");
            if (_show != null)
            {
                Debug.Log($"{LOG_TAG}Cleaning up: unsubscribing from Completed event");
                _show.Completed -= HandleCompleted;
            }
        }

        private void OnDestroy()
        {
            Debug.Log($"{LOG_TAG}OnDestroy called for object: '{_objectName}'");
        }

        private GameObject FindGameObjectIncludingInactive(string name)
        {
            Debug.Log($"{LOG_TAG}FindGameObjectIncludingInactive: Trying GameObject.Find('{name}')...");
            GameObject activeObj = GameObject.Find(name);
            if (activeObj != null)
            {
                Debug.Log($"{LOG_TAG}FindGameObjectIncludingInactive: Found via GameObject.Find!");
                return activeObj;
            }

            Debug.Log($"{LOG_TAG}FindGameObjectIncludingInactive: Not found via Find, searching inactive objects...");
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            Debug.Log($"{LOG_TAG}FindGameObjectIncludingInactive: Searching {rootObjects.Length} root objects...");

            foreach (GameObject root in rootObjects)
            {
                GameObject result = FindRecursive(root.transform, name);
                if (result != null)
                {
                    Debug.Log($"{LOG_TAG}FindGameObjectIncludingInactive: Found inactive object!");
                    return result;
                }
            }

            Debug.Log($"{LOG_TAG}FindGameObjectIncludingInactive: Object not found anywhere!");
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

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}
