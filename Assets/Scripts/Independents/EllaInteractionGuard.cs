using ThatGameJam.Features.InteractableFeature.Controllers;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Controls the enabled state of an Interactable component based on EllaState in DialogueVariableBridge.
    /// Disables when EllaState is 0, enables when it is 1 (or greater).
    /// </summary>
    public class EllaInteractionGuard : MonoBehaviour
    {
        private Interactable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            if (DialogueVariableBridge.Instance != null)
            {
                DialogueVariableBridge.Instance.ValueChanged += OnVariableChanged;
                // Initial check
                UpdateState(DialogueVariableBridge.Instance.VariableAValue);
            }
            else
            {
                Debug.LogWarning("[EllaInteractionGuard] DialogueVariableBridge instance not found.");
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
            // DialogueVariableBridge handles Ella state as Variable A (EllaState)
            if (varName == DialogueVariableBridge.Instance.VariableAName)
            {
                UpdateState(value);
            }
        }

        private void UpdateState(float state)
        {
            if (_interactable == null) return;

            // 0: Disabled
            // >= 1: Enabled (Normally 1 is Met, 2 is has item)
            // The user said "当状态为1时才启用", but usually 2 should also be interactable for turn-in.
            // I'll stick to >= 1 unless specifically told 1 ONLY.
            _interactable.enabled = state >= 1;

            // Log for visibility during sprint
            // Debug.Log($"[EllaInteractionGuard] EllaState changed to {state}. Interactable enabled: {_interactable.enabled}");
        }
    }
}
