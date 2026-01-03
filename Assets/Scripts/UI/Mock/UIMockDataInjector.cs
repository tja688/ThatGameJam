using System;
using System.Collections.Generic;
using ThatGameJam.UI.Models;
using UnityEngine;

namespace ThatGameJam.UI.Mock
{
    public class UIMockDataInjector
    {
        private readonly MockAudioSettingsService _audioSettings;
        private readonly MockSaveService _saveService;
        private readonly MockPlayerStatusProvider _playerStatus;
        private readonly MockQuestLogProvider _questLog;
        private readonly MockInventoryProvider _inventory;

        public UIMockDataInjector(
            MockAudioSettingsService audioSettings,
            MockSaveService saveService,
            MockPlayerStatusProvider playerStatus,
            MockQuestLogProvider questLog,
            MockInventoryProvider inventory)
        {
            _audioSettings = audioSettings;
            _saveService = saveService;
            _playerStatus = playerStatus;
            _questLog = questLog;
            _inventory = inventory;
        }

        public void InjectAll()
        {
            InjectAudio();
            InjectSave();
            InjectPlayerStatus();
            InjectQuests();
            InjectInventory();
        }

        private void InjectAudio()
        {
            if (_audioSettings == null)
            {
                return;
            }

            _audioSettings.SetBgmVolume(UnityEngine.Random.Range(0.2f, 0.8f));
            _audioSettings.SetSfxVolume(UnityEngine.Random.Range(0.3f, 0.9f));
            _audioSettings.SetUiVolume(UnityEngine.Random.Range(0.1f, 0.6f));
        }

        private void InjectSave()
        {
            if (_saveService == null)
            {
                return;
            }

            var info = new SaveSlotInfo
            {
                SavedAt = DateTime.Now,
                AreaName = PickOne(new[] { "Ruins", "Hall", "Observatory" }),
                DeathsInArea = UnityEngine.Random.Range(0, 5),
                TotalDeaths = UnityEngine.Random.Range(2, 18),
                LightValue = UnityEngine.Random.Range(10f, 85f),
                Summary = "Mock snapshot ready"
            };

            _saveService.SetSlotInfo(info);
        }

        private void InjectPlayerStatus()
        {
            if (_playerStatus == null)
            {
                return;
            }

            var data = new PlayerPanelData
            {
                Portrait = CreateIconTexture(new Color(0.3f, 0.6f, 0.9f)),
                AreaName = PickOne(new[] { "Ruins", "Archive", "Underpass" }),
                DeathsInArea = UnityEngine.Random.Range(0, 4),
                TotalDeaths = UnityEngine.Random.Range(3, 22),
                LightValue = UnityEngine.Random.Range(10f, 100f)
            };

            _playerStatus.SetData(data);
        }

        private void InjectQuests()
        {
            if (_questLog == null)
            {
                return;
            }

            var quests = new List<QuestData>
            {
                new QuestData
                {
                    Id = "quest_01",
                    Title = "Wake the Beacon",
                    Description = "Restore power to the eastern beacon tower.",
                    Requirements = "Requires: 2 Spark Coils",
                    IsCompleted = false
                },
                new QuestData
                {
                    Id = "quest_02",
                    Title = "Find the Archivist",
                    Description = "Locate the lost archivist in the lower halls.",
                    Requirements = "Objective: Reach Lower Hall",
                    IsCompleted = true
                },
                new QuestData
                {
                    Id = "quest_03",
                    Title = "Seal the Rift",
                    Description = "Collect sigils and close the rift at the bridge.",
                    Requirements = "Needs: 1 Rift Sigil",
                    IsCompleted = false
                }
            };

            _questLog.SetQuests(quests);
        }

        private void InjectInventory()
        {
            if (_inventory == null)
            {
                return;
            }

            var slots = new List<ItemStack>();
            for (var i = 0; i < 12; i++)
            {
                if (UnityEngine.Random.value > 0.5f)
                {
                    slots.Add(null);
                    continue;
                }

                slots.Add(new ItemStack
                {
                    ItemId = $"item_{i}",
                    DisplayName = "Mock Item",
                    Description = "A placeholder item for UI layout testing.",
                    Icon = CreateIconTexture(new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value)),
                    Quantity = UnityEngine.Random.Range(1, 4)
                });
            }

            _inventory.SetSlots(slots);
        }

        private static string PickOne(string[] options)
        {
            if (options == null || options.Length == 0)
            {
                return string.Empty;
            }

            var index = UnityEngine.Random.Range(0, options.Length);
            return options[index];
        }

        private static Texture2D CreateIconTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;
            return texture;
        }
    }
}
