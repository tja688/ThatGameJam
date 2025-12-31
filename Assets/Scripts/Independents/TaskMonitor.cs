using PixelCrushers.DialogueSystem;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Queries;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Monitors player backpack and NPC interactions to update Dialogue System variables via DialogueVariableBridge.
    /// Values:
    /// 0: No interaction
    /// 1: Interacted (talked)
    /// 2: Has required item
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
        [Tooltip("The ID of the Letter item")]
        public string LetterItemName = "Letter";
        [Tooltip("The ID of the Newspaper item")]
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
            // We want to know who the player is talking to (the Conversant)
            var conversant = DialogueManager.currentConversant;

            if (conversant != null)
            {
                if (Ella != null && conversant == Ella.transform)
                {
                    _hasInteractedWithElla = true;
                    // Force check immediately to update state to 1
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

        /// <summary>
        /// Evaluates current state and updates the DialogueVariableBridge.
        /// </summary>
        private void CheckTasks()
        {
            if (DialogueVariableBridge.Instance == null)
            {
                return;
            }

            // Check items in backpack (Supports matching by ID or Display Name)
            bool hasLetter = HasItem(LetterItemName);
            bool hasNewspaper = HasItem(NewspaperItemName);

            // Determine Ella's State (Variable A)
            float ellaState = 0;
            if (hasLetter)
            {
                ellaState = 2;
            }
            else if (_hasInteractedWithElla)
            {
                ellaState = 1;
            }

            // Determine Benjamin's State (Variable B)
            float benjaminState = 0;
            if (hasNewspaper)
            {
                benjaminState = 2;
            }
            else if (_hasInteractedWithBenjamin)
            {
                benjaminState = 1;
            }

            // Update Bridge
            DialogueVariableBridge.Instance.SetVariableA(ellaState);
            DialogueVariableBridge.Instance.SetVariableB(benjaminState);
        }

        private bool HasItem(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return false;

            // Get items directly from model to check both ID and Name
            var model = this.GetModel<ThatGameJam.Features.BackpackFeature.Models.IBackpackModel>()
                        as ThatGameJam.Features.BackpackFeature.Models.BackpackModel;

            if (model == null) return false;

            foreach (var item in model.Items)
            {
                if (item.Definition == null) continue;

                // Match against ID or Display Name (case-insensitive for safety)
                if (string.Equals(item.Definition.Id, idOrName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Definition.DisplayName, idOrName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
