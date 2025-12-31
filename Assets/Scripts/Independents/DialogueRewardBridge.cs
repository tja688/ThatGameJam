using System;
using System.Collections.Generic;
using ThatGameJam.Features.BackpackFeature.Models;
using UnityEngine;

namespace ThatGameJam.Independents
{
    /// <summary>
    /// Bridges Dialogue System sequencer commands and the Backpack system for rewards.
    /// Allows giving items by a simple key/name specified in the dialogue sequencer.
    /// </summary>
    public class DialogueRewardBridge : MonoBehaviour
    {
        public static DialogueRewardBridge Instance { get; private set; }

        [Serializable]
        public class RewardItem
        {
            [Tooltip("The unique key used in the sequencer command GiveItem(Key)")]
            public string Key;
            public ItemDefinition Definition;
            public int DefaultQuantity = 1;
        }

        [Header("Rewards Configuration")]
        public List<RewardItem> Rewards = new List<RewardItem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Tries to find a reward definition by its key.
        /// </summary>
        public ItemDefinition GetReward(string key, out int defaultQuantity)
        {
            defaultQuantity = 1;
            if (string.IsNullOrEmpty(key)) return null;

            var reward = Rewards.Find(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));
            if (reward != null)
            {
                defaultQuantity = reward.DefaultQuantity;
                return reward.Definition;
            }

            return null;
        }
    }
}
