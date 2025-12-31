using PixelCrushers.DialogueSystem;
using QFramework;
using ThatGameJam.Features.BackpackFeature.Commands;
using UnityEngine;

namespace PixelCrushers.DialogueSystem.SequencerCommands
{
    /// <summary>
    /// Sequencer Command: GiveItem(rewardKey, [quantity])
    /// 
    /// Adds an item to the player's backpack using the DialogueRewardBridge configuration.
    /// 
    /// Arguments:
    /// - rewardKey: The key defined in DialogueRewardBridge.
    /// - quantity: (Optional) The amount to give. Defaults to the bridge's setting.
    /// </summary>
    public class SequencerCommandGiveItem : SequencerCommand
    {
        public void Awake()
        {
            string rewardKey = GetParameter(0);
            int quantityOverride = GetParameterAsInt(1, -1);

            if (string.IsNullOrEmpty(rewardKey))
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandGiveItem: No reward key provided.");
                Stop();
                return;
            }

            if (ThatGameJam.Independents.DialogueRewardBridge.Instance == null)
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning("SequencerCommandGiveItem: DialogueRewardBridge instance not found in scene.");
                Stop();
                return;
            }

            var definition = ThatGameJam.Independents.DialogueRewardBridge.Instance.GetReward(rewardKey, out int defaultQuantity);

            if (definition != null)
            {
                int quantity = quantityOverride > 0 ? quantityOverride : defaultQuantity;

                // Use QFramework Architecture to send command
                GameRootApp.Interface.SendCommand(new AddItemCommand(definition, null, quantity));

                if (DialogueDebug.logInfo) Debug.Log($"SequencerCommandGiveItem: Successfully gave {quantity}x {definition.DisplayName} (Key: {rewardKey})");
            }
            else
            {
                if (DialogueDebug.logWarnings) Debug.LogWarning($"SequencerCommandGiveItem: Reward key '{rewardKey}' not found in bridge.");
            }

            Stop();
        }
    }
}
