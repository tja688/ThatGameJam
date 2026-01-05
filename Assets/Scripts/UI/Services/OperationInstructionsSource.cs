using System;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.UI.Services
{
    public class OperationInstructionsSource : MonoBehaviour, IOperationInstructionsProvider
    {
        [TextArea(6, 12)]
        [SerializeField] private string instructions;

        public event Action<string> OnChanged;

        public string GetInstructions() => instructions ?? string.Empty;

        public void SetInstructions(string text)
        {
            instructions = text ?? string.Empty;
            OnChanged?.Invoke(instructions);
        }

        private void OnEnable()
        {
            UIServiceRegistry.SetOperationInstructions(this);
        }

        private void OnDisable()
        {
            if (UIServiceRegistry.OperationInstructions == this)
            {
                UIServiceRegistry.SetOperationInstructions(null);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            OnChanged?.Invoke(instructions ?? string.Empty);
        }
#endif
    }
}
