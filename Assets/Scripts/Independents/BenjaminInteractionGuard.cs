using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Controls the active state of a Target GameObject based on BenjaminState in DialogueVariableBridge.
    /// Logic: SetActive(true) ONLY when BenjaminState is 2.
    /// </summary>
    public class BenjaminInteractionGuard : MonoBehaviour
    {
        [Tooltip("The GameObject to show/hide. (Note: If this script is on the target itself, it can turn it OFF but won't be able to turn it back ON because the script will stop running).")]
        public GameObject TargetObject;

        private void Start()
        {
            if (DialogueVariableBridge.Instance != null)
            {
                DialogueVariableBridge.Instance.ValueChanged += OnVariableChanged;
                // Initial check
                UpdateState(DialogueVariableBridge.Instance.VariableBValue);
            }
            else
            {
                Debug.LogWarning("[BenjaminInteractionGuard] DialogueVariableBridge instance not found.");
            }
        }

        private void OnDestroy()
        {
            if (DialogueVariableBridge.Instance != null)
            {
                DialogueVariableBridge.Instance.ValueChanged -= OnVariableChanged;
            }
        }

        private void OnVariableChanged(string varName, float value)
        {
            // BenjaminState is mapped to Variable B
            if (varName == DialogueVariableBridge.Instance.VariableBName)
            {
                UpdateState(value);
            }
        }

        private void UpdateState(float state)
        {
            if (TargetObject == null) return;

            // Activate when state == 2, otherwise deactivate
            bool shouldBeActive = Mathf.Approximately(state, 3f);
            TargetObject.SetActive(shouldBeActive);
        }
    }
}
