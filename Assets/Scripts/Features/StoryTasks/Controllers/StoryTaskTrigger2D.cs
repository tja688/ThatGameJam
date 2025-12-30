using System;
using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.DoorGate.Commands;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.StoryTasks.Commands;
using ThatGameJam.Features.StoryTasks.Events;
using ThatGameJam.Features.StoryTasks.Queries;
using UnityEngine;

namespace ThatGameJam.Features.StoryTasks.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class StoryTaskTrigger2D : MonoBehaviour, IController, ICanSendEvent
    {
        [SerializeField] private string triggerFlagId;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private StoryTaskAction[] actions;

        private bool _triggered;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("StoryTaskTrigger2D expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (triggerOnce && _triggered)
            {
                return;
            }

            if (!string.IsNullOrEmpty(triggerFlagId) && this.SendQuery(new HasFlagQuery(triggerFlagId)))
            {
                return;
            }

            if (!ExecuteActions())
            {
                return;
            }

            _triggered = true;
            if (!string.IsNullOrEmpty(triggerFlagId))
            {
                this.SendCommand(new SetFlagCommand(triggerFlagId));
            }
        }

        private bool ExecuteActions()
        {
            if (actions == null || actions.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < actions.Length; i++)
            {
                if (!ExecuteAction(actions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ExecuteAction(StoryTaskAction action)
        {
            switch (action.Type)
            {
                case StoryTaskActionType.RequireFlag:
                    if (string.IsNullOrEmpty(action.FlagId))
                    {
                        return true;
                    }

                    return this.SendQuery(new HasFlagQuery(action.FlagId));
                case StoryTaskActionType.SetFlag:
                    this.SendCommand(new SetFlagCommand(action.FlagId));
                    return true;
                case StoryTaskActionType.PlayDialogue:
                    return ExecuteDialogue(action);
                case StoryTaskActionType.SpawnLamp:
                    ExecuteSpawnLamp(action);
                    return true;
                case StoryTaskActionType.SetGateState:
                    if (!string.IsNullOrEmpty(action.DoorId))
                    {
                        this.SendCommand(new SetDoorStateCommand(action.DoorId, action.GateOpen));
                        AudioService.Play("SFX-INT-0012", new AudioContext
                        {
                            Position = transform.position,
                            HasPosition = true
                        });
                    }
                    return true;
                default:
                    return true;
            }
        }

        private bool ExecuteDialogue(StoryTaskAction action)
        {
            if (string.IsNullOrEmpty(action.DialogueId))
            {
                return true;
            }

            if (action.DialogueOnce)
            {
                var onceFlag = string.IsNullOrEmpty(action.DialogueOnceFlagId)
                    ? $"dialogue:{action.DialogueId}"
                    : action.DialogueOnceFlagId;

                if (this.SendQuery(new HasFlagQuery(onceFlag)))
                {
                    return true;
                }

                this.SendCommand(new SetFlagCommand(onceFlag));
            }

            this.SendEvent(new DialogueRequestedEvent
            {
                DialogueId = action.DialogueId,
                Priority = action.DialoguePriority
            });

            AudioService.Play("SFX-META-0001", new AudioContext
            {
                Position = transform.position,
                HasPosition = true
            });

            return true;
        }

        private void ExecuteSpawnLamp(StoryTaskAction action)
        {
            var position = action.PositionRef != null ? action.PositionRef.position : transform.position;
            var presetId = action.LampPresetId;
            if (string.IsNullOrEmpty(presetId) && action.LampPreset != null)
            {
                presetId = action.LampPreset.name;
            }

            this.SendEvent(new RequestSpawnLampEvent
            {
                WorldPos = position,
                PrefabOverride = action.LampPreset,
                PresetId = presetId
            });

            AudioService.Play("SFX-INT-0013", new AudioContext
            {
                Position = position,
                HasPosition = true
            });
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }

        [Serializable]
        public struct StoryTaskAction
        {
            public StoryTaskActionType Type;

            [Header("Dialogue")]
            public string DialogueId;
            public int DialoguePriority;
            public bool DialogueOnce;
            public string DialogueOnceFlagId;

            [Header("Lamp")]
            public GameObject LampPreset;
            public string LampPresetId;
            public Transform PositionRef;

            [Header("Gate")]
            public string DoorId;
            public bool GateOpen;

            [Header("Flag")]
            public string FlagId;
        }

        public enum StoryTaskActionType
        {
            PlayDialogue,
            SpawnLamp,
            SetGateState,
            SetFlag,
            RequireFlag
        }
    }
}
