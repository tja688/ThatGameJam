using PixelCrushers.DialogueSystem;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Commands;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Models;
using ThatGameJam.Features.BackpackFeature.Queries;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Monitors player backpack and NPC interactions to update Dialogue System variables via DialogueVariableBridge.
    /// Values:
    /// 0: No interaction
    /// 1: Interacted (talked)
    /// 2: Has required item (Item is removed upon next interaction in state 2)
    /// </summary>
    public class TaskMonitor : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        [Header("NPC References")]
        [Tooltip("Assign the Ella NPC object here")]
        public GameObject Ella;
        [Tooltip("Assign the Benjamin NPC object here")]
        public GameObject Benjamin;

        [Header("Item Configuration")]
        [Tooltip("The ID or Name of the Letter item")]
        public string LetterItemName = "Letter";
        [Tooltip("The ID or Name of the Newspaper item")]
        public string NewspaperItemName = "Newspaper";

        [Header("Debug / State")]
        [SerializeField] private bool _hasInteractedWithElla;
        [SerializeField] private bool _hasInteractedWithBenjamin;

        private void Start()
        {
            // Listen for backpack changes
            this.RegisterEvent<BackpackChangedEvent>(OnBackpackChanged);

            // Listen for dialogue start to detect interaction
            if (DialogueManager.instance != null)
            {
                DialogueManager.instance.conversationStarted += OnConversationStarted;
            }
            else
            {
                Debug.LogWarning("[TaskMonitor] DialogueManager instance not found. Interaction monitoring may not work.");
            }

            // Initial check
            CheckTasks();
        }

        private void OnDestroy()
        {
            this.UnRegisterEvent<BackpackChangedEvent>(OnBackpackChanged);
            if (DialogueManager.instance != null)
            {
                DialogueManager.instance.conversationStarted -= OnConversationStarted;
            }
        }

        private void OnConversationStarted(Transform actor)
        {
            var conversant = DialogueManager.currentConversant;

            if (conversant != null)
            {
                if (Ella != null && conversant == Ella.transform)
                {
                    // If we were already in state 2 (had item) and we talk to them again, remove the item
                    if (DialogueVariableBridge.Instance != null &&
                        Mathf.Approximately(DialogueVariableBridge.Instance.VariableAValue, 2f))
                    {
                        ConsumeItemForTask(LetterItemName);
                    }

                    _hasInteractedWithElla = true;
                    // Force check immediately
                    CheckTasks();
                }
                else if (Benjamin != null && conversant == Benjamin.transform)
                {
                    _hasInteractedWithBenjamin = true;
                    CheckTasks();
                }
            }
        }

        private void OnBackpackChanged(BackpackChangedEvent e)
        {
            CheckTasks();
        }

        private void ConsumeItemForTask(string idOrName)
        {
            string actualId = GetActualItemId(idOrName);
            if (!string.IsNullOrEmpty(actualId))
            {
                this.SendCommand(new RemoveItemCommand(actualId, 1));
                // Debug.Log($"[TaskMonitor] Consumed item: {actualId} for task.");
            }
        }

        /// <summary>
        /// Evaluates current state and updates the DialogueVariableBridge.
        /// </summary>
        private void CheckTasks()
        {
            if (DialogueVariableBridge.Instance == null)
            {
                return;
            }

            float currentEllaState = DialogueVariableBridge.Instance.VariableAValue;
            float currentBenjaminState = DialogueVariableBridge.Instance.VariableBValue;

            // Check items in backpack (Supports matching by ID or Display Name)
            bool hasLetter = HasItem(LetterItemName);
            bool hasNewspaper = HasItem(NewspaperItemName);

            // Determine Ella's State (Variable A)
            float ellaState = currentEllaState >= 3f ? currentEllaState : 0f;
            if (ellaState < 3f)
            {
                if (hasLetter)
                {
                    ellaState = 2f;
                }
                else if (_hasInteractedWithElla)
                {
                    ellaState = 1f;
                }
            }

            // Determine Benjamin's State (Variable B)
            float benjaminState = currentBenjaminState >= 3f ? currentBenjaminState : 0f;
            if (benjaminState < 3f)
            {
                if (hasNewspaper)
                {
                    benjaminState = 2f;
                }
                else if (_hasInteractedWithBenjamin)
                {
                    benjaminState = 1f;
                }
            }

            // Update Bridge
            DialogueVariableBridge.Instance.SetVariableA(ellaState);
            DialogueVariableBridge.Instance.SetVariableB(benjaminState);
        }

        private string GetActualItemId(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return null;

            var model = this.GetModel<IBackpackModel>() as BackpackModel;
            if (model == null) return null;

            foreach (var item in model.Items)
            {
                if (item.Definition == null) continue;
                if (string.Equals(item.Definition.Id, idOrName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Definition.DisplayName, idOrName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return item.Definition.Id;
                }
            }
            return null;
        }

        private bool HasItem(string idOrName)
        {
            return !string.IsNullOrEmpty(GetActualItemId(idOrName));
        }
    }
}
