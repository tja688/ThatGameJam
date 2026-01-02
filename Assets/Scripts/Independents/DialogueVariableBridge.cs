using System;
using System.Collections.Generic;
using Language.Lua;
using PixelCrushers.DialogueSystem;
using ThatGameJam.SaveSystem;
using ThatGameJam.UI.Models;
using ThatGameJam.UI.Services;
using ThatGameJam.UI.Services.Interfaces;
using UnityEngine;

namespace ThatGameJam.Independents
{
    [Serializable]
    public class DialogueVariableBridgeSaveData
    {
        public float variableA;
        public float variableB;
    }

    [Serializable]
    public class DialogueQuestStageContent
    {
        public string Title;
        [TextArea(2, 4)] public string Description;
        [TextArea(1, 3)] public string Requirements;
    }

    [DisallowMultipleComponent]
    public class DialogueVariableBridge : SaveParticipant<DialogueVariableBridgeSaveData>, IQuestLogProvider
    {
        public static DialogueVariableBridge Instance { get; private set; }

        [Header("Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Dialogue Variables")]
        [SerializeField] private string variableAName = "VarA";
        [SerializeField] private float variableAValue;
        [SerializeField] private string variableBName = "VarB";
        [SerializeField] private float variableBValue;

        [Header("Sync")]
        [SerializeField] private bool syncFromDialogueOnEnable = true;

        [Header("Save")]
        [SerializeField] private string saveKey = "dialogue.variables";

        [Header("UI Quest Log")]
        [SerializeField] private bool registerQuestLogProvider = true;

        [Header("Ella Quest Text (Stages 1-3)")]
        [SerializeField] private string ellaQuestId = "quest_ella_letter";
        [SerializeField] private DialogueQuestStageContent ellaStage1 = new DialogueQuestStageContent
        {
            Title = "Ella's Letter",
            Description = "Ella asked you to find her missing letter.",
            Requirements = "Progress 1/3: Find the letter."
        };
        [SerializeField] private DialogueQuestStageContent ellaStage2 = new DialogueQuestStageContent
        {
            Title = "Ella's Letter",
            Description = "You found the letter. Bring it back to Ella.",
            Requirements = "Progress 2/3: Return the letter to Ella."
        };
        [SerializeField] private DialogueQuestStageContent ellaStage3 = new DialogueQuestStageContent
        {
            Title = "Ella's Letter",
            Description = "You returned the letter to Ella.",
            Requirements = "Progress 3/3: Completed."
        };

        [Header("Benjamin Quest Text (Stages 1-3)")]
        [SerializeField] private string benjaminQuestId = "quest_benjamin_newspaper";
        [SerializeField] private DialogueQuestStageContent benjaminStage1 = new DialogueQuestStageContent
        {
            Title = "Benjamin's Newspaper",
            Description = "Benjamin wants the latest newspaper.",
            Requirements = "Progress 1/3: Find a newspaper."
        };
        [SerializeField] private DialogueQuestStageContent benjaminStage2 = new DialogueQuestStageContent
        {
            Title = "Benjamin's Newspaper",
            Description = "You have the newspaper. Bring it to Benjamin.",
            Requirements = "Progress 2/3: Deliver the newspaper to Benjamin."
        };
        [SerializeField] private DialogueQuestStageContent benjaminStage3 = new DialogueQuestStageContent
        {
            Title = "Benjamin's Newspaper",
            Description = "You delivered the newspaper to Benjamin.",
            Requirements = "Progress 3/3: Completed."
        };

        public override string SaveKey => saveKey;

        public event Action<string, float> ValueChanged;
        public event Action OnQuestChanged;

        private string _variableAIndex;
        private string _variableBIndex;
        private readonly List<QuestData> _quests = new List<QuestData>();

        public float VariableAValue => variableAValue;
        public float VariableBValue => variableBValue;

        public string VariableAName => variableAName;
        public string VariableBName => variableBName;

        public IReadOnlyList<QuestData> GetQuests() => _quests;

        public static bool TryGet(string variableName, out float value)
        {
            value = 0f;
            return Instance != null && Instance.TryGetValue(variableName, out value);
        }

        public static bool TrySet(string variableName, float value)
        {
            return Instance != null && Instance.TrySetValue(variableName, value);
        }

        public bool TryGetValue(string variableName, out float value)
        {
            if (string.Equals(variableName, variableAName, StringComparison.Ordinal))
            {
                value = variableAValue;
                return true;
            }

            if (string.Equals(variableName, variableBName, StringComparison.Ordinal))
            {
                value = variableBValue;
                return true;
            }

            value = 0f;
            return false;
        }

        public bool TrySetValue(string variableName, float value)
        {
            if (string.Equals(variableName, variableAName, StringComparison.Ordinal))
            {
                SetVariableA(value);
                return true;
            }

            if (string.Equals(variableName, variableBName, StringComparison.Ordinal))
            {
                SetVariableB(value);
                return true;
            }

            return false;
        }

        public void SetVariableA(float value)
        {
            ApplyValue(VariableSlot.A, value, true);
        }

        public void SetVariableB(float value)
        {
            ApplyValue(VariableSlot.B, value, true);
        }

        public void RefreshFromDialogue()
        {
            SyncFromDialogue();
        }

        public void ApplyToDialogue()
        {
            PushToDialogue();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CacheVariableIndices();
            RegisterMonitoring();

            if (syncFromDialogueOnEnable)
            {
                SyncFromDialogue();
            }
            else
            {
                PushToDialogue();
            }

            if (registerQuestLogProvider)
            {
                UIServiceRegistry.SetQuestLog(this);
                RefreshQuestLog();
            }
        }

        protected override void OnDisable()
        {
            UnregisterMonitoring();
            if (registerQuestLogProvider && UIServiceRegistry.QuestLog == this)
            {
                UIServiceRegistry.SetQuestLog(null);
            }
            base.OnDisable();
        }

        private void CacheVariableIndices()
        {
            _variableAIndex = string.IsNullOrEmpty(variableAName)
                ? string.Empty
                : DialogueLua.StringToTableIndex(variableAName);
            _variableBIndex = string.IsNullOrEmpty(variableBName)
                ? string.Empty
                : DialogueLua.StringToTableIndex(variableBName);
        }

        private void RegisterMonitoring()
        {
            if (!string.IsNullOrEmpty(_variableAIndex))
            {
                Assignment.MonitoredVariables.Add(_variableAIndex);
            }

            if (!string.IsNullOrEmpty(_variableBIndex))
            {
                Assignment.MonitoredVariables.Add(_variableBIndex);
            }

            Assignment.VariableChanged += HandleVariableChanged;
        }

        private void UnregisterMonitoring()
        {
            Assignment.VariableChanged -= HandleVariableChanged;

            if (!string.IsNullOrEmpty(_variableAIndex))
            {
                Assignment.MonitoredVariables.Remove(_variableAIndex);
            }

            if (!string.IsNullOrEmpty(_variableBIndex))
            {
                Assignment.MonitoredVariables.Remove(_variableBIndex);
            }
        }

        private void SyncFromDialogue()
        {
            if (!string.IsNullOrEmpty(variableAName))
            {
                var value = DialogueLua.GetVariable(variableAName, variableAValue);
                ApplyValue(VariableSlot.A, value, false);

                if (!DialogueLua.DoesVariableExist(variableAName))
                {
                    DialogueLua.SetVariable(variableAName, variableAValue);
                }
            }

            if (!string.IsNullOrEmpty(variableBName))
            {
                var value = DialogueLua.GetVariable(variableBName, variableBValue);
                ApplyValue(VariableSlot.B, value, false);

                if (!DialogueLua.DoesVariableExist(variableBName))
                {
                    DialogueLua.SetVariable(variableBName, variableBValue);
                }
            }
        }

        private void PushToDialogue()
        {
            if (!string.IsNullOrEmpty(variableAName))
            {
                DialogueLua.SetVariable(variableAName, variableAValue);
            }

            if (!string.IsNullOrEmpty(variableBName))
            {
                DialogueLua.SetVariable(variableBName, variableBValue);
            }
        }

        private void HandleVariableChanged(string tableIndex, object value)
        {
            if (string.IsNullOrEmpty(tableIndex))
            {
                return;
            }

            if (tableIndex == _variableAIndex)
            {
                if (TryConvertNumber(value, out var number))
                {
                    ApplyValue(VariableSlot.A, number, false);
                }
                return;
            }

            if (tableIndex == _variableBIndex)
            {
                if (TryConvertNumber(value, out var number))
                {
                    ApplyValue(VariableSlot.B, number, false);
                }
            }
        }

        private void ApplyValue(VariableSlot slot, float value, bool pushToDialogue)
        {
            if (slot == VariableSlot.A)
            {
                if (Mathf.Approximately(variableAValue, value))
                {
                    return;
                }

                variableAValue = value;
                if (pushToDialogue && !string.IsNullOrEmpty(variableAName))
                {
                    DialogueLua.SetVariable(variableAName, value);
                }

                ValueChanged?.Invoke(variableAName, value);
                RefreshQuestLog();
                return;
            }

            if (Mathf.Approximately(variableBValue, value))
            {
                return;
            }

            variableBValue = value;
            if (pushToDialogue && !string.IsNullOrEmpty(variableBName))
            {
                DialogueLua.SetVariable(variableBName, value);
            }

            ValueChanged?.Invoke(variableBName, value);
            RefreshQuestLog();
        }

        private static bool TryConvertNumber(object value, out float number)
        {
            if (value == null)
            {
                number = 0f;
                return false;
            }

            switch (value)
            {
                case float floatValue:
                    number = floatValue;
                    return true;
                case double doubleValue:
                    number = (float)doubleValue;
                    return true;
                case int intValue:
                    number = intValue;
                    return true;
                case long longValue:
                    number = longValue;
                    return true;
                case short shortValue:
                    number = shortValue;
                    return true;
                case byte byteValue:
                    number = byteValue;
                    return true;
                case uint uintValue:
                    number = uintValue;
                    return true;
                case ulong ulongValue:
                    number = ulongValue;
                    return true;
                case bool boolValue:
                    number = boolValue ? 1f : 0f;
                    return true;
            }

            if (float.TryParse(value.ToString(), out var parsed))
            {
                number = parsed;
                return true;
            }

            number = 0f;
            return false;
        }

        protected override DialogueVariableBridgeSaveData Capture()
        {
            return new DialogueVariableBridgeSaveData
            {
                variableA = variableAValue,
                variableB = variableBValue
            };
        }

        protected override void Restore(DialogueVariableBridgeSaveData data)
        {
            if (data == null)
            {
                return;
            }

            ApplyValue(VariableSlot.A, data.variableA, true);
            ApplyValue(VariableSlot.B, data.variableB, true);
        }

        private void RefreshQuestLog()
        {
            if (!registerQuestLogProvider)
            {
                return;
            }

            _quests.Clear();

            var ellaQuest = BuildEllaQuest(ClampStage(variableAValue));
            if (ellaQuest != null)
            {
                _quests.Add(ellaQuest);
            }

            var benjaminQuest = BuildBenjaminQuest(ClampStage(variableBValue));
            if (benjaminQuest != null)
            {
                _quests.Add(benjaminQuest);
            }

            OnQuestChanged?.Invoke();
        }

        private static int ClampStage(float value)
        {
            int stage = Mathf.RoundToInt(value);
            if (stage <= 0)
            {
                return 0;
            }

            return stage > 3 ? 3 : stage;
        }

        private QuestData BuildEllaQuest(int stage)
        {
            switch (stage)
            {
                case 1:
                    return BuildQuest(ellaQuestId, ellaStage1, false);
                case 2:
                    return BuildQuest(ellaQuestId, ellaStage2, false);
                case 3:
                    return BuildQuest(ellaQuestId, ellaStage3, true);
            }

            return null;
        }

        private QuestData BuildBenjaminQuest(int stage)
        {
            switch (stage)
            {
                case 1:
                    return BuildQuest(benjaminQuestId, benjaminStage1, false);
                case 2:
                    return BuildQuest(benjaminQuestId, benjaminStage2, false);
                case 3:
                    return BuildQuest(benjaminQuestId, benjaminStage3, true);
            }

            return null;
        }

        private static QuestData BuildQuest(string questId, DialogueQuestStageContent stage, bool completed)
        {
            DialogueQuestStageContent content = stage ?? new DialogueQuestStageContent();
            return new QuestData
            {
                Id = questId,
                Title = content.Title ?? string.Empty,
                Description = content.Description ?? string.Empty,
                Requirements = content.Requirements ?? string.Empty,
                IsCompleted = completed
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyValue(VariableSlot.A, variableAValue, true);
                ApplyValue(VariableSlot.B, variableBValue, true);
            }
        }
#endif

        private enum VariableSlot
        {
            A,
            B
        }
    }
}
